import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, map, catchError, of } from 'rxjs';
import {
  Job,
  JobStatus,
  Worker,
  WorkerStatus,
  JobType,
  CreateJobRequest,
  CreateJobTypeRequest,
  ActivityStatus,
  DashboardActivity,
  JobStatsItem,
  HealthStatus,
  SystemHealth,
  SystemMetrics,
} from '../models';

interface ApiResponse<T> {
  isSuccess: boolean;
  data: T;
  errorMessages: string[];
}

interface BackendJobTypeRef {
  name: string;
  version: number;
}

interface BackendJob {
  jobId: string;
  jobType: BackendJobTypeRef;
  payload: string;
  status: JobStatus;
  assignedWorker?: BackendWorkerRef;
  createdDateTime: string;
  modifyDateTime?: string;
  completedDateTime?: string;
  failDateTime?: string;
}

interface BackendWorker {
  workerId: string;
  workerName: string;
  status: WorkerStatus;
  lastHeartBeatTimestamp: string;
  createdDateTime: string;
  jobTypeCapabilities: BackendJobTypeRef[];
}

interface BackendWorkerRef {
  workerId: string;
  workerName: string;
}

interface BackendJobType {
  name: string;
  version: number;
  schema: string;
  description: string;
  createdDateTime: string;
  modifyDateTime?: string;
}

interface BackendMetrics {
  activeWorkers: number;
  totalJobs: number;
  queuedJobs: number;
}

interface BackendActivityItem {
  type: JobStatus;
  message: string;
  timestamp: string;
  status: ActivityStatus;
}

interface BackendComponentHealth {
  status: HealthStatus;
}

interface BackendSystemHealth {
  api: BackendComponentHealth;
  database: BackendComponentHealth;
  workers: BackendComponentHealth;
}

interface BackendJobStatsItem {
  bucketStart: string;
  bucketEnd: string;
  bucketHour: string;
  jobCount: number;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private baseUrl = '/api';

  constructor(private http: HttpClient) {}

  getJobs(): Observable<Job[]> {
    return this.http
      .get<ApiResponse<BackendJob[]>>(`${this.baseUrl}/jobs`)
      .pipe(
        map((res) =>
          res.data.map((j) => ({
            id: j.jobId,
            jobTypeName: j.jobType.name,
            jobTypeVersion: j.jobType.version,
            payload: j.payload,
            status: j.status,
            workerId: j.assignedWorker?.workerId,
            workerName: j.assignedWorker?.workerName,
            createdDateTime: j.createdDateTime,
            modifyDateTime: j.modifyDateTime,
            completedDateTime: j.completedDateTime,
            failDateTime: j.failDateTime,
          })),
        ),
      );
  }

  getJob(id: string): Observable<Job | undefined> {
    return this.http
      .get<ApiResponse<BackendJob>>(`${this.baseUrl}/jobs/${id}`)
      .pipe(
        map((res) => {
          const j = res.data;
          return {
            id: j.jobId,
            jobTypeName: j.jobType.name,
            jobTypeVersion: j.jobType.version,
            payload: j.payload,
            status: j.status,
            workerId: j.assignedWorker?.workerId,
            workerName: j.assignedWorker?.workerName,
            createdDateTime: j.createdDateTime,
            modifyDateTime: j.modifyDateTime,
            completedDateTime: j.completedDateTime,
            failDateTime: j.failDateTime,
          };
        }),
        catchError((err: HttpErrorResponse) => {
          if (err.status === 404) return of(undefined);
          throw err;
        }),
      );
  }

  createJob(req: CreateJobRequest): Observable<Job> {
    const body = {
      jobType: { name: req.jobTypeName, version: req.jobTypeVersion },
      payload: req.payload,
    };
    return this.http
      .post<ApiResponse<BackendJob>>(`${this.baseUrl}/jobs`, body)
      .pipe(
        map((res) => {
          const j = res.data;
          return {
            id: j.jobId,
            jobTypeName: j.jobType.name,
            jobTypeVersion: j.jobType.version,
            payload: j.payload,
            status: j.status,
            workerId: j.assignedWorker?.workerId,
            workerName: j.assignedWorker?.workerName,
            createdDateTime: j.createdDateTime,
            modifyDateTime: j.modifyDateTime,
            completedDateTime: j.completedDateTime,
            failDateTime: j.failDateTime,
          };
        }),
      );
  }

  getWorkers(): Observable<Worker[]> {
    return this.http
      .get<ApiResponse<BackendWorker[]>>(`${this.baseUrl}/workers`)
      .pipe(
        map((res) =>
          res.data.map((w) => ({
            id: w.workerId,
            name: w.workerName,
            status: w.status,
            lastHeartbeatTimestamp: w.lastHeartBeatTimestamp,
            createdDateTime: w.createdDateTime,
            capabilities: w.jobTypeCapabilities.map((c) => ({
              jobTypeId: c.name,
              jobTypeName: c.name,
              jobTypeVersion: c.version,
            })),
          })),
        ),
      );
  }

  getWorker(id: string): Observable<Worker | undefined> {
    return this.http
      .get<ApiResponse<BackendWorker>>(`${this.baseUrl}/workers/${id}`)
      .pipe(
        map((res) => {
          const w = res.data;
          return {
            id: w.workerId,
            name: w.workerName,
            status: w.status,
            lastHeartbeatTimestamp: w.lastHeartBeatTimestamp,
            createdDateTime: w.createdDateTime,
            capabilities: w.jobTypeCapabilities
              .map((c) => ({
                jobTypeId: c.name,
                jobTypeName: c.name,
                jobTypeVersion: c.version,
              }))
              .sort((a, b) => {
                const nameCompare = a.jobTypeName.localeCompare(b.jobTypeName);
                if (nameCompare !== 0) {
                  return nameCompare;
                }

                return a.jobTypeVersion - b.jobTypeVersion;
              }),
          };
        }),
        catchError((err: HttpErrorResponse) => {
          if (err.status === 404) return of(undefined);
          throw err;
        }),
      );
  }

  getJobTypes(): Observable<JobType[]> {
    return this.http
      .get<ApiResponse<BackendJobType[]>>(`${this.baseUrl}/job-types`)
      .pipe(
        map((res) => {
          return res.data.map((j) => ({
            id: j.name,
            name: j.name,
            version: j.version,
            description: j.description,
            schema: j.schema,
            createdDateTime: j.createdDateTime,
            modifyDateTime: j.modifyDateTime,
          }));
        }),
      );
  }

  createJobType(data: CreateJobTypeRequest): Observable<JobType> {
    return this.http
      .post<ApiResponse<BackendJobType>>(`${this.baseUrl}/job-types`, data)
      .pipe(
        map((res) => ({
          id: res.data.name,
          name: res.data.name,
          version: res.data.version,
          description: res.data.description,
          schema: res.data.schema,
          createdDateTime: res.data.createdDateTime,
          modifyDateTime: res.data.modifyDateTime,
        })),
      );
  }

  getRecentActivity(): Observable<DashboardActivity[]> {
    return this.http
      .get<
        ApiResponse<BackendActivityItem[]>
      >(`${this.baseUrl}/dashboard/activity`)
      .pipe(
        map((res) =>
          res.data.map((a) => ({
            type: a.type,
            message: a.message,
            timestamp: a.timestamp,
            status: a.status,
          })),
        ),
      );
  }

  getRecentJobStats(): Observable<JobStatsItem[]> {
    return this.http
      .get<ApiResponse<BackendJobStatsItem[]>>(`${this.baseUrl}/dashboard/job-stats`)
      .pipe(map((res) => res.data));
  }

  getSystemMetrics(): Observable<SystemMetrics> {
    return this.http
      .get<ApiResponse<BackendMetrics>>(`${this.baseUrl}/dashboard/metrics`)
      .pipe(
        map((res) => ({
          activeWorkers: res.data.activeWorkers,
          queuedJobs: res.data.queuedJobs,
          totalJobs: res.data.totalJobs,
        })),
      );
  }

  getSystemHealth(): Observable<SystemHealth> {
    return this.http
      .get<ApiResponse<BackendSystemHealth>>(`${this.baseUrl}/dashboard/health`)
      .pipe(
        map((res) => ({
          api: { status: res.data.api.status },
          database: { status: res.data.database.status },
          workers: { status: res.data.workers.status },
        })),
      );
  }
}
