import { Component, Input } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { calculateAgeLabel } from '../../../../shared/validators/health-profile-validators';

@Component({
  selector: 'app-step-dob',
  templateUrl: './step-dob.component.html',
  styleUrls: ['./step-dob.component.scss'],
  standalone: false,
})
export class StepDobComponent {
  @Input() public form!: FormGroup;

  protected readonly maxDate = new Date().toISOString().split('T')[0];

  protected get ageLabel(): string {
    const value = this.form.get('dateOfBirth')?.value as string;
    return calculateAgeLabel(value);
  }
}
