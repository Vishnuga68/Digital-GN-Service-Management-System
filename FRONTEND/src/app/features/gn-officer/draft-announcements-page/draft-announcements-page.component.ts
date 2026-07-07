import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AnnouncementService, GnAnnouncementResponseDTO } from '../../../core/services/announcement.service';

@Component({
  selector: 'app-draft-announcements-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './draft-announcements-page.component.html',
  styleUrls: ['./draft-announcements-page.component.scss']
})
export class DraftAnnouncementsPageComponent implements OnInit {
  activeTab: string = 'All';
  tabs: string[] = ['All', 'Public', 'Polls', 'Surveys'];
  
  announcements: GnAnnouncementResponseDTO[] = [];
  filteredAnnouncements: GnAnnouncementResponseDTO[] = [];
  
  // Custom Confirmation Modal
  isConfirmModalOpen = false;
  confirmTitle = '';
  confirmMessage = '';
  confirmAction: () => void = () => {};
  confirmType: 'delete' | 'publish' = 'delete';

  // Results Modal State
  isResultsModalOpen = false;
  resultsAnnouncement: GnAnnouncementResponseDTO | null = null;
  surveyResponses = [
    { id: 'R001', citizen: 'Kamal Perera', date: new Date(), response: 'The new system is much easier to use.' },
    { id: 'R002', citizen: 'Nimal Silva', date: new Date(), response: 'I prefer the physical forms for some requests.' }
  ];
  
  // For Alert Modal
  showAlertModal = false;
  alertMessage = '';
  alertType: 'success' | 'error' = 'success';

  constructor(
    private announcementService: AnnouncementService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadAnnouncements();
  }

  loadAnnouncements() {
    this.announcementService.getGnAnnouncements().subscribe({
      next: (data) => {
        this.announcements = data;
        this.filterAnnouncements();
      },
      error: (err) => console.error('Failed to load announcements', err)
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
    
    filtered = filtered.filter(a => a.status === 'Draft' || a.status === 'Expired');

    // Sort by createdAt descending
    filtered.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
    this.filteredAnnouncements = filtered;
  }

  openNewAnnouncementPage() {
    this.router.navigate(['/gn-officer/announcements/new']);
  }

  goBack() {
    this.router.navigate(['/gn-officer/announcements']);
  }

  deleteAnnouncement(id: number) {
    this.confirmTitle = 'Delete Announcement';
    this.confirmMessage = 'Are you sure you want to permanently delete this announcement? This action cannot be undone.';
    this.confirmType = 'delete';
    this.confirmAction = () => {
      this.announcementService.deleteAnnouncement(id).subscribe({
        next: () => {
          this.loadAnnouncements();
          this.showToast('Announcement removed.', 'success');
        },
        error: () => this.showToast('Failed to delete.', 'error')
      });
    };
    this.isConfirmModalOpen = true;
  }

  publishAnnouncement(ann: GnAnnouncementResponseDTO) {
    this.confirmTitle = 'Publish Announcement';
    this.confirmMessage = 'Ready to share this with your division? Citizens will receive notifications once published.';
    this.confirmType = 'publish';
    this.confirmAction = () => {
      // Just update status to Published by tweaking dates
      const now = new Date();
      const expiry = new Date(ann.expiryDate);
      if (expiry <= now) expiry.setDate(now.getDate() + 30); // Extend expiry if it was expired
      
      const updateReq = {
        announcementType: ann.announcementType,
        title: ann.title,
        description: ann.description,
        documentId: ann.documentId,
        externalSurveyUrl: ann.externalSurveyUrl,
        pollQuestion: ann.pollQuestion,
        publishDate: now,
        expiryDate: expiry,
        pollOptions: ann.pollOptions?.map(po => po.optionText)
      };

      this.announcementService.updateAnnouncement(ann.announcementId, updateReq).subscribe({
        next: () => {
          this.loadAnnouncements();
          this.showToast('Announcement published to citizens.', 'success');
        },
        error: () => this.showToast('Failed to publish.', 'error')
      });
    };
    this.isConfirmModalOpen = true;
  }

  executeConfirm() {
    this.confirmAction();
    this.isConfirmModalOpen = false;
  }

  closeConfirmModal() {
    this.isConfirmModalOpen = false;
  }

  editAnnouncement(id: number) {
    this.router.navigate(['/gn-officer/announcements/edit', id]);
  }

  openResultsModal(ann: GnAnnouncementResponseDTO) {
    if (ann.announcementType === 'Poll') {
      this.announcementService.getResults(ann.announcementId).subscribe({
        next: (results) => {
          ann.pollOptions = results.map((r: any, i: number) => ({
            optionId: i, // Dummy ID just for charting logic
            optionText: r.option,
            votes: r.votes
          }));
          this.resultsAnnouncement = ann;
          this.isResultsModalOpen = true;
        },
        error: () => this.showToast('Failed to load real-time results.', 'error')
      });
    } else {
      this.resultsAnnouncement = ann;
      this.isResultsModalOpen = true;
    }
  }

  closeResultsModal() {
    this.isResultsModalOpen = false;
    this.resultsAnnouncement = null;
  }

  exportResults() {
    if (!this.resultsAnnouncement) return;
    
    let csvContent = "data:text/csv;charset=utf-8,";
    
    if (this.resultsAnnouncement.announcementType === 'Poll') {
      csvContent += "Option,Votes\n";
      this.resultsAnnouncement.pollOptions?.forEach(opt => {
        csvContent += `"${opt.optionText}",${opt.votes}\n`;
      });
    } else {
      csvContent += "Respondent,Date,Response\n";
      this.surveyResponses.forEach(r => {
        csvContent += `"${r.citizen}","${r.date.toLocaleDateString()}","${r.response}"\n`;
      });
    }

    const encodedUri = encodeURI(csvContent);
    const link = document.createElement("a");
    link.setAttribute("href", encodedUri);
    link.setAttribute("download", `${this.resultsAnnouncement.title.replace(/\s+/g, '_')}_Results.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    this.showToast('Results exported as CSV successfully.', 'success');
  }

  showToast(message: string, type: 'success' | 'error') {
     this.alertMessage = message;
     this.alertType = type;
     this.showAlertModal = true;
  }

  closeAlert() {
     this.showAlertModal = false;
  }

  getPieChartStyle(options: { optionText: string, votes: number }[]): string {
    if (!options || options.length === 0) return 'conic-gradient(#e2e8f0 0% 100%)';
    const totalVotes = options.reduce((sum, o) => sum + o.votes, 0);
    if (totalVotes === 0) return 'conic-gradient(#e2e8f0 0% 100%)';
    
    const colors = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899', '#06b6d4'];
    let gradientParts: string[] = [];
    let currentPercentage = 0;
    
    options.forEach((opt, index) => {
      if (opt.votes > 0) {
        const percentage = (opt.votes / totalVotes) * 100;
        const color = colors[index % colors.length];
        gradientParts.push(`${color} ${currentPercentage}% ${currentPercentage + percentage}%`);
        currentPercentage += percentage;
      }
    });
    
    return `conic-gradient(${gradientParts.join(', ')})`;
  }
}
