import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-card-header',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="compact-header">
      <span class="badge" [ngClass]="type.toLowerCase()">
        <!-- Type Icons -->
        <svg *ngIf="type === 'Public'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="4 7 4 4 20 4 20 7"></polyline><line x1="9" y1="20" x2="15" y2="20"></line><line x1="12" y1="4" x2="12" y2="20"></line></svg>
        <svg *ngIf="type === 'Poll'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="20" x2="18" y2="10"></line><line x1="12" y1="20" x2="12" y2="4"></line><line x1="6" y1="20" x2="6" y2="14"></line></svg>
        <svg *ngIf="type === 'Survey'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline><line x1="16" y1="13" x2="8" y2="13"></line><line x1="16" y1="17" x2="8" y2="17"></line><polyline points="10 9 9 9 8 9"></polyline></svg>
        {{ type === 'Public' ? 'Announcement' : type }}
      </span>
      <div style="display: flex; flex-direction: column; align-items: flex-end;">
         <span class="compact-date">{{ date | date:'MMM d, y, h:mm a' }}</span>
         <span *ngIf="type === 'Poll' || type === 'Survey'" class="compact-date" style="color: #ef4444; font-weight: 600; font-size: 0.75rem;">Deadline: {{ expiryDate | date:'yyyy-MM-dd' }}</span>
      </div>
    </div>
  `
})
export class CardHeaderComponent {
  @Input() type!: string;
  @Input() date!: Date | string | null;
  @Input() expiryDate!: Date | string | null;
}

@Component({
  selector: 'app-attachment-chip',
  standalone: true,
  imports: [CommonModule],
  template: `
    <a [href]="'/document-viewer/' + documentId" target="_blank" class="attachment-chip" tabindex="0" [attr.aria-label]="'View attached document ' + documentName">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path><circle cx="12" cy="12" r="3"></circle></svg>
      <span class="filename">{{ documentName }}</span>
    </a>
  `
})
export class AttachmentChipComponent {
  @Input() documentId!: number | undefined | null;
  @Input() documentName!: string | undefined | null;
}

@Component({
  selector: 'app-poll-ui',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="poll-compact-ui">
      <!-- Voted Badge -->
      <div *ngIf="hasVoted" class="voted-badge" style="margin-bottom: 1rem;">
         <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>
         You have already voted
      </div>

      <!-- Voting Options (Always visible) -->
      <div class="poll-voting-area">
        <label *ngFor="let opt of options" class="poll-row" 
               [class.selected]="selectedOptionId === opt.optionId" 
               [class.disabled]="hasVoted"
               tabindex="0">
          <input type="radio" 
                 [name]="'poll-' + announcementId" 
                 [value]="opt.optionId" 
                 [checked]="selectedOptionId === opt.optionId"
                 [disabled]="hasVoted"
                 style="width: 16px; height: 16px; cursor: pointer; flex-shrink: 0;"
                 (change)="selectOption(opt.optionId)">
          <span class="opt-text">{{ opt.optionText }}</span>
        </label>
        
        <!-- Submit Button (Only if NOT voted) -->
        <div class="poll-action-bar" *ngIf="!hasVoted">
          <button class="btn-compact-primary" [disabled]="!selectedOptionId" (click)="submit()">Submit Vote</button>
        </div>
      </div>

      <!-- Results (Only if voted) -->
      <div *ngIf="hasVoted" class="poll-results-area" style="margin-top: 1.5rem;">
        <div class="poll-result-row" *ngFor="let opt of options">
          <div class="result-header">
            <span class="opt-name">{{ opt.optionText }}</span>
            <span class="opt-pct">{{ getPercentage(opt.votes) }}%</span>
          </div>
          <div class="progress-bar-bg">
            <div class="progress-bar-fill" [style.width.%]="getPercentage(opt.votes)"></div>
          </div>
        </div>
        <p class="poll-total-text">Total Votes: {{ totalVotes }}</p>
      </div>
    </div>
  `
})
export class PollUiComponent {
  @Input() announcementId!: number;
  @Input() options!: any[];
  @Input() hasVoted!: boolean;
  @Input() totalVotes!: number;
  @Input() selectedOptionId: number | null = null;
  @Output() voteSubmitted = new EventEmitter<number>();

  selectOption(id: number) {
    if (!this.hasVoted) {
      this.selectedOptionId = id;
    }
  }

  submit() {
    if (this.selectedOptionId) {
      this.voteSubmitted.emit(this.selectedOptionId);
    }
  }

  getPercentage(votes: number): number {
    if (this.totalVotes === 0) return 0;
    return Math.round((votes / this.totalVotes) * 100);
  }
}
