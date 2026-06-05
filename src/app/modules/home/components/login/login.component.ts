import { Component, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthenticationService } from '../../../../services/authentication/authentication-service';
import { StorageService } from '../../../../shared/service/storage/storage-service';
import { LoginRequest } from '../../../../models/user.model';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  standalone: false,
})
export class LoginComponent {
  protected username = signal('');
  protected password = signal('');
  protected isLoading = signal(false);
  protected errorMessage = signal('');

  constructor(
    private router: Router,
    private authService: AuthenticationService,
    private storageService: StorageService,
  ) {}

  public onLogin(): void {
    if (!this.username() || !this.password()) {
      this.errorMessage.set('Please fill in all fields');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    const credentials: LoginRequest = {
      username: this.username(),
      password: this.password(),
    };

    this.authService.login(credentials).subscribe({
      next: (response) => {
        this.storageService.set('token', response?.token);
        this.storageService.set('user', JSON.stringify(response?.user));
        this.isLoading.set(false);
        this.router.navigate(['/main/dashboard']);
      },
      error: (error: Error) => {
        this.isLoading.set(false);
        this.errorMessage.set(error.message ?? 'Login failed');
      },
    });
  }
}
