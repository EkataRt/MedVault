import { Component, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthenticationService } from '../../../../services/authentication/authentication-service';
import { StorageService } from '../../../../shared/service/storage/storage-service';
import { SignupRequest } from '../../../../models/user.model';

const USERNAME_PATTERN = /^[a-zA-Z0-9_.-]{3,20}$/;
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

@Component({
  selector: 'app-signup',
  templateUrl: './signup.component.html',
  styleUrls: ['./signup.component.scss'],
  standalone: false,
})
export class SignupComponent {
  protected email = signal('');
  protected username = signal('');
  protected password = signal('');
  protected confirmPassword = signal('');
  protected isLoading = signal(false);
  protected errorMessage = signal('');
  protected showPassword = signal(false);
  protected showConfirmPassword = signal(false);

  protected hasMinLength = computed((): boolean => this.password().length >= 6);
  protected hasNumber = computed((): boolean => /\d/.test(this.password()));
  protected hasUppercase = computed((): boolean =>
    /[A-Z]/.test(this.password()),
  );
  protected hasSpecialChar = computed((): boolean =>
    /[^A-Za-z0-9]/.test(this.password()),
  );

  protected emailError = computed((): string => {
    if (!this.email()) {
      return '';
    }

    if (!EMAIL_PATTERN.test(this.email())) {
      return 'Enter a valid email address';
    }

    return '';
  });

  protected usernameError = computed((): string => {
    if (!this.username()) {
      return '';
    }

    if (!USERNAME_PATTERN.test(this.username())) {
      return '3-20 characters: letters, numbers, underscores, dots or hyphens';
    }

    return '';
  });

  protected passwordError = computed((): string => {
    if (!this.password()) {
      return '';
    }

    if (
      !this.hasMinLength() ||
      !this.hasNumber() ||
      !this.hasUppercase() ||
      !this.hasSpecialChar()
    ) {
      return 'Password does not meet the requirements below';
    }

    return '';
  });

  protected confirmPasswordError = computed((): string => {
    if (!this.confirmPassword()) {
      return '';
    }

    if (this.confirmPassword() !== this.password()) {
      return 'Passwords do not match';
    }

    return '';
  });

  protected isFormValid = computed((): boolean => {
    return (
      !!this.email() &&
      !!this.username() &&
      !!this.password() &&
      !!this.confirmPassword() &&
      !this.emailError() &&
      !this.usernameError() &&
      !this.passwordError() &&
      !this.confirmPasswordError()
    );
  });

  constructor(
    private router: Router,
    private authService: AuthenticationService,
    private storageService: StorageService,
  ) {}

  public togglePasswordVisibility(): void {
    this.showPassword.update((value) => !value);
  }

  public toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword.update((value) => !value);
  }

  public onSignup(): void {
    if (
      !this.email() ||
      !this.username() ||
      !this.password() ||
      !this.confirmPassword()
    ) {
      this.errorMessage.set('Please fill in all fields');
      return;
    }

    if (!this.isFormValid()) {
      this.errorMessage.set('Please fix the errors above before continuing');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    const data: SignupRequest = {
      email: this.email(),
      password: this.password(),
      username: this.username(),
    };

    this.authService.signup(data).subscribe({
      next: (response) => {
        this.storageService.set('token', response?.token);
        this.storageService.set('user', JSON.stringify(response?.user));
        this.isLoading.set(false);
        this.router.navigate(['/onboarding']);
      },
      error: (error: Error) => {
        this.isLoading.set(false);
        this.errorMessage.set(error.message ?? 'Signup failed');
      },
    });
  }
}
