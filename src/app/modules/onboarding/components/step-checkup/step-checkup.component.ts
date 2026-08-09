import { Component, Input } from '@angular/core';
import { FormGroup } from '@angular/forms';

@Component({
  selector: 'app-step-checkup',
  templateUrl: './step-checkup.component.html',
  styleUrls: ['./step-checkup.component.scss'],
  standalone: false,
})
export class StepCheckupComponent {
  @Input() public form!: FormGroup;

  protected readonly maxDate = new Date().toISOString().split('T')[0];
}
