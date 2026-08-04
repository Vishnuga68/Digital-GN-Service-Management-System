import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AnnouncementService, GnAnnouncementResponseDTO } from '../../../core/services/announcement.service';
import { CardHeaderComponent, AttachmentChipComponent, PollUiComponent } from './components/announcement-ui';
import { SearchService } from '../../../core/services/search.service';

@Component({
  selector: 'app-announcements-page',
  standalone: true,
  imports: [CommonModule, FormsModule, CardHeaderComponent, AttachmentChipComponent, PollUiComponent],
  templateUrl: './announcements-page.component.html',
  styleUrls: ['./announcements-page.component.scss']
})
export class AnnouncementsPageComponent implements OnInit {
  announcements: GnAnnouncementResponseDTO[] = [];
  filteredAnnouncements: GnAnnouncementResponseDTO[] = [];
  activeTab: string = 'All';
  isLoading: boolean = true;

  // Poll Vote logic
  votedPolls = new Set<number>(); // Track which polls the user voted in this session
  votedPollsData = new Map<number, number>(); // Map<announcementId, optionId>
  totalVotesCache = new Map<number, number>();
  
  private searchService = inject(SearchService);
  searchQuery = '';

  constructor(private announcementService: AnnouncementService) {}

  ngOnInit(): void {
    this.searchService.searchQuery$.subscribe(query => {
      this.searchQuery = query;
      this.filterAnnouncements();
    });
    this.loadAnnouncements();
  }

  loadAnnouncements() {
    this.isLoading = true;
    this.announcementService.getCitizenAnnouncements().subscribe({
      next: (data) => {
        this.announcements = data;
        
        // Populate total votes cache for UI
        this.announcements.forEach(a => {
          if (a.announcementType === 'Poll' && a.pollOptions) {
            let total = a.pollOptions.reduce((sum, opt) => sum + opt.votes, 0);
            this.totalVotesCache.set(a.announcementId, total);
          }
        });

        this.filterAnnouncements();
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load announcements', err);
        this.isLoading = false;
      }
    });
  }

  setTab(tab: string) {
    this.activeTab = tab;
    this.filterAnnouncements();
  }

  filterAnnouncements() {
    let filtered = [...this.announcements];

    if (this.activeTab !== 'All') {
      const typeMap: any = {
        'Public': 'Public',
        'Polls': 'Poll',
        'Surveys': 'Survey'
      };
      filtered = filtered.filter(a => a.announcementType === typeMap[this.activeTab]);
    }

    if (this.searchQuery) {
      const q = this.searchQuery.toLowerCase();
      filtered = filtered.filter(a => 
        (a.title || '').toLowerCase().includes(q) || 
        (a.description || '').toLowerCase().includes(q) ||
        (a.announcementType || '').toLowerCase().includes(q)
      );
    }
    
    // Sort by latest date
    filtered.sort((a, b) => {
      const dateA = a.publishDate ? new Date(a.publishDate).getTime() : new Date(a.createdAt).getTime();
      const dateB = b.publishDate ? new Date(b.publishDate).getTime() : new Date(b.createdAt).getTime();
      return dateB - dateA;
    });

    this.filteredAnnouncements = filtered;
  }

  submitPoll(announcementId: number, optionId: number) {
    const ann = this.announcements.find(a => a.announcementId === announcementId);
    if (ann?.hasVoted) return; // Prevent double voting
    
    this.announcementService.submitVote(announcementId, optionId).subscribe({
      next: () => {
        // Fetch updated results from DB
        this.announcementService.getAnnouncementById(announcementId).subscribe({
          next: (updatedAnn) => {
             const index = this.announcements.findIndex(a => a.announcementId === announcementId);
             if (index !== -1) {
               this.announcements[index] = updatedAnn;
               
               // Update total votes cache
               let total = updatedAnn.pollOptions.reduce((sum, opt) => sum + opt.votes, 0);
               this.totalVotesCache.set(announcementId, total);
               
               this.filterAnnouncements();
             }
          }
        });
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to submit vote. You might have already voted.');
      }
    });
  }

  hasVoted(announcementId: number): boolean {
    const ann = this.announcements.find(a => a.announcementId === announcementId);
    return ann?.hasVoted || false;
  }
  
  getUserVotedOptionId(announcementId: number): number | null {
    const ann = this.announcements.find(a => a.announcementId === announcementId);
    return ann?.userVotedOptionId || null;
  }

  getTotalVotes(announcementId: number): number {
    return this.totalVotesCache.get(announcementId) || 0;
  }
}
