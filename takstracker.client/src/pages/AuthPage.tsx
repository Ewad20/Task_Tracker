import * as React from 'react'
import type { ApiError, LoginRequest, RegisterRequest } from '../api/types'
import { api } from '../api/client'

interface AuthPageProps {
  onAuthSuccess: () => Promise<void> | void
}

const AuthPage = ({ onAuthSuccess }: AuthPageProps) => {
  const [login, setLogin] = React.useState<LoginRequest>({ email: '', password: '' })
  const [register, setRegister] = React.useState<RegisterRequest>({
    email: '',
    password: '',
    displayName: '',
  })
  const [error, setError] = React.useState<string | null>(null)
  const [loading, setLoading] = React.useState(false)
  const [isRegisterOpen, setIsRegisterOpen] = React.useState(false)

  const handleLogin = async () => {
    try {
      setLoading(true)
      const response = await api.login(login)
      api.setToken(response.token)
      await onAuthSuccess()
      setError(null)
      setLogin({ email: '', password: '' })
    } catch (err: unknown) {
      const message = (err as ApiError)?.message
      setError(message ? `Logowanie nieudane: ${message}` : 'Logowanie nieudane.')
    } finally {
      setLoading(false)
    }
  }

  const handleRegister = async () => {
    try {
      setLoading(true)
      const response = await api.register(register)
      api.setToken(response.token)
      await onAuthSuccess()
      setError(null)
      setRegister({ email: '', password: '', displayName: '' })
    } catch (err: unknown) {
      const message = (err as ApiError)?.message
      setError(message ? `Rejestracja nieudana: ${message}` : 'Rejestracja nieudana.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-shell">
      <div className="auth-header">
        <div className="auth-logo" aria-hidden="true">
          <span className="auth-logo-check"></span>
        </div>
        <h1>TaskTracker</h1>
        <p>Zaloguj się, aby przejść dalej</p>
      </div>
      <div className="card auth-card">
        <h2>Logowanie</h2>
        <form className="form" onSubmit={(event) => event.preventDefault()}>
          <label>
            E-mail
            <input
              type="email"
              value={login.email}
              onChange={(event) => setLogin({ ...login, email: event.target.value })}
            />
          </label>
          <label>
            Hasło
            <input
              type="password"
              value={login.password}
              onChange={(event) => setLogin({ ...login, password: event.target.value })}
            />
          </label>
          <button type="button" className="btn btn-primary" onClick={handleLogin} disabled={loading}>
            {loading ? 'Logowanie...' : 'Zaloguj'}
          </button>
        </form>
        <div className="auth-register-hint">
          <span>Nie masz konta?</span>
          <button type="button" className="link-button" onClick={() => setIsRegisterOpen(true)}>
            Zarejestruj się
          </button>
        </div>
      </div>
      {error && <div className="banner">{error}</div>}

      {isRegisterOpen && (
        <div className="modal-backdrop" role="presentation" onClick={() => setIsRegisterOpen(false)}>
          <div
            className="modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="register-modal-title"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="modal-header">
              <h2 id="register-modal-title">Rejestracja</h2>
              <button
                type="button"
                className="btn icon-button"
                onClick={() => setIsRegisterOpen(false)}
                aria-label="Zamknij"
              >
                x
              </button>
            </div>
            <form className="form" onSubmit={(event) => event.preventDefault()}>
              <label>
                E-mail
                <input
                  type="email"
                  value={register.email}
                  onChange={(event) => setRegister({ ...register, email: event.target.value })}
                />
              </label>
              <label>
                Hasło
                <input
                  type="password"
                  value={register.password}
                  onChange={(event) => setRegister({ ...register, password: event.target.value })}
                />
              </label>
              <label>
                Nazwa wyświetlana
                <input
                  type="text"
                  value={register.displayName}
                  onChange={(event) => setRegister({ ...register, displayName: event.target.value })}
                />
              </label>
              <div className="modal-actions">
                <button type="button" className="btn btn-outline-secondary" onClick={() => setIsRegisterOpen(false)}>
                  Anuluj
                </button>
                <button type="button" className="btn btn-primary" onClick={handleRegister} disabled={loading}>
                  {loading ? 'Rejestracja...' : 'Zarejestruj'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}

export default AuthPage
