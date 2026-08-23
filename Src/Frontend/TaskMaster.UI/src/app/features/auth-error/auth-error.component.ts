import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAlertCircle, LucideRefreshCcw } from '@lucide/angular';
import { AUTH_SERVICE } from '../../core/auth/auth.service';

@Component({
  selector: 'app-auth-error',
  standalone: true,
  imports: [RouterLink, LucideAlertCircle, LucideRefreshCcw],
  templateUrl: './auth-error.component.html',
  styleUrl: './auth-error.component.css',
})
export class AuthErrorComponent {
  private readonly authService = inject(AUTH_SERVICE);

  readonly errorMessage = this.authService.lastAuthError;
}
