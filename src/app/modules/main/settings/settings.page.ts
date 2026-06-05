import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { AuthenticationService } from '../../../services/authentication/authentication-service';
import { StorageService } from '../../../shared/service/storage/storage-service';

@Component({
  selector: 'app-settings',
  standalone: false,
  styleUrls: ['./settings.page.scss'],
  templateUrl: './settings.page.html',
})
export class SettingsPage implements OnInit {
  private readonly auth = inject(AuthenticationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly storageService = inject(StorageService);

  protected isDarkMode = signal<boolean>(false);

  ngOnInit(): void {
    this.storageService
      .getAsync('darkMode')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => {
        const enabled = value === 'true';
        this.isDarkMode.set(enabled);
        document.body.classList.toggle('dark', enabled);
      });
  }

  protected onDarkModeToggle(event: CustomEvent): void {
    const enabled = (event.detail as { checked: boolean }).checked;
    this.isDarkMode.set(enabled);
    document.body.classList.toggle('dark', enabled);
    this.storageService.set('darkMode', String(enabled));
  }

  protected onLogout(): void {
    this.auth.logout();
    this.router.navigate(['/home/login']);
  }

  protected onNavigate(route: string): void {
    this.router.navigate(['/main/settings', route]);
  }
}
