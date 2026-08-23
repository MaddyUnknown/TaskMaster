import { Component, OnInit, OnDestroy, Inject } from '@angular/core';
import { of, Subject } from 'rxjs';
import { LucideInfo, LucideLogIn, LucideLogOut } from '@lucide/angular';
import { DialogComponent } from '../../components/dialog/dialog.component';
import { AUTH_SERVICE, AuthService } from '../../../core/auth/auth.service';
import { AuthConfigService } from '../../../core/auth/auth-config.service';
import { AsyncPipe } from '@angular/common';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [AsyncPipe, LucideInfo, LucideLogIn, LucideLogOut, DialogComponent],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css',
})
export class HeaderComponent implements OnInit, OnDestroy {
  showSystemInfo = false;
  authEnabled = false;
  isAuthenticated$ = of(false);

  private destroy$ = new Subject<void>();

  constructor(
    @Inject(AUTH_SERVICE) private authService: AuthService,
    private authConfigService: AuthConfigService,
  ) {}

  ngOnInit() {
    this.authEnabled = this.authConfigService.isAuthEnabled;
    this.isAuthenticated$ = this.authService.isAuthenticated$;
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  login() {
    this.authService.login();
  }

  logout() {
    this.authService.logout();
  }
}
