import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { NavigationService } from '../../core/services/navigation.service';
import { ContentComponent } from '../../shared/layout/content/content.component';
import { CardComponent } from '../../shared/components/card/card.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { LoadingComponent } from '../../shared/components/loading/loading.component';
import { Worker, WorkerStatus } from '../../core/models';

@Component({
  selector: 'app-worker-details',
  standalone: true,
  imports: [RouterLink, DatePipe, ContentComponent, CardComponent, BadgeComponent, LoadingComponent],
  templateUrl: './worker-details.component.html',
  styleUrl: './worker-details.component.css'
})
export class WorkerDetailsComponent implements OnInit {
  nav = inject(NavigationService);

  worker: Worker | undefined = undefined;
  notFound = false;
  WorkerStatus = WorkerStatus;

  constructor(
    private route: ActivatedRoute,
    private api: ApiService
  ) {}

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.api.getWorker(id).subscribe(w => {
        if (w) {
          this.worker = w;
        } else {
          this.notFound = true;
        }
      });
    }
  }
}
