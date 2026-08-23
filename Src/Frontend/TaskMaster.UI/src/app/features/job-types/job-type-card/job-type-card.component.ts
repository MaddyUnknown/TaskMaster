import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  OnDestroy,
  Inject,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { LucideFileText, LucidePlus } from '@lucide/angular';
import { CardComponent } from '../../../shared/components/card/card.component';
import { GroupInfo } from '../job-types.models';
import { JobType } from '../../../core/models';
import { AUTH_SERVICE, AuthService } from '../../../core/auth/auth.service';
import { UserPermission } from '../../../core/auth/auth.models';

@Component({
  selector: 'app-job-type-card',
  standalone: true,
  imports: [DatePipe, LucideFileText, LucidePlus, CardComponent],
  templateUrl: './job-type-card.component.html',
  styleUrl: './job-type-card.component.css',
})
export class JobTypeCardComponent implements OnInit, OnDestroy {
  @Input({ required: true }) group!: GroupInfo;
  @Output() selectVersion = new EventEmitter<{
    groupName: string;
    version: JobType;
  }>();
  @Output() addVersion = new EventEmitter<string>();

  canAddVersion = false;

  private destroy$ = new Subject<void>();

  constructor(@Inject(AUTH_SERVICE) private authService: AuthService) {}

  ngOnInit() {
    this.authService.permissions$
      .pipe(takeUntil(this.destroy$))
      .subscribe(
        () =>
          (this.canAddVersion = this.authService.hasPermission(
            UserPermission.createJobTypes,
          )),
      );
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSelectVersion(version: JobType) {
    this.selectVersion.emit({ groupName: this.group.name, version });
  }

  onAddVersion() {
    this.addVersion.emit(this.group.name);
  }
}
