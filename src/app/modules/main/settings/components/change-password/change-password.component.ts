import { Component, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';

import { AuthenticationService } from '../../../../../services/authentication/authentication-service';

@Component({
  selector: 'app-change-password',
  standalone: false,
  styleUrls: ['./change-password.component.scss'],
  templateUrl: './change-password.component.html',
})
export class ChangePasswordComponent {
  protected errorMessage = signal<string | null>(null);

  protected form: FormGroup;

  protected isUpdating = signal(false);

  protected successMessage = signal<string | null>(null);

  constructor(
    private auth: AuthenticationService,
    private fb: FormBuilder,
    private router: Router,
  ) {
    this.form = this.fb.group(
      {
        confirmPassword: ['', Validators.required],
        currentPassword: ['', Validators.required],
        newPassword: ['', [Validators.required, Validators.minLength(8)]],
      },
      { validators: this.passwordMatchValidator },
    );
  }

  protected onBack(): void {
    this.router.navigate(['/main/settings']);
  }

  protected onSubmit(): void {
    if (this.form.invalid) {
      return;
    }

    const user = this.auth.getActiveUser();

    if (!user?.id) {
      return;
    }

    if (!user.password || user.password !== this.form.value.currentPassword) {
      this.errorMessage.set('Current password is incorrect.');
      return;
    }

    this.errorMessage.set(null);
    this.isUpdating.set(true);

    this.auth
      .updateUser(String(user.id), { password: this.form.value.newPassword })
      .subscribe({
        error: () => {
          this.errorMessage.set('Failed to update password. Please try again.');
          this.isUpdating.set(false);
        },
        next: () => {
          this.form.reset();
          this.isUpdating.set(false);
          this.successMessage.set('Password updated successfully.');
          setTimeout(() => this.successMessage.set(null), 3000);
        },
      });
  }

  private passwordMatchValidator(
    group: AbstractControl,
  ): ValidationErrors | null {
    const newPw = group.get('newPassword')?.value;
    const confirm = group.get('confirmPassword')?.value;

    return newPw === confirm ? null : { mismatch: true };
  }
}
