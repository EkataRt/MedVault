import { Component, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthenticationService } from '../../../../services/authentication/authentication-service';
import { StorageService } from '../../../../shared/service/storage/storage-service';
import { SignupRequest } from '../../../../models/user.model';

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

  constructor(
    private router: Router,
    private authService: AuthenticationService,
    private storageService: StorageService,
  ) {}

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

    if (this.password() !== this.confirmPassword()) {
      this.errorMessage.set('Passwords do not match');
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
        this.router.navigate(['/main/dashboard']);
      },
      error: (error: Error) => {
        this.isLoading.set(false);
        this.errorMessage.set(error.message ?? 'Signup failed');
      },
    });
  }
}
