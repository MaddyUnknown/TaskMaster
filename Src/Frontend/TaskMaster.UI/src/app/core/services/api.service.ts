import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable, map, catchError, of, forkJoin } from 'rxjs';
import {
  Job,
  JobStatus,
  Worker,
  WorkerStatus,
  JobType,
  CreateJobRequest,
  CreateJobTypeRequest,
  ActivityStatus,
  ActivityType,
  DashboardActivity,
  EntityType,
  JobStatsItem,
  HealthStatus,
  SystemHealth,
  SystemMetrics,
  PagedResult,
  JobQuery,
  WorkerQuery,
  JobCounts,
  WorkerCounts,
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
  entityType: EntityType;
  entityId: string;
  activityType: ActivityType;
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

interface BackendPagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

interface BackendJobCounts {
  queued: number;
  inProgress: number;
  completed: number;
  failed: number;
  total: number;
}

interface BackendWorkerCounts {
  active: number;
  inactive: number;
  total: number;
}

interface JobPage {
  page: PagedResult<Job>;
  counts: JobCounts;
}

interface WorkerPage {
  page: PagedResult<Worker>;
  counts: WorkerCounts;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private baseUrl = '/api';

  constructor(private http: HttpClient) {}

  private mapJob(j: BackendJob): Job {
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
  }

  private mapJobStatusForQuery(status?: JobStatus): string | undefined {
    const map: Record<string, string> = {
      [JobStatus.InProgress]: 'InProgress',
    };
    return status ? (map[status] ?? status) : undefined;
  }

  private mapWorkerStatusForQuery(status?: WorkerStatus): string | undefined {
    const map: Record<string, string> = {
      [WorkerStatus.Inactive]: 'InActive',
    };
    return status ? (map[status] ?? status) : undefined;
  }

  getJobs(query: JobQuery = {}): Observable<PagedResult<Job>> {
    let params = new HttpParams();
    if (query.page != null) params = params.set('page', query.page);
    if (query.pageSize != null) params = params.set('pageSize', query.pageSize);
    const status = this.mapJobStatusForQuery(query.status);
    if (status != null) params = params.set('status', status);

    return this.http
      .get<ApiResponse<BackendPagedResult<BackendJob>>>(`${this.baseUrl}/jobs`, { params })
      .pipe(
        map((res) => ({
          items: res.data.items.map((j) => this.mapJob(j)),
          page: res.data.page,
          pageSize: res.data.pageSize,
          totalCount: res.data.totalCount,
          totalPages: res.data.totalPages,
          hasPreviousPage: res.data.hasPreviousPage,
          hasNextPage: res.data.hasNextPage,
        })),
      );
  }

  getJobCounts(): Observable<JobCounts> {
    return this.http
      .get<ApiResponse<BackendJobCounts>>(`${this.baseUrl}/jobs/counts`)
      .pipe(map((res) => res.data));
  }

  getJobsWithCounts(query: JobQuery = {}): Observable<JobPage> {
    return forkJoin({
      page: this.getJobs(query),
      counts: this.getJobCounts(),
    });
  }

  getJob(id: string): Observable<Job | undefined> {
    return this.http
      .get<ApiResponse<BackendJob>>(`${this.baseUrl}/jobs/${id}`)
      .pipe(
        map((res) => this.mapJob(res.data)),
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
      .pipe(map((res) => this.mapJob(res.data)));
  }

  private mapWorker(w: BackendWorker): Worker {
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
  }

  getWorkers(query: WorkerQuery = {}): Observable<PagedResult<Worker>> {
    let params = new HttpParams();
    if (query.page != null) params = params.set('page', query.page);
    if (query.pageSize != null) params = params.set('pageSize', query.pageSize);
    const status = this.mapWorkerStatusForQuery(query.status);
    if (status != null) params = params.set('status', status);

    return this.http
      .get<ApiResponse<BackendPagedResult<BackendWorker>>>(`${this.baseUrl}/workers`, { params })
      .pipe(
        map((res) => ({
          items: res.data.items.map((w) => this.mapWorker(w)),
          page: res.data.page,
          pageSize: res.data.pageSize,
          totalCount: res.data.totalCount,
          totalPages: res.data.totalPages,
          hasPreviousPage: res.data.hasPreviousPage,
          hasNextPage: res.data.hasNextPage,
        })),
      );
  }

  getWorkerCounts(): Observable<WorkerCounts> {
    return this.http
      .get<ApiResponse<BackendWorkerCounts>>(`${this.baseUrl}/workers/counts`)
      .pipe(map((res) => res.data));
  }

  getWorkersWithCounts(query: WorkerQuery = {}): Observable<WorkerPage> {
    return forkJoin({
      page: this.getWorkers(query),
      counts: this.getWorkerCounts(),
    });
  }

  getWorker(id: string): Observable<Worker | undefined> {
    return this.http
      .get<ApiResponse<BackendWorker>>(`${this.baseUrl}/workers/${id}`)
      .pipe(
        map((res) => this.mapWorker(res.data)),
        catchError((err: HttpErrorResponse) => {
          if (err.status === 404) return of(undefined);
          throw err;
        }),
      );
  }

  getJobTypes(): Observable<JobType[]> {
    return this.http
      .get<ApiResponse<BackendPagedResult<BackendJobType>>>(`${this.baseUrl}/job-types`)
      .pipe(
        map((res) =>
          res.data.items.map((j) => ({
            id: j.name,
            name: j.name,
            version: j.version,
            description: j.description,
            schema: j.schema,
            createdDateTime: j.createdDateTime,
            modifyDateTime: j.modifyDateTime,
          })),
        ),
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
            entityType: a.entityType,
            entityId: a.entityId,
            activityType: a.activityType,
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
