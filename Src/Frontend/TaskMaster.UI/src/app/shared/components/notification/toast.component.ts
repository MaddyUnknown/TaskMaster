import { Component } from '@angular/core';
import { LucideCircleCheck, LucideCircleX, LucideTriangleAlert, LucideInfo } from '@lucide/angular';
import { NotificationService, Notification } from '../../../core/services/notification.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [LucideCircleCheck, LucideCircleX, LucideTriangleAlert, LucideInfo],
  templateUrl: './toast.component.html',
  styleUrl: './toast.component.css'
})
export class ToastComponent {
  constructor(public notificationService: NotificationService) {}
}
