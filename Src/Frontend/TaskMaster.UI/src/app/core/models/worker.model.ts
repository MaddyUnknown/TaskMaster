export enum WorkerStatus {
  Active = 'active',
  Inactive = 'in-active',
}

export interface Worker {
  id: string;
  name: string;
  status: WorkerStatus;
  lastHeartbeatTimestamp?: string;
  createdDateTime: string;
  capabilities: WorkerCapability[];
}

export interface WorkerCapability {
  jobTypeId: string;
  jobTypeName: string;
  jobTypeVersion: number;
}

export interface RegisterWorkerRequest {
  name: string;
  capabilities: { jobTypeName: string; jobTypeVersion: number }[];
}
