import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { from } from 'rxjs';
import { Notification } from '../../../models/med-vault-model';
import { AuthenticationService } from '../../../services/authentication/authentication-service';
import { NotificationService } from '../../service/notification/notification-service';

@Component({
  selector: 'app-notification-bell',
  standalone: false,
  templateUrl: './notification-bell.component.html',
  styleUrls: ['./notification-bell.component.scss'],
})
export class NotificationBellComponent implements OnInit {
  private readonly notificationService = inject(NotificationService);
  private readonly authService = inject(AuthenticationService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  public readonly notifications = signal<Notification[]>([]);
  public readonly isOpen = signal<boolean>(false);
  public readonly isLoading = signal<boolean>(false);

  public readonly unreadCount = (): number =>
    this.notifications().filter((n) => !n.read).length;

  ngOnInit(): void {
    this.loadNotifications();

    this.notificationService.newNotification$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadNotifications());
  }

  public loadNotifications(): void {
    const userId = this.authService.getActiveUser()?.id;
    if (!userId) return;

    this.isLoading.set(true);

    from(this.notificationService.getInAppNotifications(userId))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => this.isLoading.set(false),
        next: (data) => {
          this.notifications.set(data);
          this.isLoading.set(false);
        },
      });
  }

  public openPopover(event: Event): void {
    event.stopPropagation();
    this.isOpen.set(true);

    const userId = this.authService.getActiveUser()?.id;
    if (userId && this.unreadCount() > 0) {
      from(this.notificationService.markAllAsRead(userId))
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.notifications.update((list) =>
              list.map((n) => ({ ...n, read: true })),
            );
          },
        });
    }
  }

  public closePopover(): void {
    this.isOpen.set(false);
  }

  public navigateTo(notification: Notification): void {
    this.closePopover();

    if (notification.type === 'medicine') {
      this.router.navigate(['/main/medicines']);
    } else {
      this.router.navigate(['/main/appointments']);
    }
  }

  public formatRelativeTime(isoString: string): string {
    const now = new Date();
    const date = new Date(isoString);
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;

    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours}h ago`;

    const diffDays = Math.floor(diffHours / 24);
    return `${diffDays}d ago`;
  }

  public trackById(_index: number, item: Notification): string {
    return item.id;
  }
}
