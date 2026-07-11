import { Component, OnInit, inject } from '@angular/core';
import { NgComponentOutlet } from '@angular/common';
import { RouterLink } from '@angular/router';
import {
  LucideBriefcase,
  LucideUsers,
  LucideList,
  LucideIcon,
} from '@lucide/angular';
import { ApiService } from '../../core/services/api.service';
import { NavigationService } from '../../core/services/navigation.service';
import { ContentComponent } from '../../shared/layout/content/content.component';
import { SystemHealthCardComponent } from './system-health-card/system-health-card.component';
import { RecentActivityListComponent } from './recent-activity-list/recent-activity-list.component';
import {
  ChartDataPoint,
  JobsPerHourChartComponent,
} from './jobs-per-hour-chart/jobs-per-hour-chart.component';
import { DashboardActivity, SystemHealth, JobStatsItem } from '../../core/models';

export interface DashboardStat {
  label: string;
  value: string | number;
  icon: LucideIcon;
  route: string;
  iconBg: string;
  iconColor: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    NgComponentOutlet,
    RouterLink,
    ContentComponent,
    SystemHealthCardComponent,
    RecentActivityListComponent,
    JobsPerHourChartComponent,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
})
export class DashboardComponent implements OnInit {
  private nav = inject(NavigationService);

  stats: DashboardStat[] = [
    {
      label: 'Total Jobs',
      value: '0',
      icon: LucideBriefcase,
      route: this.nav.routes.jobs,
      iconBg: 'var(--color-primary-light)',
      iconColor: 'var(--color-primary)',
    },
    {
      label: 'Active Workers',
      value: '0',
      icon: LucideUsers,
      route: this.nav.routes.workers,
      iconBg: 'var(--color-success-light)',
      iconColor: 'var(--color-success)',
    },
    {
      label: 'Queue Jobs',
      value: '0',
      icon: LucideList,
      route: this.nav.routes.jobs,
      iconBg: 'var(--color-warning-light)',
      iconColor: 'var(--color-warning)',
    },
  ];
  activities: DashboardActivity[] = [];
  health: SystemHealth | null = null;
  chartData: ChartDataPoint[] = [];
  loadingActivities = true;
  loadingHealth = true;
  loadingChart = true;

  constructor(private api: ApiService) {}

  ngOnInit() {
    this.api.getRecentActivity().subscribe((a) => {
      this.activities = a;
      this.loadingActivities = false;
    });

    this.api.getSystemHealth().subscribe((h) => {
      this.health = h;
      this.loadingHealth = false;
    });

    this.api.getSystemMetrics().subscribe((m) => {
      this.stats[0].value = m.totalJobs;
      this.stats[1].value = m.activeWorkers;
      this.stats[2].value = m.queuedJobs;
    });

    this.api.getRecentJobStats().subscribe((items: JobStatsItem[]) => {
      const counts = items.map(i => i.jobCount);
      const max = Math.max(...counts, 1);
      this.chartData = items.map(item => ({
        value: item.jobCount,
        height: (item.jobCount / max) * 100,
        label: item.bucketHour,
        start: item.bucketStart,
        end: item.bucketEnd,
      }));
      this.loadingChart = false;
    });
  }
}
