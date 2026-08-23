import { UserPermission } from '../auth.models';

export const SCOPE_PERMISSION_MAP: Record<string, UserPermission> = {
  'jobs:read': UserPermission.readJob,
  'jobs:stats': UserPermission.readJobStats,
  'jobs:create': UserPermission.createJob,
  'jobtypes:read': UserPermission.readJobTypes,
  'jobtypes:create': UserPermission.createJobTypes,
  'workers:read': UserPermission.readWorkers,
  'workers:stats': UserPermission.readWorkersStats,
  'dashboard:read': UserPermission.readDashboard,
};
