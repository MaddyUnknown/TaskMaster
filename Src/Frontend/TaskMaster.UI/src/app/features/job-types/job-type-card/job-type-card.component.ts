import { Component, Input, Output, EventEmitter } from '@angular/core';
import { DatePipe } from '@angular/common';
import { LucideFileText, LucidePlus } from '@lucide/angular';
import { CardComponent } from '../../../shared/components/card/card.component';
import { GroupInfo } from '../job-types.models';
import { JobType } from '../../../core/models';

@Component({
  selector: 'app-job-type-card',
  standalone: true,
  imports: [DatePipe, LucideFileText, LucidePlus, CardComponent],
  templateUrl: './job-type-card.component.html',
  styleUrl: './job-type-card.component.css',
})
export class JobTypeCardComponent {
  @Input({ required: true }) group!: GroupInfo;
  @Output() selectVersion = new EventEmitter<{ groupName: string; version: JobType }>();
  @Output() addVersion = new EventEmitter<string>();

  onSelectVersion(version: JobType) {
    this.selectVersion.emit({ groupName: this.group.name, version });
  }

  onAddVersion() {
    this.addVersion.emit(this.group.name);
  }
}
