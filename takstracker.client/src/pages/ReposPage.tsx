import { useEffect, useState } from "react";
import type {
  ApiError,
  CreateProjectRequest,
  ProjectDto,
  UserProfileDto,
} from "../api/types";
import { api } from "../api/client";

const formatDate = (value: string) =>
  new Intl.DateTimeFormat("pl-PL", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  }).format(new Date(value));

const emptyProjectForm: CreateProjectRequest = {
  name: "",
  description: "",
  memberUserIds: [],
};

const ReposPage = () => {
  const [projects, setProjects] = useState<ProjectDto[]>([]);
  const [users, setUsers] = useState<UserProfileDto[]>([]);
  const [profile, setProfile] = useState<UserProfileDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState<CreateProjectRequest>(emptyProjectForm);
  const [editingProject, setEditingProject] = useState<ProjectDto | null>(null);
  const [saving, setSaving] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [modalError, setModalError] = useState<string | null>(null);

  const loadProjects = () =>
    Promise.all([api.getProjects(), api.getUsers(), api.getProfile()])
      .then(([projectData, userData, profileData]) => {
        setProjects(projectData);
        setUsers(userData);
        setProfile(profileData);
        setError(null);
      })
      .catch((err: unknown) => {
        const status = (err as ApiError)?.status;
        setError(
          status === 401
            ? "Zaloguj się, aby pobrać projekty."
            : "Nie udało się pobrać projektów.",
        );
      });

  useEffect(() => {
    loadProjects();
  }, []);

  const getUserName = (userId: string) =>
    users.find((user) => user.userId === userId)?.displayName ?? userId;

  const isAdmin = profile?.role === "Admin";

  const toggleMember = (userId: string) => {
    setForm((current) => ({
      ...current,
      memberUserIds: current.memberUserIds.includes(userId)
        ? current.memberUserIds.filter((id) => id !== userId)
        : [...current.memberUserIds, userId],
    }));
  };

  const openCreateModal = () => {
    setEditingProject(null);
    setForm(emptyProjectForm);
    setIsModalOpen(true);
  };

  const openEditModal = (project: ProjectDto) => {
    setEditingProject(project);
    setForm({
      name: project.name,
      description: project.description,
      memberUserIds: project.members.map((member) => member.userId),
    });
    setIsModalOpen(true);
  };

  const closeModal = () => {
    setIsModalOpen(false);
    setEditingProject(null);
    setForm(emptyProjectForm);
    setModalError(null);
  };

  const handleSubmit = async () => {
    if (!form.name.trim()) {
      setError("Nazwa projektu jest wymagana.");
      return;
    }

    try {
      setSaving(true);
      if (editingProject) {
        await api.updateProject(editingProject.id, form);
      } else {
        await api.createProject(form);
      }
      closeModal();
      await loadProjects();
      window.dispatchEvent(new Event("projects-changed"));
    } catch (err: unknown) {
      const apiError = err as ApiError;
      const status = apiError?.status;
      setModalError(
        !status
          ? "Brak połączenia z serwerem."
          : status === 401
            ? "Zaloguj się, aby zapisać projekt."
            : apiError?.message
              ? apiError.message
              : editingProject
                ? "Nie udało się zaktualizować projektu."
                : "Nie udało się dodać projektu.",
      );
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (project: ProjectDto) => {
    if (!window.confirm(`Usunąć projekt "${project.name}"?`)) {
      return;
    }

    try {
      await api.deleteProject(project.id);
      await loadProjects();
      window.dispatchEvent(new Event("projects-changed"));
    } catch (err: unknown) {
      const status = (err as ApiError)?.status;
      setError(
        status === 401
          ? "Zaloguj się, aby usunąć projekt."
          : "Nie udało się usunąć projektu.",
      );
    }
  };

  return (
    <>
      <header className="page-header">
        <div>
          <h1>Projekty</h1>
          <p>Lista projektów jako repozytoria kodu.</p>
        </div>
        {isAdmin && (
          <button
            type="button"
            className="btn btn-primary primary-action"
            onClick={openCreateModal}
          >
            Dodaj projekt
          </button>
        )}
      </header>

      <section className="workspace repos-workspace">
        <div className="card work wide-panel repos-panel">
          <div className="repos-panel-header">
            <div>
              <h2>Repozytoria</h2>
              <span>
                {projects.length === 1
                  ? "1 aktywny projekt"
                  : `${projects.length} aktywne projekty`}
              </span>
            </div>
          </div>

          {error ? (
            <div className="empty-state repos-empty">{error}</div>
          ) : (
            <div className="repo-grid">
              {projects.map((project) => (
                <article key={project.id} className="repo-card">
                  <div className="repo-card-main">
                    <div className="repo-avatar" aria-hidden="true">
                      {project.name.trim().slice(0, 2).toUpperCase()}
                    </div>
                    <div>
                      <h3>{project.name}</h3>
                      <p>{project.description || "Brak opisu"}</p>
                    </div>
                  </div>
                  <div className="repo-meta">
                    <span>
                      Właściciel:{" "}
                      {project.ownerId ? getUserName(project.ownerId) : "brak"}
                    </span>
                    <span>Członkowie: {project.members.length}</span>
                    <span>Utworzono: {formatDate(project.createdAt)}</span>
                  </div>
                  {isAdmin && (
                    <div className="item-actions">
                      <button
                        type="button"
                        className="btn btn-outline-secondary"
                        onClick={() => openEditModal(project)}
                      >
                        Edytuj
                      </button>
                      <button
                        type="button"
                        className="btn btn-outline-danger"
                        onClick={() => handleDelete(project)}
                      >
                        Usuń
                      </button>
                    </div>
                  )}
                </article>
              ))}
              {projects.length === 0 && (
                <div className="empty-state repos-empty">
                  <strong>Brak projektów</strong>
                  <span>
                    {isAdmin
                      ? "Dodaj projekt w module Projekty."
                      : "Nie przypisano Cię jeszcze do żadnego projektu."}
                  </span>
                </div>
              )}
            </div>
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
            aria-labelledby="project-modal-title"
            onMouseDown={(event) => event.stopPropagation()}
          >
            <div className="modal-header">
              <h2 id="project-modal-title">
                {editingProject ? "Edytuj projekt" : "Dodaj projekt"}
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
                Nazwa
                <input
                  type="text"
                  value={form.name}
                  onChange={(event) =>
                    setForm({ ...form, name: event.target.value })
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
              <fieldset className="member-picker">
                <legend>Dostęp użytkowników</legend>
                <div className="member-list">
                  {users.map((user) => (
                    <label key={user.userId} className="member-option">
                      <input
                        type="checkbox"
                        checked={form.memberUserIds.includes(user.userId)}
                        onChange={() => toggleMember(user.userId)}
                      />
                      <span>
                        <strong>{user.displayName}</strong>
                        <small>
                          {user.role === "Admin"
                            ? "Administrator"
                            : "Użytkownik"}
                        </small>
                      </span>
                    </label>
                  ))}
                </div>
              </fieldset>
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
                    : editingProject
                      ? "Zapisz zmiany"
                      : "Zapisz projekt"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </>
  );
};

export default ReposPage;
