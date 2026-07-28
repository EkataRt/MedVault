import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastController } from '@ionic/angular';
import { forkJoin } from 'rxjs';
import { HealthProfile } from '../../../../../models/med-vault-model';
import { AuthenticationService } from '../../../../../services/authentication/authentication-service';
import { HealthProfileService } from '../../../../../services/health-profile/health-profile-service';
import { DataRefreshService } from '../../../../../shared/service/data-refresh/data-refresh-service';

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
  protected errorMessage = signal<string | null>(null);
  protected isSaved = signal(false);
  protected isSaving = signal(false);

  private healthProfile: HealthProfile | undefined;

  ngOnInit(): void {
    this.form = this.fb.group({
      age: [
        null,
        [Validators.required, Validators.min(0), Validators.max(150)],
      ],
      allergies: [''],
      bloodType: ['', Validators.required],
      fullName: ['', Validators.required],
      height: [null, [Validators.required, Validators.min(0)]],
      lastCheckup: ['', Validators.required],
      sex: ['', Validators.required],
      weight: [null, [Validators.required, Validators.min(0)]],
    });

    const user = this.auth.getActiveUser();
    if (!user?.id) return;

    this.healthProfileService.getProfileByUserId(String(user.id)).subscribe({
      error: () => this.errorMessage.set('Failed to load profile.'),
      next: (profile) => {
        this.healthProfile = profile;
        if (profile) {
          this.form.patchValue({
            age: profile.age,
            allergies: profile.allergies.join(', '),
            bloodType: profile.bloodType,
            fullName: profile.fullName,
            height: profile.height,
            lastCheckup: profile.lastCheckup,
            sex: profile.sex,
            weight: profile.weight,
          });
        }
      },
    });
  }

  protected onBack(): void {
    this.router.navigate(['/main/settings']);
  }

  protected onSave(): void {
    if (this.form.invalid) return;

    const user = this.auth.getActiveUser();
    if (!user?.id) return;

    this.isSaving.set(true);

    const allergiesRaw: string = this.form.value.allergies ?? '';
    const allergies: string[] = allergiesRaw
      .split(',')
      .map((s: string) => s.trim())
      .filter((s: string) => s.length > 0);

    const healthProfileData: Partial<HealthProfile> = {
      age: this.form.value.age,
      allergies,
      bloodType: this.form.value.bloodType,
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
        ...(healthProfileData as Omit<HealthProfile, 'id'>),
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
