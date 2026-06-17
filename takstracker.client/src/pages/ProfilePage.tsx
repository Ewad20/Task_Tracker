import * as React from "react";
import { api } from "../api/client";
import type { ApiError, UserProfileDto } from "../api/types";

interface ProfilePageProps {
  onProfileUpdated: (profile: UserProfileDto) => void;
}

const ProfilePage = ({ onProfileUpdated }: ProfilePageProps) => {
  const [isLoading, setIsLoading] = React.useState(true);
  const [isSaving, setIsSaving] = React.useState(false);
  const [isChangingPassword, setIsChangingPassword] = React.useState(false);
  const [message, setMessage] = React.useState<string | null>(null);
  const [error, setError] = React.useState<string | null>(null);
  const [passwordMessage, setPasswordMessage] = React.useState<string | null>(
    null,
  );
  const [passwordError, setPasswordError] = React.useState<string | null>(null);
  const [form, setForm] = React.useState({
    displayName: "",
    bio: "",
  });
  const [passwordForm, setPasswordForm] = React.useState({
    currentPassword: "",
    newPassword: "",
    confirmPassword: "",
  });

  React.useEffect(() => {
    const loadProfile = async () => {
      try {
        const profile = await api.getProfile();
        setForm({
          displayName: profile.displayName,
          bio: profile.bio ?? "",
        });
        setError(null);
      } catch (err: unknown) {
        const status = (err as ApiError)?.status;
        setError(
          status === 401
            ? "Brak aktywnej sesji. Zaloguj sie ponownie."
            : "Nie udalo sie pobrac profilu.",
        );
      } finally {
        setIsLoading(false);
      }
    };

    void loadProfile();
  }, []);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (form.displayName.trim().length < 2) {
      setError("Nazwa profilu musi miec co najmniej 2 znaki.");
      setMessage(null);
      return;
    }

    try {
      setIsSaving(true);
      const updated = await api.updateProfile({
        displayName: form.displayName.trim(),
        bio: form.bio.trim(),
      });
      onProfileUpdated(updated);
      window.dispatchEvent(new Event("auth-changed"));
      setMessage("Profil zostal zaktualizowany.");
      setError(null);
    } catch (err: unknown) {
      const apiMessage = (err as Error)?.message;
      setError(apiMessage || "Nie udalo sie zapisac zmian.");
      setMessage(null);
    } finally {
      setIsSaving(false);
    }
  };

  const handlePasswordSubmit = async (
    event: React.FormEvent<HTMLFormElement>,
  ) => {
    event.preventDefault();

    if (!passwordForm.currentPassword) {
      setPasswordError("Podaj obecne haslo.");
      setPasswordMessage(null);
      return;
    }

    if (passwordForm.newPassword.length < 8) {
      setPasswordError("Nowe haslo musi miec co najmniej 8 znakow.");
      setPasswordMessage(null);
      return;
    }

    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      setPasswordError("Nowe hasla nie sa identyczne.");
      setPasswordMessage(null);
      return;
    }

    try {
      setIsChangingPassword(true);
      await api.changePassword({
        currentPassword: passwordForm.currentPassword,
        newPassword: passwordForm.newPassword,
      });
      setPasswordForm({
        currentPassword: "",
        newPassword: "",
        confirmPassword: "",
      });
      setPasswordMessage("Haslo zostalo zmienione.");
      setPasswordError(null);
    } catch (err: unknown) {
      const apiMessage = (err as Error)?.message;
      setPasswordError(apiMessage || "Nie udalo sie zmienic hasla.");
      setPasswordMessage(null);
    } finally {
      setIsChangingPassword(false);
    }
  };

  return (
    <>
      <header className="page-header">
        <div>
          <h1>Mój profil</h1>
          <p>Edytuj nazwę wyświetlaną i opis konta</p>
        </div>
        <div className="status-pill">Profil</div>
      </header>

      <section className="profile-layout">
        <div className="card settings-card profile-card">
          <h2>Dane profilu</h2>
          {isLoading ? (
            <div className="empty-state">Ladowanie profilu...</div>
          ) : (
            <form className="form" onSubmit={handleSubmit}>
              <label>
                Nazwa wyświetlana
                <input
                  type="text"
                  value={form.displayName}
                  maxLength={120}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      displayName: event.target.value,
                    }))
                  }
                />
              </label>
              <label>
                Bio
                <textarea
                  value={form.bio}
                  maxLength={1000}
                  rows={6}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      bio: event.target.value,
                    }))
                  }
                />
              </label>
              {message && <div className="banner profile-success">{message}</div>}
              {error && <div className="banner modal-error">{error}</div>}
              <button type="submit" className="btn btn-primary" disabled={isSaving}>
                {isSaving ? "Zapisywanie..." : "Zapisz profil"}
              </button>
            </form>
          )}
        </div>
        <div className="card settings-card profile-card">
          <h2>Zmiana hasła</h2>
          <form className="form" onSubmit={handlePasswordSubmit}>
            <label>
              Obecne hasło
              <input
                type="password"
                value={passwordForm.currentPassword}
                autoComplete="current-password"
                onChange={(event) =>
                  setPasswordForm((current) => ({
                    ...current,
                    currentPassword: event.target.value,
                  }))
                }
              />
            </label>
            <label>
              Nowe hasło
              <input
                type="password"
                value={passwordForm.newPassword}
                autoComplete="new-password"
                onChange={(event) =>
                  setPasswordForm((current) => ({
                    ...current,
                    newPassword: event.target.value,
                  }))
                }
              />
            </label>
            <label>
              Powtórz nowe hasło
              <input
                type="password"
                value={passwordForm.confirmPassword}
                autoComplete="new-password"
                onChange={(event) =>
                  setPasswordForm((current) => ({
                    ...current,
                    confirmPassword: event.target.value,
                  }))
                }
              />
            </label>
            {passwordMessage && (
              <div className="banner profile-success">{passwordMessage}</div>
            )}
            {passwordError && (
              <div className="banner modal-error">{passwordError}</div>
            )}
            <button
              type="submit"
              className="btn btn-primary"
              disabled={isChangingPassword}
            >
              {isChangingPassword ? "Zapisywanie..." : "Zmień hasło"}
            </button>
          </form>
        </div>
      </section>
    </>
  );
};

export default ProfilePage;
