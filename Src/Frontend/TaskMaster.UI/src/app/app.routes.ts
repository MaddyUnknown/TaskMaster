import { Routes } from '@angular/router';
import { LayoutComponent } from './shared/layout/layout/layout.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { JobsComponent } from './features/jobs/jobs.component';
import { JobDetailsComponent } from './features/job-details/job-details.component';
import { WorkersComponent } from './features/workers/workers.component';
import { WorkerDetailsComponent } from './features/worker-details/worker-details.component';
import { JobTypesComponent } from './features/job-types/job-types.component';
import { NotFoundComponent } from './features/not-found/not-found.component';

export const routes: Routes = [
  {
    path: '',
    component: LayoutComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        component: DashboardComponent,
        title: 'Dashboard - TaskMaster',
      },
      {
        path: 'jobs',
        component: JobsComponent,
        title: 'Jobs - TaskMaster',
      },
      {
        path: 'jobs/:id',
        component: JobDetailsComponent,
        title: 'Job Details - TaskMaster',
      },
      {
        path: 'workers',
        component: WorkersComponent,
        title: 'Workers - TaskMaster',
      },
      {
        path: 'workers/:id',
        component: WorkerDetailsComponent,
        title: 'Worker Details - TaskMaster',
      },
      {
        path: 'job-types',
        component: JobTypesComponent,
        title: 'Job Types - TaskMaster',
      },

      {
        path: '**',
        component: NotFoundComponent,
        title: 'Not Found - TaskMaster',
      },
    ],
  },
];
