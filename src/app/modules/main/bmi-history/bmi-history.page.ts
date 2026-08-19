import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { BmiHistoryEntry } from '../../../models/med-vault-model';
import { HealthProfileService } from '../../../services/health-profile/health-profile-service';

@Component({
  selector: 'app-bmi-history',
  standalone: false,
  styleUrls: ['./bmi-history.page.scss'],
  templateUrl: './bmi-history.page.html',
})
export class BmiHistoryPage implements OnInit {
  private readonly healthProfileService = inject(HealthProfileService);
  private readonly route = inject(ActivatedRoute);

  protected readonly isLoading = signal<boolean>(true);
  protected readonly history = signal<BmiHistoryEntry[]>([]);

  ngOnInit(): void {
    const profileId = this.route.snapshot.paramMap.get('id');
    if (!profileId) {
      this.isLoading.set(false);
      return;
    }

    this.healthProfileService.getBmiHistory(profileId).subscribe({
      error: () => this.isLoading.set(false),
      next: (entries) => {
        this.history.set(entries);
        this.isLoading.set(false);
      },
    });
  }
}
