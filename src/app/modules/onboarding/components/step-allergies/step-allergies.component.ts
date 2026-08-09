import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { COMMON_ALLERGIES } from '../../../../shared/constants/health-profile-options';

@Component({
  selector: 'app-step-allergies',
  templateUrl: './step-allergies.component.html',
  styleUrls: ['./step-allergies.component.scss'],
  standalone: false,
})
export class StepAllergiesComponent implements OnInit {
  @Input() public form!: FormGroup;
  @Output() public readonly allergiesChanged = new EventEmitter<void>();

  protected readonly commonAllergies = COMMON_ALLERGIES;

  ngOnInit(): void {
    const noneControl = this.allergiesGroup.get('noneSelected')!;
    const otherControl = this.allergiesGroup.get('otherAllergyText')!;

    noneControl.valueChanges.subscribe((checked: boolean) => {
      if (checked) {
        this.commonGroup.reset(this.buildResetValue(), { emitEvent: false });
        otherControl.setValue('', { emitEvent: false });
      }
      this.allergiesChanged.emit();
    });

    this.commonGroup.valueChanges.subscribe(
      (values: Record<string, boolean>) => {
        const anyChecked = Object.values(values).some((checked) => checked);
        if (anyChecked && noneControl.value) {
          noneControl.setValue(false, { emitEvent: false });
        }
        this.allergiesChanged.emit();
      },
    );

    otherControl.valueChanges.subscribe((value: string) => {
      if (value && noneControl.value) {
        noneControl.setValue(false, { emitEvent: false });
      }
      this.allergiesChanged.emit();
    });
  }

  protected get allergiesGroup(): FormGroup {
    return this.form.get('allergies') as FormGroup;
  }

  protected get commonGroup(): FormGroup {
    return this.allergiesGroup.get('common') as FormGroup;
  }

  protected get isNoneSelected(): boolean {
    return !!this.allergiesGroup.get('noneSelected')?.value;
  }

  private buildResetValue(): Record<string, boolean> {
    const resetValue: Record<string, boolean> = {};
    this.commonAllergies.forEach((allergy) => {
      resetValue[allergy] = false;
    });
    return resetValue;
  }
}
