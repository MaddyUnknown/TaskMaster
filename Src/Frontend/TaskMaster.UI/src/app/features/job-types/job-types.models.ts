import { JobType } from '../../core/models';

export interface GroupInfo {
  name: string;
  versions: JobType[];
  selectedVersion: JobType;
}
