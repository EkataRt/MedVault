import { Component, Input, OnChanges, signal } from '@angular/core';
import { Router } from '@angular/router';

import { HealthProfile } from '../../../../../models/med-vault-model';
import { BmiCategory } from '../../../../../shared/enums/bmi-tag';

const BMI_THRESHOLDS = {
  Obese: 30,
  Overweight: 25,
  Underweight: 18.5,
} as const;

@Component({
  selector: 'app-user-info-card',
  templateUrl: './user-info-card.component.html',
  styleUrls: ['./user-info-card.component.scss'],
  standalone: false,
})
export class UserInfoCardComponent implements OnChanges {
  @Input() public isLoading: boolean = true;
  @Input() public profile: HealthProfile | null = null;

  protected readonly BmiCategory = BmiCategory;
  protected readonly bmi = signal<number | null>(null);
  protected readonly bmiCategory = signal<BmiCategory | null>(null);

  constructor(private router: Router) {}

  ngOnChanges(): void {
    if (this.profile?.height && this.profile?.weight) {
      const heightM = this.profile.height / 100;
      const bmiVal = this.profile.weight / (heightM * heightM);

      this.bmi.set(Math.round(bmiVal * 10) / 10);
      this.bmiCategory.set(this.categorizeBmi(bmiVal));
    } else {
      this.bmi.set(null);
      this.bmiCategory.set(null);
    }
  }

  protected get allergiesDisplay(): string {
    if (!this.profile?.allergies?.length) return 'None known';
    return this.profile.allergies.join(', ');
  }

  protected get initials(): string {
    if (!this.profile?.fullName) return '?';
    return this.profile.fullName
      .trim()
      .split(' ')
      .map((p) => p[0])
      .join('')
      .slice(0, 2)
      .toUpperCase();
  }

  protected onEditClick(): void {
    this.router.navigate(['/main/settings']);
  }

  protected onBmiClick(): void {
    if (!this.profile?.id) return;
    this.router.navigate(['/main/bmi-history', this.profile.id]);
  }
  private categorizeBmi(bmi: number): BmiCategory {
    if (bmi < BMI_THRESHOLDS.Underweight) return BmiCategory.Underweight;
    if (bmi < BMI_THRESHOLDS.Overweight) return BmiCategory.Normal;
    if (bmi < BMI_THRESHOLDS.Obese) return BmiCategory.Overweight;
    return BmiCategory.Obese;
  }
}
