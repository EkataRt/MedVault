import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HealthProfile } from '../../models/med-vault-model';
import { AuthenticationService } from '../../services/authentication/authentication-service';
import { HealthProfileService } from '../../services/health-profile/health-profile-service';
import { COMMON_ALLERGIES } from '../../shared/constants/health-profile-options';
import { capitalizeFirstLetter } from '../../shared/utils/capitalize';
import {
  alphaOnlyValidator,
  dobAgeRangeValidator,
  lastCheckupRangeValidator,
  nameValidator,
} from '../../shared/validators/health-profile-validators';

const STEP_ORDER = [
  'fullName',
  'sex',
  'bloodType',
  'dateOfBirth',
  'height',
  'weight',
  'lastCheckup',
  'allergies',
];
const TOTAL_STEPS = STEP_ORDER.length;

@Component({
  selector: 'app-onboarding',
  templateUrl: './onboarding.page.html',
  styleUrls: ['./onboarding.page.scss'],
  standalone: false,
})
export class OnboardingPage implements OnInit {
  private readonly auth = inject(AuthenticationService);
  private readonly fb = inject(FormBuilder);
  private readonly healthProfileService = inject(HealthProfileService);
  private readonly router = inject(Router);

  protected form!: FormGroup;
  protected readonly currentStep = signal(0);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly isSubmitting = signal(false);
  protected readonly stepValid = signal(false);
  protected readonly totalSteps = TOTAL_STEPS;

  ngOnInit(): void {
    this.buildForm();
    this.form.statusChanges.subscribe(() => this.refreshStepValidity());
    this.refreshStepValidity();
  }

  protected get progress(): number {
    return (this.currentStep() + 1) / this.totalSteps;
  }

  protected onBack(): void {
    if (this.currentStep() === 0) return;
    this.currentStep.update((step) => step - 1);
    this.refreshStepValidity();
  }

  protected onNext(): void {
    if (!this.stepValid()) return;
    if (this.currentStep() === this.totalSteps - 1) {
      this.submit();
      return;
    }
    this.currentStep.update((step) => step + 1);
    this.refreshStepValidity();
  }

  protected refreshStepValidity(): void {
    const controlName = STEP_ORDER[this.currentStep()];
    if (controlName === 'allergies') {
      this.stepValid.set(this.isAllergiesStepValid());
      return;
    }
    const control = this.form.get(controlName);
    this.stepValid.set(!!control && control.valid);
  }

  private buildForm(): void {
    const commonControls: Record<string, boolean> = {};
    COMMON_ALLERGIES.forEach((allergy) => {
      commonControls[allergy] = false;
    });
    this.form = this.fb.group({
      allergies: this.fb.group({
        common: this.fb.group(commonControls),
        noneSelected: [false],
        otherAllergyText: ['', alphaOnlyValidator()],
      }),
      bloodType: ['', Validators.required],
      dateOfBirth: ['', [Validators.required, dobAgeRangeValidator()]],
      fullName: ['', [Validators.required, nameValidator()]],
      height: [
        null,
        [Validators.required, Validators.min(20), Validators.max(280)],
      ],
      lastCheckup: ['', [Validators.required, lastCheckupRangeValidator()]],
      sex: ['', Validators.required],
      weight: [
        null,
        [Validators.required, Validators.min(0.2), Validators.max(200)],
      ],
    });
  }

  private isAllergiesStepValid(): boolean {
    const allergiesGroup = this.form.get('allergies');
    if (!allergiesGroup) return false;
    const noneSelected = allergiesGroup.get('noneSelected')?.value as boolean;
    if (noneSelected) return true;
    const common = allergiesGroup.get('common')?.value as Record<
      string,
      boolean
    >;
    const hasCommonSelected = Object.values(common ?? {}).some(
      (checked) => checked,
    );
    const otherControl = allergiesGroup.get('otherAllergyText');
    const otherText = ((otherControl?.value as string) ?? '').trim();
    const otherValid = !!otherText && !!otherControl && otherControl.valid;
    return hasCommonSelected || otherValid;
  }

  private buildAllergiesList(): string[] {
    const allergiesGroup = this.form.get('allergies');
    const noneSelected = allergiesGroup?.get('noneSelected')?.value as boolean;
    if (noneSelected) return [];
    const common = allergiesGroup?.get('common')?.value as Record<
      string,
      boolean
    >;
    const selected = Object.entries(common ?? {})
      .filter(([, checked]) => checked)
      .map(([allergy]) => allergy);
    const otherText = (
      (allergiesGroup?.get('otherAllergyText')?.value as string) ?? ''
    ).trim();
    if (otherText) {
      const capitalized = capitalizeFirstLetter(otherText);
      if (!selected.includes(capitalized)) selected.push(capitalized);
    }
    return selected;
  }

  private submit(): void {
    const userId = this.auth.getActiveUser()?.id;
    if (!userId) {
      this.errorMessage.set('You must be logged in to continue.');
      return;
    }
    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    const payload: Omit<HealthProfile, 'id' | 'age'> = {
      allergies: this.buildAllergiesList(),
      bloodType: this.form.value.bloodType,
      dateOfBirth: this.form.value.dateOfBirth,
      fullName: this.form.value.fullName,
      height: this.form.value.height,
      lastCheckup: this.form.value.lastCheckup,
      sex: this.form.value.sex,
      userId,
      weight: this.form.value.weight,
    };
    this.healthProfileService.createProfile(payload).subscribe({
      error: () => {
        this.isSubmitting.set(false);
        this.errorMessage.set('Failed to save your profile. Please try again.');
      },
      next: () => {
        this.isSubmitting.set(false);
        this.router.navigate(['/main/dashboard']);
      },
    });
  }
}
