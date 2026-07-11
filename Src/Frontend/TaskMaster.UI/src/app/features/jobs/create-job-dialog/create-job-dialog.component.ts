import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  OnChanges,
  SimpleChanges,
} from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { DialogComponent } from '../../../shared/components/dialog/dialog.component';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { ApiService } from '../../../core/services/api.service';
import { JobType, CreateJobRequest } from '../../../core/models';

@Component({
  selector: 'app-create-job-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, DialogComponent, ButtonComponent],
  templateUrl: './create-job-dialog.component.html',
  styleUrl: './create-job-dialog.component.css',
})
export class CreateJobDialogComponent implements OnInit, OnChanges {
  @Input({ required: true }) open = false;
  @Input() submitting = false;
  @Output() close = new EventEmitter<void>();
  @Output() submit = new EventEmitter<CreateJobRequest>();

  createJobForm: FormGroup;
  jobTypes: JobType[] = [];
  uniqueNames: string[] = [];
  versions: number[] = [];

  constructor(
    private fb: FormBuilder,
    private api: ApiService,
  ) {
    this.createJobForm = this.fb.group({
      jobTypeName: ['', Validators.required],
      version: [{ value: '', disabled: true }, Validators.required],
      payload: ['', Validators.required],
    });
  }

  ngOnInit() {
    this.api.getJobTypes().subscribe((jts) => {
      this.jobTypes = jts;
      this.uniqueNames = [...new Set(jts.map((jt) => jt.name))].sort();
    });

    this.createJobForm.get('jobTypeName')?.valueChanges.subscribe((name) => {
      if (name) {
        const typeVersions = this.jobTypes.filter((jt) => jt.name === name);
        this.versions = typeVersions
          .map((jt) => jt.version)
          .sort((a, b) => b - a);
        this.createJobForm.get('version')?.enable();
        this.createJobForm.get('version')?.setValue(this.versions[0]);
      } else {
        this.versions = [];
        this.createJobForm.get('version')?.disable();
        this.createJobForm.get('version')?.setValue('');
      }
    });
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['open'] && this.open) {
      this.createJobForm.reset({ jobTypeName: '', payload: '' });
      this.versions = [];
      this.createJobForm.get('version')?.disable();
    }
  }

  onClose() {
    this.close.emit();
  }

  onSubmit() {
    if (this.createJobForm.invalid) {
      this.createJobForm.markAllAsTouched();
      return;
    }

    this.submit.emit({
      jobTypeName: this.createJobForm.value.jobTypeName,
      jobTypeVersion: this.createJobForm.value.version,
      payload: this.createJobForm.value.payload,
    });
  }
}
