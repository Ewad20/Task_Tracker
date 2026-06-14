import * as React from 'react'
import type { ApiError, UserProfileDto } from '../api/types'
import { api } from '../api/client'

const formatShortId = (id: string) => `${id.slice(0, 8)}...${id.slice(-6)}`

const SettingsPage = () => {
  const [profile, setProfile] = React.useState<UserProfileDto | null>(null)
  const [users, setUsers] = React.useState<UserProfileDto[]>([])
  const [error, setError] = React.useState<string | null>(null)
  const [usersError, setUsersError] = React.useState<string | null>(null)
  const [updatingUserId, setUpdatingUserId] = React.useState<string | null>(null)
  const [roleMenuUserId, setRoleMenuUserId] = React.useState<string | null>(null)
  const [passwordUser, setPasswordUser] = React.useState<UserProfileDto | null>(null)
  const [passwordForm, setPasswordForm] = React.useState({
    newPassword: '',
    confirmPassword: '',
  })
  const [passwordMessage, setPasswordMessage] = React.useState<string | null>(null)

  const loadProfile = async () => {
    try {
      const data = await api.getProfile()
      setProfile(data)
      setError(null)
    } catch (err: unknown) {
      const status = (err as ApiError)?.status
      if (status === 401) {
        setProfile(null)
        setError('Brak aktywnej sesji. Zaloguj się.')
        return
      }
      setError('Nie udało się pobrać profilu.')
    }
  }

  React.useEffect(() => {
    if (api.getToken()) {
      loadProfile()
    }
  }, [])

  React.useEffect(() => {
    const loadUsers = async () => {
      if (profile?.role !== 'Admin') {
        setUsers([])
        setUsersError(null)
        return
      }

      try {
        const data = await api.getUsers()
        setUsers(data)
        setUsersError(null)
      } catch (err: unknown) {
        const status = (err as ApiError)?.status
        setUsersError(status === 401 ? 'Zaloguj się, aby zarządzać użytkownikami.' : 'Nie udało się pobrać użytkowników.')
      }
    }

    loadUsers()
  }, [profile?.role])

  const handleRoleChange = async (userId: string, role: string) => {
    try {
      setUpdatingUserId(userId)
      setRoleMenuUserId(null)
      const updated = await api.updateUserRole(userId, { role })
      setUsers((current) => current.map((user) => (user.userId === userId ? updated : user)))
      setUsersError(null)
    } catch (err: unknown) {
      const message = (err as Error)?.message
      setUsersError(message ? `Zmiana roli nieudana: ${message}` : 'Nie udało się zmienić roli.')
    } finally {
      setUpdatingUserId(null)
    }
  }

  const openPasswordModal = (user: UserProfileDto) => {
    setPasswordUser(user)
    setPasswordForm({ newPassword: '', confirmPassword: '' })
    setPasswordMessage(null)
  }

  const closePasswordModal = () => {
    setPasswordUser(null)
    setPasswordForm({ newPassword: '', confirmPassword: '' })
    setPasswordMessage(null)
  }

  const handlePasswordChange = async () => {
    if (!passwordUser) {
      return
    }

    if (!passwordForm.newPassword) {
      setPasswordMessage('Podaj nowe hasło.')
      return
    }

    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      setPasswordMessage('Nowe hasła nie są identyczne.')
      return
    }

    try {
      await api.resetUserPassword(passwordUser.userId, {
        newPassword: passwordForm.newPassword,
      })
      setPasswordForm({ newPassword: '', confirmPassword: '' })
      setPasswordMessage('Hasło zostało zaktualizowane.')
    } catch (err: unknown) {
      const message = (err as Error)?.message
      setPasswordMessage(message ? `Zmiana hasła nieudana: ${message}` : 'Nie udało się zmienić hasła.')
    }
  }

  return (
    <>
      <header className="page-header">
        <div>
          <h1>Ustawienia</h1>
          <p>Zarządzaj kontami użytkowników</p>
        </div>
        <div className="status-pill">Konto</div>
      </header>

      <section className="settings-grid settings-grid-single">
        {profile?.role === 'Admin' && (
          <div className="card settings-card">
              <h2>Użytkownicy</h2>
              {usersError ? (
                <div className="empty-state">{usersError}</div>
              ) : (
                <div className="users-grid">
                  {users.map((user) => (
                    <div key={user.userId} className="user-card">
                      <div className="user-card-header">
                        <div>
                          <strong>{user.displayName}</strong>
                          <span>{formatShortId(user.userId)}</span>
                        </div>
                        <div className="role-picker">
                          {user.userId !== profile.userId && <span className="role-picker-label">ZMIEŃ ROLĘ</span>}
                          <button
                            type="button"
                            className={`role-pill ${user.role === 'Admin' ? 'admin' : 'user'}`}
                            onClick={() =>
                              setRoleMenuUserId((current) => (current === user.userId ? null : user.userId))
                            }
                            disabled={updatingUserId === user.userId || user.userId === profile.userId}
                            aria-haspopup="listbox"
                            aria-expanded={roleMenuUserId === user.userId}
                          >
                            {user.role === 'Admin' ? 'Administrator' : 'Użytkownik'}
                          </button>
                          {roleMenuUserId === user.userId && (
                            <div className="role-menu" role="listbox">
                              <button
                                type="button"
                                className={user.role === 'User' ? 'active' : ''}
                                onClick={() => handleRoleChange(user.userId, 'User')}
                              >
                                Użytkownik
                              </button>
                              <button
                                type="button"
                                className={user.role === 'Admin' ? 'active' : ''}
                                onClick={() => handleRoleChange(user.userId, 'Admin')}
                              >
                                Administrator
                              </button>
                            </div>
                          )}
                        </div>
                      </div>
                      <button
                        type="button"
                        className="btn btn-outline-secondary user-password-button"
                        onClick={() => openPasswordModal(user)}
                      >
                        Zmień hasło
                      </button>
                    </div>
                  ))}
                  {users.length === 0 && <div className="empty-state">Brak użytkowników do wyświetlenia.</div>}
                </div>
              )}
            </div>
        )}
      </section>

      {passwordUser && (
        <div className="modal-backdrop" role="presentation" onClick={closePasswordModal}>
          <div
            className="modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="password-modal-title"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="modal-header">
              <h2 id="password-modal-title">Zmień hasło</h2>
              <button type="button" className="btn icon-button" onClick={closePasswordModal} aria-label="Zamknij">
                x
              </button>
            </div>
            <p className="modal-subtitle">{passwordUser.displayName}</p>
            <form className="form" onSubmit={(event) => event.preventDefault()}>
              <label>
                Nowe hasło
                <input
                  type="password"
                  value={passwordForm.newPassword}
                  onChange={(event) => setPasswordForm({ ...passwordForm, newPassword: event.target.value })}
                />
              </label>
              <label>
                Powtórz nowe hasło
                <input
                  type="password"
                  value={passwordForm.confirmPassword}
                  onChange={(event) => setPasswordForm({ ...passwordForm, confirmPassword: event.target.value })}
                />
              </label>
              {passwordMessage && <div className="empty-state">{passwordMessage}</div>}
              <div className="modal-actions">
                <button type="button" className="btn btn-outline-secondary" onClick={closePasswordModal}>
                  Anuluj
                </button>
                <button type="button" className="btn btn-primary" onClick={handlePasswordChange}>
                  Zmień hasło
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {error && <div className="banner">{error}</div>}
    </>
  )
}

export default SettingsPage
