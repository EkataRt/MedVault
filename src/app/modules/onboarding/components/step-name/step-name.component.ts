import { Component, Input } from '@angular/core';
import { FormGroup } from '@angular/forms';

@Component({
  selector: 'app-step-name',
  templateUrl: './step-name.component.html',
  styleUrls: ['./step-name.component.scss'],
  standalone: false,
})
export class StepNameComponent {
  @Input() public form!: FormGroup;
}
