import { JobStatus } from './job.model';

export enum ActivityStatus {
  Success = 'success',
  Error = 'error',
  Info = 'info',
}

export enum HealthStatus {
  Healthy = 'healthy',
  Unhealthy = 'unhealthy',
  Degraded = 'degraded',
}

export interface ComponentHealth {
  status: HealthStatus;
}

export interface DashboardActivity {
  type: JobStatus;
  message: string;
  timestamp: string;
  status: ActivityStatus;
}

export interface SystemHealth {
  api: ComponentHealth;
  database: ComponentHealth;
  workers: ComponentHealth;
}

export interface SystemMetrics {
  activeWorkers: number;
  totalJobs: number;
  queuedJobs: number;
}

export interface JobStatsItem {
  bucketStart: string;
  bucketEnd: string;
  bucketHour: string;
  jobCount: number;
}
