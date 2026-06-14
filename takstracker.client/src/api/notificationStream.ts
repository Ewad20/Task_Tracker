import * as signalR from '@microsoft/signalr'
import { api } from './client'
import type { NotificationDto } from './types'

export const connectNotificationStream = (
  onNotification: (notification: NotificationDto) => void,
) => {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${api.getApiBase()}/hubs/notifications`, {
      accessTokenFactory: () => api.getToken() ?? '',
    })
    .configureLogging(signalR.LogLevel.None)
    .withAutomaticReconnect()
    .build()

  connection.on('notification', (notification: NotificationDto) => {
    onNotification(notification)
  })

  connection.start().catch(() => {
    // The initial page data still loads through REST; reconnect handles later availability
  })

  return () => {
    connection.stop().catch(() => undefined)
  }
}
