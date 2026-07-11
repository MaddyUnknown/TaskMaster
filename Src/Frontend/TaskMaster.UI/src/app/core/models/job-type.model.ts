export interface JobType {
  id: string;
  name: string;
  version: number;
  description: string;
  schema: string;
  createdDateTime: string;
  modifyDateTime?: string;
}

export interface CreateJobTypeRequest {
  name: string;
  version: number;
  description: string;
  schema: string;
}
