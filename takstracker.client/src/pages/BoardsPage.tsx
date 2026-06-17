import { useEffect, useMemo, useState } from "react";
import type {
  ApiError,
  CreateTaskRequest,
  ProjectDto,
  TaskDto,
  UserProfileDto,
} from "../api/types";
import { api } from "../api/client";

const statusLabels = ["Do zrobienia", "W trakcie", "Ukończone", "Zablokowane"];
const priorityLabels = ["Niski", "Średni", "Wysoki", "Krytyczny"];

type TaskFormState = CreateTaskRequest & { status: number };

const emptyTaskForm: TaskFormState = {
  projectId: "",
  title: "",
  description: "",
  assigneeId: "",
  priority: 1,
  status: 0,
  dueDate: undefined,
};

const emptyFilters = {
  query: "",
  status: "",
  priority: "",
  assignee: "",
  dueFrom: "",
  dueTo: "",
};

const toDateInputValue = (value?: string | null) => value?.slice(0, 10) ?? "";

interface BoardsPageProps {
  selectedProjectId: string;
  selectedProject: ProjectDto | null;
  currentUser: UserProfileDto | null;
}

const BoardsPage = ({
  selectedProjectId,
  selectedProject,
  currentUser,
}: BoardsPageProps) => {
  const [tasks, setTasks] = useState<TaskDto[]>([]);
  const [projects, setProjects] = useState<ProjectDto[]>([]);
  const [users, setUsers] = useState<UserProfileDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingTask, setEditingTask] = useState<TaskDto | null>(null);
  const [form, setForm] = useState<TaskFormState>(emptyTaskForm);
  const [filters, setFilters] = useState(emptyFilters);
  const [modalError, setModalError] = useState<string | null>(null);
  const isAdmin = currentUser?.role === "Admin";

  const loadData = () =>
    Promise.all([
      api.getTasks(
        selectedProjectId ? { projectId: selectedProjectId } : undefined,
      ),
      api.getProjects(),
      api.getUsers(),
    ])
      .then(([taskData, projectData, userData]) => {
        const accessibleProjectIds = new Set(
          projectData.map((project) => project.id),
        );

        setTasks(
          selectedProjectId
            ? taskData
            : taskData.filter((task) =>
                accessibleProjectIds.has(task.projectId),
              ),
        );
        setProjects(projectData);
        setUsers(userData);
        setError(null);
      })
      .catch((err: unknown) => {
        const status = (err as ApiError)?.status;
        setError(
          status === 401
            ? "Zaloguj się, aby pobrać zadania."
            : "Błąd pobierania zadań.",
        );
      });

  useEffect(() => {
    loadData();
  }, [selectedProjectId]);

  const getProjectAssigneeOptions = (projectId: string) => {
    const project =
      projects.find((item) => item.id === projectId) ?? selectedProject;
    const memberIds = new Set(
      project?.members.map((member) => member.userId) ?? [],
    );

    if (project?.ownerId) {
      memberIds.add(project.ownerId);
    }

    if (!isAdmin) {
      return currentUser && memberIds.has(currentUser.userId)
        ? [currentUser]
        : [];
    }

    return users.filter((user) => memberIds.has(user.userId));
  };

  const openCreateModal = () => {
    const assigneeOptions = getProjectAssigneeOptions(selectedProjectId);
    setEditingTask(null);
    setForm({
      ...emptyTaskForm,
      projectId: selectedProjectId,
      assigneeId: assigneeOptions[0]?.userId ?? "",
    });
    setIsModalOpen(true);
  };

  const openEditModal = (task: TaskDto) => {
    setEditingTask(task);
    setForm({
      projectId: task.projectId,
      title: task.title,
      description: task.description,
      assigneeId: task.assigneeId,
      priority: task.priority,
      status: task.status,
      dueDate: toDateInputValue(task.dueDate) || undefined,
    });
    setIsModalOpen(true);
  };

  const closeModal = () => {
    setIsModalOpen(false);
    setEditingTask(null);
    setForm(emptyTaskForm);
    setModalError(null);
  };

  const handleSubmit = async () => {
    if (!selectedProjectId || !form.title.trim()) {
      setError("Wybierz projekt na pasku u góry i podaj tytuł zadania.");
      return;
    }

    const assigneeId = isAdmin
      ? form.assigneeId
      : (currentUser?.userId ?? form.assigneeId);

    try {
      setSaving(true);
      if (editingTask) {
        await api.updateTask(editingTask.id, {
          title: form.title,
          description: form.description,
          assigneeId,
          priority: form.priority,
          status: form.status,
          dueDate: form.dueDate,
        });
      } else {
        await api.createTask({
          projectId: selectedProjectId,
          title: form.title,
          description: form.description,
          assigneeId,
          priority: form.priority,
          dueDate: form.dueDate,
        });
      }
      closeModal();
      await loadData();
    } catch (err: unknown) {
      const apiError = err as ApiError;
      const status = apiError?.status;
      setModalError(
        !status
          ? "Brak połączenia z serwerem."
          : status === 401
            ? "Zaloguj się, aby zapisać zadanie."
            : apiError?.message
              ? apiError.message
              : editingTask
                ? "Nie udało się zaktualizować zadania."
                : "Nie udało się dodać zadania.",
      );
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (task: TaskDto) => {
    if (!window.confirm(`Usunąć zadanie "${task.title}"?`)) {
      return;
    }

    try {
      await api.deleteTask(task.id);
      await loadData();
    } catch (err: unknown) {
      const status = (err as ApiError)?.status;
      setError(
        status === 401
          ? "Zaloguj się, aby usunąć zadanie."
          : "Nie udało się usunąć zadania.",
      );
    }
  };

  const getUserName = (userId: string) =>
    users.find((user) => user.userId === userId)?.displayName ?? userId;

  const getProjectName = (projectId: string) =>
    projects.find((project) => project.id === projectId)?.name ?? projectId;

  const canManageTask = (task: TaskDto) =>
    isAdmin || task.assigneeId === currentUser?.userId;

  const modalAssigneeOptions = getProjectAssigneeOptions(
    form.projectId || selectedProjectId,
  );

  const filteredTasks = useMemo(() => {
    const query = filters.query.trim().toLowerCase();

    return tasks.filter((task) => {
      if (query) {
        const haystack =
          `${task.title} ${task.description ?? ""}`.toLowerCase();
        if (!haystack.includes(query)) {
          return false;
        }
      }

      if (filters.status !== "" && task.status !== Number(filters.status)) {
        return false;
      }

      if (
        filters.priority !== "" &&
        task.priority !== Number(filters.priority)
      ) {
        return false;
      }

      if (filters.assignee === "unassigned" && task.assigneeId) {
        return false;
      }

      if (
        filters.assignee &&
        filters.assignee !== "unassigned" &&
        task.assigneeId !== filters.assignee
      ) {
        return false;
      }

      const dueDateValue = task.dueDate?.slice(0, 10);
      if (
        filters.dueFrom &&
        (!dueDateValue || dueDateValue < filters.dueFrom)
      ) {
        return false;
      }

      if (filters.dueTo && (!dueDateValue || dueDateValue > filters.dueTo)) {
        return false;
      }

      return true;
    });
  }, [filters, tasks]);

  return (
    <>
      <header className="page-header">
        <div>
          <h1>Wszystkie zadania</h1>
          <p>
            {selectedProject
              ? `Zadania projektu ${selectedProject.name}`
              : "Zadania ze wszystkich projektów"}
          </p>
        </div>
        <button
          type="button"
          className="btn btn-primary primary-action"
          onClick={openCreateModal}
          disabled={!selectedProjectId}
        >
          Dodaj zadanie
        </button>
      </header>

      <section className="workspace">
        <div className="card work wide-panel task-list-panel">
          <div className="task-list-header">
            <div>
              <h2>Lista zadań</h2>
              <span>
                {filteredTasks.length} z {tasks.length} zadań
              </span>
            </div>
          </div>
          <div className="task-filters">
            <div className="task-filters-row primary">
              <label>
                Szukaj
                <input
                  type="text"
                  value={filters.query}
                  onChange={(event) =>
                    setFilters({ ...filters, query: event.target.value })
                  }
                  placeholder="Tytuł lub opis"
                />
              </label>
              <label>
                Status
                <select
                  value={filters.status}
                  onChange={(event) =>
                    setFilters({ ...filters, status: event.target.value })
                  }
                >
                  <option value="">Wszystkie</option>
                  {statusLabels.map((label, index) => (
                    <option key={label} value={index}>
                      {label}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Priorytet
                <select
                  value={filters.priority}
                  onChange={(event) =>
                    setFilters({ ...filters, priority: event.target.value })
                  }
                >
                  <option value="">Wszystkie</option>
                  {priorityLabels.map((label, index) => (
                    <option key={label} value={index}>
                      {label}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Przypisany
                <select
                  value={filters.assignee}
                  onChange={(event) =>
                    setFilters({ ...filters, assignee: event.target.value })
                  }
                >
                  <option value="">Wszyscy</option>
                  <option value="unassigned">Nieprzypisane</option>
                  {users.map((user) => (
                    <option key={user.userId} value={user.userId}>
                      {user.displayName}
                    </option>
                  ))}
                </select>
              </label>
            </div>
            <div className="task-filters-row secondary">
              <label>
                Termin od
                <input
                  type="date"
                  value={filters.dueFrom}
                  onChange={(event) =>
                    setFilters({ ...filters, dueFrom: event.target.value })
                  }
                />
              </label>
              <label>
                Termin do
                <input
                  type="date"
                  value={filters.dueTo}
                  onChange={(event) =>
                    setFilters({ ...filters, dueTo: event.target.value })
                  }
                />
              </label>
              <button
                type="button"
                className="btn btn-outline-secondary"
                onClick={() => setFilters(emptyFilters)}
              >
                Wyczyść
              </button>
            </div>
          </div>
          {error ? (
            <div className="empty-state">{error}</div>
          ) : (
            <table className="table">
              <thead>
                <tr>
                  <th>Tytuł</th>
                  <th>Projekt</th>
                  <th>Przypisano</th>
                  <th>Status</th>
                  <th>Priorytet</th>
                  <th>Akcje</th>
                </tr>
              </thead>
              <tbody>
                {filteredTasks.map((task) => (
                  <tr key={task.id}>
                    <td>
                      <strong className="task-title-cell">{task.title}</strong>
                    </td>
                    <td>{getProjectName(task.projectId)}</td>
                    <td>
                      {task.assigneeId
                        ? getUserName(task.assigneeId)
                        : "Nieprzypisane"}
                    </td>
                    <td>
                      <span className={`table-pill status-${task.status}`}>
                        {statusLabels[task.status] ?? task.status}
                      </span>
                    </td>
                    <td>
                      <span className={`table-pill priority-${task.priority}`}>
                        {priorityLabels[task.priority] ?? task.priority}
                      </span>
                    </td>
                    <td>
                      {canManageTask(task) ? (
                        <div className="table-actions">
                          {selectedProjectId && (
                            <button
                              type="button"
                              className="btn btn-outline-secondary"
                              onClick={() => openEditModal(task)}
                            >
                              Edytuj
                            </button>
                          )}
                          <button
                            type="button"
                            className="btn btn-outline-danger"
                            onClick={() => handleDelete(task)}
                          >
                            Usuń
                          </button>
                        </div>
                      ) : (
                        <span className="empty-state">Tylko podgląd</span>
                      )}
                    </td>
                  </tr>
                ))}
                {tasks.length === 0 && (
                  <tr>
                    <td colSpan={6}>Brak zadań dla wybranego projektu.</td>
                  </tr>
                )}
                {tasks.length > 0 && filteredTasks.length === 0 && (
                  <tr>
                    <td colSpan={6}>Brak zadań spełniających kryteria.</td>
                  </tr>
                )}
              </tbody>
            </table>
          )}
        </div>
      </section>

      {isModalOpen && (
        <div
          className="modal-backdrop"
          role="presentation"
          onMouseDown={closeModal}
        >
          <div
            className="modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="task-modal-title"
            onMouseDown={(event) => event.stopPropagation()}
          >
            <div className="modal-header">
              <h2 id="task-modal-title">
                {editingTask ? "Edytuj zadanie" : "Dodaj zadanie"}
              </h2>
              <button
                type="button"
                className="btn icon-button"
                onClick={closeModal}
                aria-label="Zamknij"
              >
                x
              </button>
            </div>
            <form className="form" onSubmit={(event) => event.preventDefault()}>
              <label>
                Projekt
                <select value={form.projectId || selectedProjectId} disabled>
                  <option value="">Wybierz projekt na pasku</option>
                  {projects.map((project) => (
                    <option key={project.id} value={project.id}>
                      {project.name}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Tytuł
                <input
                  type="text"
                  value={form.title}
                  onChange={(event) =>
                    setForm({ ...form, title: event.target.value })
                  }
                />
              </label>
              <label>
                Opis
                <textarea
                  rows={3}
                  value={form.description}
                  onChange={(event) =>
                    setForm({ ...form, description: event.target.value })
                  }
                />
              </label>
              <label>
                Przypisana osoba
                <select
                  value={form.assigneeId}
                  onChange={(event) =>
                    setForm({ ...form, assigneeId: event.target.value })
                  }
                  disabled={!isAdmin}
                >
                  {isAdmin && <option value="">Nieprzypisane</option>}
                  {modalAssigneeOptions.map((user) => (
                    <option key={user.userId} value={user.userId}>
                      {user.displayName}
                    </option>
                  ))}
                </select>
                {modalAssigneeOptions.length === 0 && (
                  <small>Brak osób przypisanych do tego projektu.</small>
                )}
              </label>
              {editingTask && (
                <label>
                  Status
                  <select
                    value={form.status}
                    onChange={(event) =>
                      setForm({ ...form, status: Number(event.target.value) })
                    }
                  >
                    {statusLabels.map((label, index) => (
                      <option key={label} value={index}>
                        {label}
                      </option>
                    ))}
                  </select>
                </label>
              )}
              <label>
                Priorytet
                <select
                  value={form.priority}
                  onChange={(event) =>
                    setForm({ ...form, priority: Number(event.target.value) })
                  }
                >
                  {priorityLabels.map((label, index) => (
                    <option key={label} value={index}>
                      {label}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Termin
                <input
                  type="date"
                  value={form.dueDate ?? ""}
                  onChange={(event) =>
                    setForm({
                      ...form,
                      dueDate: event.target.value || undefined,
                    })
                  }
                />
              </label>
              {modalError && <div className="banner">{modalError}</div>}
              <div className="modal-actions">
                <button
                  type="button"
                  className="btn btn-outline-secondary"
                  onClick={closeModal}
                >
                  Anuluj
                </button>
                <button
                  type="button"
                  className="btn btn-primary"
                  onClick={handleSubmit}
                  disabled={saving}
                >
                  {saving
                    ? "Zapisywanie..."
                    : editingTask
                      ? "Zapisz zmiany"
                      : "Zapisz zadanie"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </>
  );
};

export default BoardsPage;
