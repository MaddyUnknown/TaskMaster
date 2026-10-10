import { Component, OnInit, OnDestroy, Inject } from '@angular/core';
import { of, Subject } from 'rxjs';
import { LucideInfo, LucideLogIn, LucideLogOut } from '@lucide/angular';
import { DialogComponent } from '../../components/dialog/dialog.component';
import { AUTH_SERVICE, AuthService } from '../../../core/auth/auth.service';
import { AuthConfigService } from '../../../core/auth/auth-config.service';
import { AppConfigService } from '../../../core/services/app-config.service';
import { SystemInfoService } from '../../../core/services/system-info.service';
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
    private appConfigService: AppConfigService,
    private systemInfoService: SystemInfoService,
  ) {}

  ngOnInit() {
    this.authEnabled = this.authConfigService.isAuthEnabled;
    this.isAuthenticated$ = this.authService.isAuthenticated$;
  }

  get version(): string {
    return this.systemInfoService.current.version;
  }

  get environment(): string {
    return this.systemInfoService.current.environment;
  }

  get apiEndpoint(): string {
    return this.appConfigService.apiEndpoint;
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
