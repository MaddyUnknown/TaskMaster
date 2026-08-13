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

export enum EntityType {
  Job = 'job',
  Worker = 'worker',
}

export enum ActivityType {
  JobCreated = 'job-created',
  JobAssigned = 'job-assigned',
  JobCompleted = 'job-completed',
  JobFailed = 'job-failed',
  WorkerRegistered = 'worker-registered',
  WorkerInactive = 'worker-inactive',
  WorkerRemoved = 'worker-removed',
}

export interface ComponentHealth {
  status: HealthStatus;
}

export interface DashboardActivity {
  entityType: EntityType;
  entityId: string;
  activityType: ActivityType;
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
