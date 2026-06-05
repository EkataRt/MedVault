import { Component, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthenticationService } from '../../../../../services/authentication/authentication-service';

@Component({
  selector: 'app-delete-account',
  standalone: false,
  styleUrls: ['./delete-account.component.scss'],
  templateUrl: './delete-account.component.html',
})
export class DeleteAccountComponent {
  protected errorMessage = signal('');

  protected form: FormGroup;

  protected showConfirm = signal(false);

  constructor(
    private auth: AuthenticationService,
    private fb: FormBuilder,
    private router: Router,
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
    });
  }

  protected onBack(): void {
    this.router.navigate(['/main/settings']);
  }

  protected onCancel(): void {
    this.showConfirm.set(false);
  }

  protected onConfirmDelete(): void {
    const user = this.auth.getActiveUser();

    if (!user?.id) {
      return;
    }

    this.auth.deleteUser(String(user.id)).subscribe({
      error: () => {
        this.errorMessage.set('Failed to delete account. Please try again.');
        this.showConfirm.set(false);
      },
      next: () => {
        this.auth.logout();
        this.router.navigate(['/home/login']);
      },
    });
  }

  protected onPermanentlyDelete(): void {
    if (this.form.invalid) {
      return;
    }

    const user = this.auth.getActiveUser();

    if (!user) {
      return;
    }

    const emailMatches = user.email === this.form.value.email;
    const passwordMatches =
      !!user.password && user.password === this.form.value.password;

    if (!emailMatches || !passwordMatches) {
      this.errorMessage.set('Email or password is incorrect.');
      return;
    }

    this.errorMessage.set('');
    this.showConfirm.set(true);
  }
}
