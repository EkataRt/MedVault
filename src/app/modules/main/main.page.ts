import { Component, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-main',
  templateUrl: './main.page.html',
  styleUrls: ['./main.page.scss'],
  standalone: false,
})
export class MainPage {
  protected title = signal('Dashboard');

  private readonly pageSegments: { segment: string; title: string }[] = [
    { segment: 'edit-profile', title: 'Settings' },
    { segment: 'change-password', title: 'Settings' },
    { segment: 'delete-account', title: 'Settings' },
    { segment: 'settings', title: 'Settings' },
    { segment: 'appointments', title: 'Appointments' },
    { segment: 'dashboard', title: 'Dashboard' },
    { segment: 'document-vault', title: 'Document Vault' },
    { segment: 'medicines', title: 'Medicines' },
  ];

  constructor(private router: Router) {
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event: NavigationEnd) => {
        const url = event.urlAfterRedirects;
        const matched = this.pageSegments.find((p) => url.includes(p.segment));
        this.title.set(matched?.title ?? 'Dashboard');
      });
  }

  protected onSearchDoctor(): void {
    this.router.navigate(['main/doctors/search-doctors']);
  }
}
