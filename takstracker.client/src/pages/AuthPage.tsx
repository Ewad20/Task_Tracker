import * as React from "react";
import type { ApiError, LoginRequest, RegisterRequest } from "../api/types";
import { api } from "../api/client";

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

interface AuthPageProps {
  onAuthSuccess: () => Promise<void> | void;
}

const AuthPage = ({ onAuthSuccess }: AuthPageProps) => {
  const [login, setLogin] = React.useState<LoginRequest>({
    email: "",
    password: "",
  });
  const [register, setRegister] = React.useState<RegisterRequest>({
    email: "",
    password: "",
    displayName: "",
  });
  const [loginError, setLoginError] = React.useState<string | null>(null);
  const [registerError, setRegisterError] = React.useState<string | null>(null);
  const [loading, setLoading] = React.useState(false);
  const [isRegisterOpen, setIsRegisterOpen] = React.useState(false);

  const toErrorMessage = (err: unknown, prefix: string): string => {
    const apiError = err as ApiError;
    if (!apiError?.status) {
      return `${prefix}: brak połączenia z serwerem.`;
    }
    return apiError.message ? `${prefix}: ${apiError.message}` : `${prefix}.`;
  };

  const validateRegister = (payload: RegisterRequest): string | null => {
    const email = payload.email.trim();
    const displayName = payload.displayName.trim();

    if (!email) {
      return "Rejestracja nieudana: podaj adres e-mail.";
    }

    if (!emailPattern.test(email)) {
      return "Rejestracja nieudana: podaj poprawny adres e-mail.";
    }

    if (payload.password.length < 8) {
      return "Rejestracja nieudana: hasło musi mieć co najmniej 8 znaków.";
    }

    if (displayName.length < 2 || displayName.length > 120) {
      return "Rejestracja nieudana: nazwa wyświetlana musi mieć od 2 do 120 znaków.";
    }

    return null;
  };

  const handleLogin = async () => {
    setLoginError(null);
    try {
      setLoading(true);
      const response = await api.login(login);
      api.setToken(response.token);
      await onAuthSuccess();
      setLogin({ email: "", password: "" });
    } catch (err: unknown) {
      setLoginError(toErrorMessage(err, "Logowanie nieudane"));
    } finally {
      setLoading(false);
    }
  };

  const handleRegister = async () => {
    setRegisterError(null);

    const normalizedRegister: RegisterRequest = {
      email: register.email.trim(),
      password: register.password,
      displayName: register.displayName.trim(),
    };

    const validationError = validateRegister(normalizedRegister);
    if (validationError) {
      setRegisterError(validationError);
      return;
    }

    try {
      setLoading(true);
      const response = await api.register(normalizedRegister);
      api.setToken(response.token);
      await onAuthSuccess();
      setRegister({ email: "", password: "", displayName: "" });
    } catch (err: unknown) {
      setRegisterError(toErrorMessage(err, "Rejestracja nieudana"));
    } finally {
      setLoading(false);
    }
  };

  const closeRegisterModal = () => {
    setIsRegisterOpen(false);
    setRegisterError(null);
  };

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
              onChange={(event) =>
                setLogin({ ...login, email: event.target.value })
              }
            />
          </label>
          <label>
            Hasło
            <input
              type="password"
              value={login.password}
              onChange={(event) =>
                setLogin({ ...login, password: event.target.value })
              }
            />
          </label>
          {loginError && <div className="banner">{loginError}</div>}
          <button
            type="button"
            className="btn btn-primary"
            onClick={handleLogin}
            disabled={loading}
          >
            {loading ? "Logowanie..." : "Zaloguj"}
          </button>
        </form>
        <div className="auth-register-hint">
          <span>Nie masz konta?</span>
          <button
            type="button"
            className="link-button"
            onClick={() => setIsRegisterOpen(true)}
          >
            Zarejestruj się
          </button>
        </div>
      </div>

      {isRegisterOpen && (
        <div
          className="modal-backdrop"
          role="presentation"
          onMouseDown={closeRegisterModal}
        >
          <div
            className="modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="register-modal-title"
            onMouseDown={(event) => event.stopPropagation()}
          >
            <div className="modal-header">
              <h2 id="register-modal-title">Rejestracja</h2>
              <button
                type="button"
                className="btn icon-button"
                onClick={closeRegisterModal}
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
                  onChange={(event) =>
                    setRegister({ ...register, email: event.target.value })
                  }
                />
              </label>
              <label>
                Hasło
                <input
                  type="password"
                  value={register.password}
                  onChange={(event) =>
                    setRegister({ ...register, password: event.target.value })
                  }
                />
              </label>
              <label>
                Nazwa wyświetlana
                <input
                  type="text"
                  value={register.displayName}
                  onChange={(event) =>
                    setRegister({
                      ...register,
                      displayName: event.target.value,
                    })
                  }
                />
              </label>
              {registerError && <div className="banner">{registerError}</div>}
              <div className="modal-actions">
                <button
                  type="button"
                  className="btn btn-outline-secondary"
                  onClick={closeRegisterModal}
                >
                  Anuluj
                </button>
                <button
                  type="button"
                  className="btn btn-primary"
                  onClick={handleRegister}
                  disabled={loading}
                >
                  {loading ? "Rejestracja..." : "Zarejestruj"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default AuthPage;
