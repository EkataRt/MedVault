import { Component, Input } from '@angular/core';
import { FormGroup } from '@angular/forms';

@Component({
  selector: 'app-step-weight',
  templateUrl: './step-weight.component.html',
  styleUrls: ['./step-weight.component.scss'],
  standalone: false,
})
export class StepWeightComponent {
  @Input() public form!: FormGroup;
}
