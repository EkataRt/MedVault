import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastController } from '@ionic/angular';
import { forkJoin } from 'rxjs';
import { HealthProfile } from '../../../../../models/med-vault-model';
import { AuthenticationService } from '../../../../../services/authentication/authentication-service';
import { HealthProfileService } from '../../../../../services/health-profile/health-profile-service';
import {
  BLOOD_TYPE_OPTIONS,
  COMMON_ALLERGIES,
  SEX_OPTIONS,
  SelectOption,
} from '../../../../../shared/constants/health-profile-options';
import { DataRefreshService } from '../../../../../shared/service/data-refresh/data-refresh-service';
import { capitalizeFirstLetter } from '../../../../../shared/utils/capitalize';
import {
  alphaOnlyValidator,
  dobAgeRangeValidator,
  lastCheckupRangeValidator,
  nameValidator,
} from '../../../../../shared/validators/health-profile-validators';

@Component({
  selector: 'app-edit-profile',
  standalone: false,
  styleUrls: ['./edit-profile.component.scss'],
  templateUrl: './edit-profile.component.html',
})
export class EditProfileComponent implements OnInit {
  private readonly auth = inject(AuthenticationService);
  private readonly dataRefreshService = inject(DataRefreshService);
  private readonly fb = inject(FormBuilder);
  private readonly healthProfileService = inject(HealthProfileService);
  private readonly router = inject(Router);
  private readonly toastController = inject(ToastController);

  protected form!: FormGroup;
  protected readonly bloodTypeOptions: SelectOption[] = BLOOD_TYPE_OPTIONS;
  protected readonly commonAllergies = COMMON_ALLERGIES;
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly isSaved = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly maxDate = new Date().toISOString().split('T')[0];
  protected readonly sexOptions: SelectOption[] = SEX_OPTIONS;

  private healthProfile: HealthProfile | undefined;

  ngOnInit(): void {
    this.buildForm();
    this.wireAllergiesInteractions();

    const user = this.auth.getActiveUser();
    if (!user?.id) return;

    this.healthProfileService.getProfileByUserId(String(user.id)).subscribe({
      error: () => this.errorMessage.set('Failed to load profile.'),
      next: (profile) => {
        this.healthProfile = profile;
        if (profile) {
          this.patchForm(profile);
        }
      },
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

  protected onBack(): void {
    this.router.navigate(['/main/settings']);
  }

  protected onSave(): void {
    if (this.form.invalid) return;
    const user = this.auth.getActiveUser();
    if (!user?.id) return;

    this.isSaving.set(true);

    const healthProfileData: Partial<HealthProfile> = {
      allergies: this.buildAllergiesList(),
      bloodType: this.form.value.bloodType,
      dateOfBirth: this.form.value.dateOfBirth,
      fullName: this.form.value.fullName,
      height: this.form.value.height,
      lastCheckup: this.form.value.lastCheckup,
      sex: this.form.value.sex,
      weight: this.form.value.weight,
    };

    const profileRequest$ = this.healthProfile?.id
      ? this.healthProfileService.updateProfile(
          String(this.healthProfile.id),
          healthProfileData,
        )
      : this.healthProfileService.createProfile({
          ...(healthProfileData as Omit<HealthProfile, 'id' | 'age'>),
          userId: String(user.id),
        });

    const userData = { username: this.form.value.fullName };

    forkJoin({
      healthProfile: profileRequest$,
      user: this.auth.updateUser(String(user.id), userData),
    }).subscribe({
      error: () => {
        this.errorMessage.set('Failed to save. Please try again.');
        this.isSaving.set(false);
      },
      next: (result) => {
        this.healthProfile = result.healthProfile;
        this.isSaved.set(true);
        this.isSaving.set(false);
        this.dataRefreshService.emitProfileChanged();
        this.presentSavedToast();
        setTimeout(() => this.isSaved.set(false), 3000);
      },
    });
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

  private wireAllergiesInteractions(): void {
    const noneControl = this.allergiesGroup.get('noneSelected')!;
    const otherControl = this.allergiesGroup.get('otherAllergyText')!;

    noneControl.valueChanges.subscribe((checked: boolean) => {
      if (checked) {
        this.commonGroup.reset(this.buildAllergiesResetValue(), {
          emitEvent: false,
        });
        otherControl.setValue('', { emitEvent: false });
      }
    });

    this.commonGroup.valueChanges.subscribe(
      (values: Record<string, boolean>) => {
        const anyChecked = Object.values(values).some((checked) => checked);
        if (anyChecked && noneControl.value) {
          noneControl.setValue(false, { emitEvent: false });
        }
      },
    );

    otherControl.valueChanges.subscribe((value: string) => {
      if (value && noneControl.value) {
        noneControl.setValue(false, { emitEvent: false });
      }
    });
  }

  private buildAllergiesResetValue(): Record<string, boolean> {
    const resetValue: Record<string, boolean> = {};
    this.commonAllergies.forEach((allergy) => {
      resetValue[allergy] = false;
    });
    return resetValue;
  }

  private buildAllergiesList(): string[] {
    const noneSelected = this.allergiesGroup.get('noneSelected')
      ?.value as boolean;
    if (noneSelected) return [];

    const common = this.commonGroup.value as Record<string, boolean>;
    const selected = Object.entries(common)
      .filter(([, checked]) => checked)
      .map(([allergy]) => allergy);

    const otherText = (
      (this.allergiesGroup.get('otherAllergyText')?.value as string) ?? ''
    ).trim();
    if (otherText) {
      const capitalized = capitalizeFirstLetter(otherText);
      if (!selected.includes(capitalized)) selected.push(capitalized);
    }

    return selected;
  }

  private patchForm(profile: HealthProfile): void {
    const commonPatch: Record<string, boolean> = {};
    const otherAllergies: string[] = [];

    profile.allergies.forEach((allergy) => {
      if (this.commonAllergies.includes(allergy)) {
        commonPatch[allergy] = true;
      } else {
        otherAllergies.push(allergy);
      }
    });

    this.form.patchValue({
      allergies: {
        common: commonPatch,
        noneSelected: profile.allergies.length === 0,
        otherAllergyText: otherAllergies[0] ?? '',
      },
      bloodType: profile.bloodType,
      dateOfBirth: profile.dateOfBirth,
      fullName: profile.fullName,
      height: profile.height,
      lastCheckup: profile.lastCheckup,
      sex: profile.sex,
      weight: profile.weight,
    });
  }

  private async presentSavedToast(): Promise<void> {
    const toast = await this.toastController.create({
      color: 'success',
      duration: 2000,
      icon: 'checkmark-circle-outline',
      message: 'Saved successfully.',
      position: 'top',
    });
    await toast.present();
  }
}
