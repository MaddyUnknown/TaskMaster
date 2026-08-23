export enum AuthState {
  initializing = 'initializing',
  authenticated = 'authenticated',
  unauthenticated = 'unauthenticated',
  error = 'error',
}

export enum UserPermission {
  readJob = 'jobs.read',
  readJobStats = 'jobs.stats',
  createJob = 'jobs.create',
  readJobTypes = 'jobtypes.read',
  createJobTypes = 'jobtypes.create',
  readWorkers = 'workers.read',
  readWorkersStats = 'workers.stats',
  readDashboard = 'dashboard.read',
}
