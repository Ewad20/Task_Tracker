import * as React from "react";
import { api } from "../api/client";
import { connectNotificationStream } from "../api/notificationStream";
import type { ApiError, NotificationDto } from "../api/types";

const formatDateTime = (value: string) =>
  new Date(value).toLocaleString("pl-PL", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });

const NotificationsPage = () => {
  const [notifications, setNotifications] = React.useState<NotificationDto[]>(
    [],
  );
  const [isLoading, setIsLoading] = React.useState(true);
  const [updatingId, setUpdatingId] = React.useState<string | null>(null);
  const [error, setError] = React.useState<string | null>(null);

  React.useEffect(() => {
    const loadNotifications = async () => {
      try {
        const data = await api.getNotifications();
        setNotifications(data);
        setError(null);
      } catch (err: unknown) {
        const status = (err as ApiError)?.status;
        setError(
          status === 401
            ? "Brak aktywnej sesji. Zaloguj sie ponownie."
            : "Nie udalo sie pobrac powiadomien.",
        );
      } finally {
        setIsLoading(false);
      }
    };

    void loadNotifications();
  }, []);

  React.useEffect(() => {
    if (!api.getToken()) {
      return undefined;
    }

    return connectNotificationStream((notification) => {
      setNotifications((current) => [
        notification,
        ...current.filter((item) => item.id !== notification.id),
      ]);
    });
  }, []);

  const handleMarkRead = async (notification: NotificationDto) => {
    try {
      setUpdatingId(notification.id);
      const updated = await api.markNotificationRead(notification.id, {
        isRead: !notification.isRead,
      });
      setNotifications((current) =>
        current.map((item) => (item.id === updated.id ? updated : item)),
      );
      setError(null);
    } catch (err: unknown) {
      const apiMessage = (err as Error)?.message;
      setError(apiMessage || "Nie udalo sie zaktualizowac powiadomienia.");
    } finally {
      setUpdatingId(null);
    }
  };

  const unreadCount = notifications.filter((notification) => !notification.isRead)
    .length;

  return (
    <>
      <header className="page-header">
        <div>
          <h1>Powiadomienia</h1>
          <p>Przeglądaj zdarzenia i oznaczaj je jako przeczytane</p>
        </div>
        <div className="status-pill">
          {unreadCount > 0 ? `${unreadCount} nieprzeczytane` : "Wszystko przeczytane"}
        </div>
      </header>

      <section className="notifications-layout">
        <div className="card notifications-card">
          {isLoading ? (
            <div className="empty-state">Ladowanie powiadomien...</div>
          ) : notifications.length === 0 ? (
            <div className="empty-state">Brak powiadomien.</div>
          ) : (
            <div className="notifications-list">
              {notifications.map((notification) => (
                <article
                  key={notification.id}
                  className={`notification-item ${
                    notification.isRead ? "read" : "unread"
                  }`}
                >
                  <div className="notification-main">
                    <strong>{notification.message}</strong>
                    <span>{formatDateTime(notification.createdAt)}</span>
                  </div>
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={() => handleMarkRead(notification)}
                    disabled={updatingId === notification.id}
                  >
                    {notification.isRead
                      ? "Oznacz jako nieprzeczytane"
                      : "Oznacz jako przeczytane"}
                  </button>
                </article>
              ))}
            </div>
          )}
        </div>
      </section>

      {error && <div className="banner modal-error">{error}</div>}
    </>
  );
};

export default NotificationsPage;
