import { Component, input } from '@angular/core';
import { SmartSearchResult } from '../../../../../models/med-vault-model';
import { formatDate } from '../../../../../shared/utils/format-date';

@Component({
  selector: 'app-smart-search-result-card',
  standalone: false,
  templateUrl: './smart-search-result-card.component.html',
  styleUrl: './smart-search-result-card.component.scss',
})
export class SmartSearchResultCardComponent {
  public readonly result = input.required<SmartSearchResult>();

  public formattedReportDate(): string {
    return formatDate(this.result().reportDate);
  }

  public formattedUploadDate(): string {
    return formatDate(this.result().uploadDate);
  }
}
