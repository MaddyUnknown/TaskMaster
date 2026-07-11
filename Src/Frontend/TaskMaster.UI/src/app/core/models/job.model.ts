export enum JobStatus {
  Queued = 'queued',
  InProgress = 'in-progress',
  Completed = 'completed',
  Failed = 'failed',
}

export interface Job {
  id: string;
  jobTypeName: string;
  jobTypeVersion: number;
  payload: string;
  status: JobStatus;
  workerId?: string;
  workerName?: string;
  createdDateTime: string;
  modifyDateTime?: string;
  completedDateTime?: string;
  failDateTime?: string;
}

export interface CreateJobRequest {
  jobTypeName: string;
  jobTypeVersion: number;
  payload: string;
}

export interface JobPullResponse {
  job: Job;
  workerId: string;
}
