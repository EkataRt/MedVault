import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { App, AppState } from '@capacitor/app';
import { AuthenticationService } from './services/authentication/authentication-service';
import { NotificationService } from './shared/service/notification/notification-service';
import { StorageService } from './shared/service/storage/storage-service';

@Component({
  selector: 'app-root',
  templateUrl: 'app.component.html',
  styleUrls: ['app.component.scss'],
  standalone: false,
})
export class AppComponent implements OnInit {
  private readonly authService = inject(AuthenticationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly notificationService = inject(NotificationService);
  private readonly storageService = inject(StorageService);

  ngOnInit(): void {
    this.restoreDarkMode();
    this.initNotifications();

    App.addListener('appStateChange', (state: AppState) => {
      if (state.isActive) {
        this.scheduleNotifications();
      }
    });
  }

  private restoreDarkMode(): void {
    this.storageService
      .getAsync('darkMode')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => {
        document.body.classList.toggle('dark', value === 'true');
      });
  }

  private async initNotifications(): Promise<void> {
    await this.notificationService.requestPermission();

    const userId = this.authService.getActiveUser()?.id;
    if (!userId) return;

    this.notificationService.listenForReceived(userId);
    this.notificationService.listenForActions(userId);
    await this.scheduleNotifications();
  }

  private async scheduleNotifications(): Promise<void> {
    const userId = this.authService.getActiveUser()?.id;
    if (!userId) return;

    await this.notificationService.scheduleAll(userId);
  }
}
