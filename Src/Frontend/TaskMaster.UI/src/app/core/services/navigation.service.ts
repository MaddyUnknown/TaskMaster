import { Injectable } from '@angular/core';
import {
  LucideLayoutDashboard,
  LucideFileText,
  LucideUsers,
  LucideClipboardList,
  LucideIcon,
} from '@lucide/angular';

export interface NavItem {
  label: string;
  route: string;
  icon: LucideIcon;
  badge?: number;
}

@Injectable({ providedIn: 'root' })
export class NavigationService {
  readonly routes = {
    dashboard: '/dashboard',
    jobs: '/jobs',
    workers: '/workers',
    jobTypes: '/job-types',
  } as const;

  items: NavItem[] = [
    {
      label: 'Dashboard',
      route: this.routes.dashboard,
      icon: LucideLayoutDashboard,
    },
    { label: 'Jobs', route: this.routes.jobs, icon: LucideFileText },
    { label: 'Workers', route: this.routes.workers, icon: LucideUsers },
    {
      label: 'Job Types',
      route: this.routes.jobTypes,
      icon: LucideClipboardList,
    },
  ];
}
