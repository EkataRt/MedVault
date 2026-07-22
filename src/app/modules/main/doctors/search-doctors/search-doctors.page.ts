import {
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Doctor } from '../../../../models/med-vault-model';
import { DoctorService } from '../../../../services/doctor/doctor-service';

@Component({
  selector: 'app-search-doctors',
  standalone: false,
  templateUrl: './search-doctors.page.html',
  styleUrl: './search-doctors.page.scss',
})
export class SearchDoctorsPage implements OnInit {
  private readonly doctorService = inject(DoctorService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly healthProblem = signal('');
  protected readonly hospitalType = signal<'all' | 'private' | 'public'>('all');
  protected readonly locationQuery = signal('');
  protected readonly selectedLocation = signal('');
  protected readonly showLocationSuggestions = signal(false);
  protected readonly isSort = signal(false);
  protected readonly isSearching = signal(false);
  protected readonly hasSearched = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly doctors = signal<Doctor[]>([]);
  protected readonly locations = signal<string[]>([]);

  protected readonly locationSuggestions = computed(() => {
    const query = this.locationQuery().trim().toLowerCase();
    if (!query) return [];
    return this.locations().filter((loc) =>
      loc.toLowerCase().includes(query),
    );
  });

  ngOnInit(): void {
    this.loadLocations();
  }

  private loadLocations(): void {
    this.doctorService
      .getAvailableLocations()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => this.errorMessage.set('Failed to load locations.'),
        next: (data) => this.locations.set(data),
      });
  }

  protected setHospitalType(value: 'all' | 'private' | 'public'): void {
    this.hospitalType.set(value);
  }

  protected onLocationInput(value: string): void {
    this.locationQuery.set(value);

    if (value === this.selectedLocation()) {
      return; // this is the programmatic echo from selectLocation — ignore it
    }

    this.selectedLocation.set('');
    this.showLocationSuggestions.set(true);
  }

  protected onLocationFocus(): void {
    if (this.locationQuery().trim()) {
      this.showLocationSuggestions.set(true);
    }
  }

  protected selectLocation(loc: string): void {
    this.selectedLocation.set(loc);
    this.locationQuery.set(loc);
    this.showLocationSuggestions.set(false);
  }

  protected onLocationBlur(): void {
    this.showLocationSuggestions.set(false);
  }

  protected toggleSort(): void {
    this.isSort.set(!this.isSort());
  }

  protected onSearch(): void {
    if (!this.selectedLocation()) {
      this.errorMessage.set('Please select a location.');
      return;
    }

    this.isSearching.set(true);
    this.errorMessage.set(null);

    this.doctorService
      .searchNearby(
        this.selectedLocation(),
        this.healthProblem(),
        this.hospitalType(),
        this.isSort(),
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => {
          this.errorMessage.set('Failed to search doctors.');
          this.isSearching.set(false);
        },
        next: (data) => {
          this.doctors.set(data);
          this.hasSearched.set(true);
          this.isSearching.set(false);
        },
      });
  }
}
