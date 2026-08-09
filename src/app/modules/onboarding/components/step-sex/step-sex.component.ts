import { Component, Input } from '@angular/core';
import { FormGroup } from '@angular/forms';
import {
  SEX_OPTIONS,
  SelectOption,
} from '../../../../shared/constants/health-profile-options';

@Component({
  selector: 'app-step-sex',
  templateUrl: './step-sex.component.html',
  styleUrls: ['./step-sex.component.scss'],
  standalone: false,
})
export class StepSexComponent {
  @Input() public form!: FormGroup;

  protected readonly options: SelectOption[] = SEX_OPTIONS;
}
