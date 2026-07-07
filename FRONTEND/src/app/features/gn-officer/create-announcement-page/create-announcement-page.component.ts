import { Component, OnInit } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AnnouncementService, GnAnnouncementRequestDTO, GnDocumentResponseDTO } from '../../../core/services/announcement.service';

@Component({
  selector: 'app-create-announcement-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './create-announcement-page.component.html',
  styleUrls: ['./create-announcement-page.component.scss']
})
export class CreateAnnouncementPageComponent implements OnInit {
  isEditMode = false;
  editId: number | null = null;
  
  currentAnnouncement: any = {
    type: 'Public',
    status: 'Draft',
    title: '',
    description: '',
    surveyLink: '',
    estimatedTime: '',
    documentId: null,
    expiryDate: new Date(new Date().setDate(new Date().getDate() + 30)).toISOString().split('T')[0]
  };

  pollOptionsInput: string[] = ['', ''];
  documents: GnDocumentResponseDTO[] = [];
  
  showAlertModal = false;
  alertType: 'success' | 'error' = 'success';
  alertMessage = '';
  isSubmitting = false;

  constructor(
    private announcementService: AnnouncementService,
    private router: Router,
    private route: ActivatedRoute,
    private location: Location
  ) {}

  ngOnInit(): void {
    this.fetchDocuments();
    
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.isEditMode = true;
        this.editId = parseInt(id, 10);
        this.loadAnnouncement(this.editId);
      }
    });
  }

  loadAnnouncement(id: number) {
    this.announcementService.getAnnouncementById(id).subscribe({
      next: (data) => {
        const dExpiry = new Date(data.expiryDate);
        this.currentAnnouncement = {
          type: data.announcementType,
          status: data.status,
          title: data.title,
          description: data.description || (data.announcementType === 'Poll' ? data.pollQuestion : ''),
          surveyLink: data.externalSurveyUrl || '',
          documentId: data.documentId,
          expiryDate: !isNaN(dExpiry.getTime()) ? dExpiry.toISOString().split('T')[0] : ''
        };
        
        if (data.announcementType === 'Poll' && data.pollOptions && data.pollOptions.length > 0) {
          this.pollOptionsInput = data.pollOptions.map(po => po.optionText);
        }
      },
      error: () => this.showAlert('error', 'Failed to load announcement for editing.')
    });
  }

  fetchDocuments() {
    this.announcementService.getPublishedDocuments().subscribe({
      next: (docs) => {
        this.documents = docs;
      },
      error: (err) => console.error('Failed to load documents', err)
    });
  }

  addPollOption() {
    this.pollOptionsInput.push('');
  }

  removePollOption(index: number) {
    if (this.pollOptionsInput.length > 2) {
      this.pollOptionsInput.splice(index, 1);
    }
  }

  trackByIndex(index: number, item: any): number {
    return index;
  }

  goBack() {
    this.location.back();
  }

  saveAnnouncement() {
    if (!this.currentAnnouncement.title || !this.currentAnnouncement.description || !this.currentAnnouncement.expiryDate) {
      this.showAlert('error', 'Please provide title, description, and expiry date.');
      return;
    }

    if (this.currentAnnouncement.type === 'Poll') {
      const validOptions = this.pollOptionsInput.filter(opt => opt.trim() !== '');
      if (validOptions.length < 2) {
        this.showAlert('error', 'Polls require at least 2 valid options.');
        return;
      }
    }

    if (this.currentAnnouncement.type === 'Survey' && !this.currentAnnouncement.surveyLink) {
      this.showAlert('error', 'Surveys require an external link.');
      return;
    }

    this.isSubmitting = true;
    
    const publishDate = new Date();
    const expiryDate = new Date(this.currentAnnouncement.expiryDate);

    const request: GnAnnouncementRequestDTO = {
      announcementType: this.currentAnnouncement.type,
      title: this.currentAnnouncement.title,
      description: this.currentAnnouncement.description,
      documentId: this.currentAnnouncement.documentId ? Number(this.currentAnnouncement.documentId) : undefined,
      externalSurveyUrl: this.currentAnnouncement.surveyLink,
      pollQuestion: this.currentAnnouncement.type === 'Poll' ? this.currentAnnouncement.description : undefined,
      publishDate: this.currentAnnouncement.status === 'Published' ? publishDate : null,
      expiryDate: expiryDate,
      pollOptions: this.currentAnnouncement.type === 'Poll' ? this.pollOptionsInput.filter(opt => opt.trim() !== '') : undefined
    };

    if (this.isEditMode && this.editId) {
      this.announcementService.updateAnnouncement(this.editId, request).subscribe({
        next: (res) => {
          this.showAlert('success', 'Announcement updated successfully!');
          this.isSubmitting = false;
        },
        error: (err) => {
          console.error('Error updating announcement', err);
          this.showAlert('error', 'Failed to update announcement.');
          this.isSubmitting = false;
        }
      });
    } else {
      this.announcementService.createAnnouncement(request).subscribe({
        next: (res) => {
          this.showAlert('success', 'Announcement created successfully!');
          this.isSubmitting = false;
        },
        error: (err) => {
          console.error('Error creating announcement', err);
          this.showAlert('error', 'Failed to create announcement.');
          this.isSubmitting = false;
        }
      });
    }
  }

  showAlert(type: 'success' | 'error', message: string) {
    this.alertType = type;
    this.alertMessage = message;
    this.showAlertModal = true;
  }

  closeAlert() {
    this.showAlertModal = false;
    if (this.alertType === 'success') {
      if (this.currentAnnouncement.status === 'Draft') {
        this.router.navigate(['/gn-officer/announcements/drafts']);
      } else {
        this.router.navigate(['/gn-officer/announcements']);
      }
    }
  }
}
