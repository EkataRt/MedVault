import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HealthProfile } from '../../../models/med-vault-model';
import { AuthenticationService } from '../../../services/authentication/authentication-service';
import { HealthProfileService } from '../../../services/health-profile/health-profile-service';
import { DataRefreshService } from '../../../shared/service/data-refresh/data-refresh-service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.page.html',
  styleUrls: ['./dashboard.page.scss'],
  standalone: false,
})
export class DashboardPage implements OnInit {
  private readonly auth = inject(AuthenticationService);
  private readonly dataRefreshService = inject(DataRefreshService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly healthProfileService = inject(HealthProfileService);

  protected readonly isLoading = signal<boolean>(true);
  protected readonly profile = signal<HealthProfile | null>(null);

  ngOnInit(): void {
    const userId = this.auth.getActiveUser()?.id;
    if (!userId) {
      this.isLoading.set(false);
      return;
    }

    this.loadProfile(userId);

    this.dataRefreshService.profileChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadProfile(userId));
  }

  private loadProfile(userId: string): void {
    this.isLoading.set(true);
    this.healthProfileService.getProfileByUserId(userId).subscribe({
      error: () => this.isLoading.set(false),
      next: (profile) => {
        this.profile.set(profile ?? null);
        this.isLoading.set(false);
      },
    });
  }
}
