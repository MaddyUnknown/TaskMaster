import { Injectable } from '@angular/core';

export interface Notification {
  id: string;
  type: 'success' | 'error' | 'info' | 'warning';
  title: string;
  message: string;
  timestamp: Date;
  read: boolean;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private counter = 0;
  notifications: Notification[] = [];
  unreadCount = 0;

  add(type: Notification['type'], title: string, message: string) {
    const notif: Notification = {
      id: `notif-${++this.counter}`,
      type,
      title,
      message,
      timestamp: new Date(),
      read: false
    };
    this.notifications = [notif, ...this.notifications];
    this.unreadCount++;

    setTimeout(() => this.remove(notif.id), 5000);
  }

  remove(id: string) {
    this.notifications = this.notifications.filter(x => x.id !== id);
  }

  markAllRead() {
    this.notifications = this.notifications.map(x => ({ ...x, read: true }));
    this.unreadCount = 0;
  }
}
