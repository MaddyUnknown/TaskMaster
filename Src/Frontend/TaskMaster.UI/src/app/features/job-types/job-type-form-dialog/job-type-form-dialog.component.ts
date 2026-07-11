import {
  Component,
  Input,
  Output,
  EventEmitter,
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
import { CreateJobTypeRequest } from '../../../core/models';

@Component({
  selector: 'app-job-type-form-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, DialogComponent, ButtonComponent],
  templateUrl: './job-type-form-dialog.component.html',
  styleUrl: './job-type-form-dialog.component.css',
})
export class JobTypeFormDialogComponent implements OnChanges {
  @Input({ required: true }) open = false;
  @Input() mode: 'create' | 'add-version' = 'create';
  @Input() version: number | null = null;
  @Input() submitting = false;
  @Input() initialData: {
    name: string;
    description: string;
    schema: string;
  } | null = null;
  @Output() close = new EventEmitter<void>();
  @Output() submit = new EventEmitter<CreateJobTypeRequest>();

  jobTypeForm: FormGroup;

  constructor(private fb: FormBuilder) {
    this.jobTypeForm = this.fb.group({
      name: ['', Validators.required],
      description: ['', Validators.required],
      schema: ['', Validators.required],
    });
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['initialData'] || changes['mode']) {
      this.resetForm();
    }
  }

  private resetForm() {
    if (this.initialData) {
      this.jobTypeForm.setValue({
        name: this.initialData.name,
        description: this.initialData.description,
        schema: this.initialData.schema,
      });
    } else {
      this.jobTypeForm.reset({ name: '', description: '', schema: '' });
    }
  }

  onClose() {
    this.close.emit();
  }

  onSubmit() {
    if (this.jobTypeForm.invalid) {
      this.jobTypeForm.markAllAsTouched();
      return;
    }

    const jobTypeValue = this.jobTypeForm.value;
    this.submit.emit({
      name: jobTypeValue.name,
      version: this.mode === 'add-version' && this.version ? this.version : 1,
      description: jobTypeValue.description,
      schema: jobTypeValue.schema,
    });
  }
}
