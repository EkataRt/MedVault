import { Component, Input } from '@angular/core';
import { FormGroup } from '@angular/forms';
import {
  BLOOD_TYPE_OPTIONS,
  SelectOption,
} from '../../../../shared/constants/health-profile-options';

@Component({
  selector: 'app-step-blood-type',
  templateUrl: './step-blood-type.component.html',
  styleUrls: ['./step-blood-type.component.scss'],
  standalone: false,
})
export class StepBloodTypeComponent {
  @Input() public form!: FormGroup;

  protected readonly options: SelectOption[] = BLOOD_TYPE_OPTIONS;
}
