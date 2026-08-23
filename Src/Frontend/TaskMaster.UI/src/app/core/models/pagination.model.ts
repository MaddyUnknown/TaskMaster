import { JobStatus } from './job.model';
import { WorkerStatus } from './worker.model';

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface PaginationQuery {
  page?: number;
  pageSize?: number;
}

export interface JobQuery extends PaginationQuery {
  status?: JobStatus;
}

export interface WorkerQuery extends PaginationQuery {
  status?: WorkerStatus;
}

export interface JobCounts {
  queued: number;
  inProgress: number;
  completed: number;
  failed: number;
  total: number;
}

export interface WorkerCounts {
  active: number;
  inActive: number;
  total: number;
}
