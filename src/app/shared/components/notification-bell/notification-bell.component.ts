import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { from, interval } from 'rxjs';

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
  public readonly isOpen = signal(false);
  public readonly isLoading = signal(false);


  public unreadCount(): number {
    return this.notifications()
      .filter(notification => !notification.read)
      .length;
  }


  ngOnInit(): void {
    this.loadNotifications();
    this.notificationService.requestBrowserPermission();

    const userId = this.authService.getActiveUser()?.id;

    if (userId) {
      interval(30000)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(() => {
          this.notificationService.checkDueNotifications(userId);
        });
    }

    this.notificationService.newNotification$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.loadNotifications();
      });
  }


  public loadNotifications(): void {

    const userId = this.authService.getActiveUser()?.id;

    if (!userId) {
      console.log("No logged-in user");
      return;
    }


    this.isLoading.set(true);


    from(this.notificationService.getInAppNotifications(userId))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({

        next: (notifications) => {

          // newest notification first
          const sorted = notifications.sort(
            (a, b) =>
              new Date(b.createdAt).getTime() -
              new Date(a.createdAt).getTime()
          );


          this.notifications.set(sorted);

          this.isLoading.set(false);
        },


        error: (err) => {
          console.error(
            "Failed loading notifications",
            err
          );

          this.isLoading.set(false);
        }

      });
  }



  public openPopover(event: Event): void {

    event.stopPropagation();

    this.isOpen.set(true);


    const userId = this.authService.getActiveUser()?.id;


    if (
      userId &&
      this.unreadCount() > 0
    ) {

      from(this.notificationService.markAllAsRead(userId))
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({

          next: () => {

            this.notifications.update(
              list =>
                list.map(notification => ({
                  ...notification,
                  read: true
                }))
            );

          }

        });

    }

  }



  public closePopover(): void {
    this.isOpen.set(false);
  }




  public navigateTo(notification: Notification): void {

    this.closePopover();


    if (notification.type === "medicine") {

      this.router.navigate([
        "/main/medicines"
      ]);

    }
    else {

      this.router.navigate([
        "/main/appointments"
      ]);

    }

  }





  public formatRelativeTime(
    isoString: string
  ): string {

    const now = new Date();
    const date = new Date(isoString);


    const diffMs =
      now.getTime() -
      date.getTime();


    const diffMinutes =
      Math.floor(diffMs / 60000);



    if (diffMinutes < 1)
      return "Just now";


    if (diffMinutes < 60)
      return `${diffMinutes}m ago`;



    const hours =
      Math.floor(diffMinutes / 60);


    if (hours < 24)
      return `${hours}h ago`;



    const days =
      Math.floor(hours / 24);


    return `${days}d ago`;
  }




  public trackById(
    _index: number,
    item: Notification
  ): string {

    return item.id;

  }

}
