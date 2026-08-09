import { Component, Input } from '@angular/core';
import { FormGroup } from '@angular/forms';

@Component({
  selector: 'app-step-height',
  templateUrl: './step-height.component.html',
  styleUrls: ['./step-height.component.scss'],
  standalone: false,
})
export class StepHeightComponent {
  @Input() public form!: FormGroup;
}
