import { Component, OnInit } from '@angular/core';
import { LucidePlus } from '@lucide/angular';
import { ApiService } from '../../core/services/api.service';
import { ContentComponent } from '../../shared/layout/content/content.component';
import { LoadingComponent } from '../../shared/components/loading/loading.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { JobType, CreateJobTypeRequest } from '../../core/models';
import { GroupInfo } from './job-types.models';
import { JobTypeCardComponent } from './job-type-card/job-type-card.component';
import { JobTypeFormDialogComponent } from './job-type-form-dialog/job-type-form-dialog.component';

@Component({
  selector: 'app-job-types',
  standalone: true,
  imports: [
    ContentComponent,
    LoadingComponent,
    EmptyStateComponent,
    ButtonComponent,
    JobTypeCardComponent,
    JobTypeFormDialogComponent,
    LucidePlus,
  ],
  templateUrl: './job-types.component.html',
  styleUrl: './job-types.component.css',
})
export class JobTypesComponent implements OnInit {
  jobTypes: JobType[] = [];
  groups: GroupInfo[] = [];
  loading = true;

  showCreateDialog = false;
  dialogMode: 'create' | 'add-version' = 'create';
  submitting = false;
  dialogVersion: number | null = null;
  dialogInitialData: {
    name: string;
    description: string;
    schema: string;
  } | null = null;

  constructor(private api: ApiService) {}

  ngOnInit() {
    this.refreshJobTypes();
  }

  refreshJobTypes() {
    this.loading = true;
    this.api.getJobTypes().subscribe((jt: JobType[]) => {
      this.jobTypes = jt;
      this.updateGroups();
      this.loading = false;
    });
  }

  private updateGroups() {
    const map = new Map<string, JobType[]>();
    for (const jt of this.jobTypes) {
      if (!map.has(jt.name)) map.set(jt.name, []);
      map.get(jt.name)!.push(jt);
    }
    this.groups = Array.from(map.entries())
      .map(([name, versions]) => ({
        name,
        versions: versions.sort((a, b) => b.version - a.version),
        selectedVersion: versions.sort((a, b) => b.version - a.version)[0],
      }))
      .sort((a, b) => a.name.localeCompare(b.name));
  }

  selectVersion(event: { groupName: string; version: JobType }) {
    const group = this.groups.find((g) => g.name === event.groupName);
    if (group) group.selectedVersion = event.version;
  }

  openCreateDialog() {
    this.dialogMode = 'create';
    this.dialogVersion = null;
    this.dialogInitialData = null;
    this.showCreateDialog = true;
  }

  openAddVersion(groupName: string) {
    const group = this.groups.find((g) => g.name === groupName);
    if (!group) return;

    this.dialogMode = 'add-version';
    const maxVersion = Math.max(...group.versions.map((v) => v.version));
    const maxVersionData = group.versions.find((v) => v.version === maxVersion);

    this.dialogVersion = maxVersion + 1;
    this.dialogInitialData = {
      name: group.name,
      description: maxVersionData!.description,
      schema: maxVersionData!.schema,
    };

    this.showCreateDialog = true;
  }

  closeDialog() {
    this.showCreateDialog = false;
  }

  submit(req: CreateJobTypeRequest) {
    this.submitting = true;
    this.api.createJobType(req).subscribe({
      next: () => {
        this.submitting = false;
        this.showCreateDialog = false;
        this.refreshJobTypes();
      },
      error: () => {
        this.submitting = false;
      },
    });
  }
}
