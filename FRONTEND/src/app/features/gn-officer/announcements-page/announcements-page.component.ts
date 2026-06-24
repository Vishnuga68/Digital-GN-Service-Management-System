import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export type AnnouncementType = 'Personal' | 'Public' | 'Poll' | 'Survey';

export interface Announcement {
  id: string;
  type: AnnouncementType;
  title: string;
  description: string;
  date: Date;
  isRead?: boolean;
  priority?: boolean;
  
  // Poll Specific
  options?: { id: string, text: string, votes?: number, percentage?: number }[];
  userVotedId?: string;
  totalVotes?: number;

  // Survey Specific
  estimatedTime?: string;
  surveyLink?: string;
  
  // Personal Specific
  targetCitizens?: string[];
  
  // Default/All Types Data
  actionLabel?: string;
  actionLink?: string;
  attachmentName?: string;
  attachmentFile?: File | null;
  status: 'Published' | 'Draft';
}

@Component({
  selector: 'app-announcements-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './announcements-page.component.html',
  styleUrls: ['./announcements-page.component.scss']
})
export class AnnouncementsPageComponent implements OnInit {
  activeTab: string = 'All';
  tabs: string[] = ['All', 'Public', 'Personal', 'Polls', 'Surveys'];
  
  announcements: Announcement[] = [];
  filteredAnnouncements: Announcement[] = [];
  
  // Modal State
  isModalOpen = false;
  isEditMode = false;
  currentAnnouncement: Partial<Announcement> = {};
  
  // Custom Confirmation Modal
  isConfirmModalOpen = false;
  confirmTitle = '';
  confirmMessage = '';
  confirmAction: () => void = () => {};
  confirmType: 'delete' | 'publish' = 'delete';

  // Results Modal State
  isResultsModalOpen = false;
  resultsAnnouncement: Announcement | null = null;
  surveyResponses = [
    { id: 'R001', citizen: 'Kamal Perera', date: new Date(), response: 'The new system is much easier to use.' },
    { id: 'R002', citizen: 'Nimal Silva', date: new Date(), response: 'I prefer the physical forms for some requests.' },
    { id: 'R003', citizen: 'Sunil Shantha', date: new Date(), response: 'Great initiative by the GN office!' }
  ];
  
  // Data for Selectors
  mockCitizens = [
    { id: 'C001', name: 'Kamal Perera', address: '12 Temple Rd' },
    { id: 'C002', name: 'Nimal Silva', address: '45 School Ln' },
    { id: 'C003', name: 'Sunil Shantha', address: '8 Main Jct' }
  ];
  
  mockDocuments = [
    'Residence Certificate - Form A',
    'Character Certificate Request Form',
    'Business Registration Guidelines 2024'
  ];

  // Specific Form states
  selectedCitizens: string[] = [];
  citizenSearchTerm: string = '';
  filteredMockCitizens = this.mockCitizens;
  pollOptionsInput: string[] = [''];
  selectedAttachmentType: 'none' | 'upload' | 'system' = 'none';
  selectedSystemDoc: string = '';
  uploadedFile: File | null = null;

  // For Alert Modal
  showAlertModal = false;
  alertMessage = '';
  alertType: 'success' | 'error' = 'success';

  ngOnInit(): void {
    this.loadMockData();
    this.filterAnnouncements();
  }

  loadMockData() {
    this.announcements = [
      {
        id: '1',
        type: 'Public',
        title: 'Community Shramadana Campaign',
        description: 'The monthly village cleanup campaign will take place this Sunday at the Community Center. All households are requested strictly to participate.',
        date: new Date(Date.now() - 1000 * 60 * 60 * 48), // 2 days ago
        attachmentName: 'Schedule_Details.pdf',
        status: 'Published'
      },
      {
        id: '2',
        type: 'Poll',
        title: 'New Street Light Placement Preference',
        description: 'Where should the new solar streetlights be prioritized in our division?',
        date: new Date(Date.now() - 1000 * 60 * 60 * 72), // 3 days ago
        totalVotes: 145,
        options: [
          { id: 'opt1', text: 'Main Junction', votes: 80, percentage: 55 },
          { id: 'opt2', text: 'School Lane', votes: 45, percentage: 31 },
          { id: 'opt3', text: 'Temple Road', votes: 20, percentage: 14 }
        ],
        status: 'Published'
      },
      {
        id: '3',
        type: 'Public',
        title: 'Water Supply Interruption Notice',
        description: 'The main water supply will be interrupted tomorrow from 8:00 AM to 5:00 PM due to pipeline maintenance work by the Water Board.',
        date: new Date(Date.now() - 1000 * 60 * 60 * 96), // 4 days ago
        status: 'Published'
      },
      {
        id: '4',
        type: 'Survey',
        title: 'Annual Livelihood Assessment Survey 2024',
        description: 'Please complete this mandatory survey to update the divisional secretariat records regarding employment and income sources.',
        date: new Date(),
        estimatedTime: '5 mins',
        status: 'Draft'
      }
    ];
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
        'Personal': 'Personal',
        'Polls': 'Poll',
        'Surveys': 'Survey'
      };
      filtered = filtered.filter(a => a.type === typeMap[this.activeTab]);
    }
    
    filtered.sort((a, b) => b.date.getTime() - a.date.getTime());
    this.filteredAnnouncements = filtered;
  }

  openModal(announcement?: Announcement) {
    this.isModalOpen = true;
    this.resetFormState();
    
    if (announcement) {
      this.isEditMode = true;
      this.currentAnnouncement = { ...announcement };
      
      // Load specific states
      if (announcement.type === 'Personal' && announcement.targetCitizens) {
        this.selectedCitizens = [...announcement.targetCitizens];
      }
      if (announcement.type === 'Poll' && announcement.options) {
        this.pollOptionsInput = announcement.options.map(opt => opt.text);
      }
      if (announcement.attachmentName) {
         if (this.mockDocuments.includes(announcement.attachmentName)) {
            this.selectedAttachmentType = 'system';
            this.selectedSystemDoc = announcement.attachmentName;
         } else {
            this.selectedAttachmentType = 'upload';
         }
      }
    } else {
      this.isEditMode = false;
      this.currentAnnouncement = {
        type: 'Public',
        status: 'Draft'
      };
    }
  }

  resetFormState() {
     this.selectedCitizens = [];
     this.citizenSearchTerm = '';
     this.filteredMockCitizens = [...this.mockCitizens];
     this.pollOptionsInput = ['', ''];
     this.selectedAttachmentType = 'none';
     this.selectedSystemDoc = '';
     this.uploadedFile = null;
  }

  closeModal() {
    this.isModalOpen = false;
  }

  addPollOption() {
      this.pollOptionsInput.push('');
  }

  trackByIndex(index: number, obj: any): any {
      return index;
  }

  removePollOption(index: number) {
      if (this.pollOptionsInput.length > 2) {
          this.pollOptionsInput.splice(index, 1);
      }
  }

  handleFileUpload(event: any) {
      if (event.target.files.length > 0) {
          this.uploadedFile = event.target.files[0];
          this.currentAnnouncement.attachmentName = this.uploadedFile?.name;
      }
  }

  toggleCitizenSelection(citizenId: string) {
      const index = this.selectedCitizens.indexOf(citizenId);
      if (index > -1) {
          this.selectedCitizens.splice(index, 1);
      } else {
          this.selectedCitizens.push(citizenId);
      }
  }

  filterTargetCitizens() {
    const term = this.citizenSearchTerm.toLowerCase().trim();
    if (!term) {
      this.filteredMockCitizens = [...this.mockCitizens];
      return;
    }
    this.filteredMockCitizens = this.mockCitizens.filter(c => 
      c.name.toLowerCase().includes(term) || c.id.toLowerCase().includes(term)
    );
  }

  saveAnnouncement() {
    if (!this.currentAnnouncement.title || !this.currentAnnouncement.description) {
       this.showToast('Please fill all required fields.', 'error');
       return;
    }

    if (this.currentAnnouncement.type === 'Personal' && this.selectedCitizens.length === 0) {
       this.showToast('Please select at least one citizen for a personal announcement.', 'error');
       return;
    }

    if (this.currentAnnouncement.type === 'Poll') {
       const validOptions = this.pollOptionsInput.filter(o => o.trim() !== '');
       if (validOptions.length < 2) {
           this.showToast('A poll requires at least 2 options.', 'error');
           return;
       }
       this.currentAnnouncement.options = validOptions.map((text, i) => ({
           id: `opt${i}`, text, votes: 0, percentage: 0
       }));
    }

    if (this.currentAnnouncement.type === 'Personal') {
        this.currentAnnouncement.targetCitizens = [...this.selectedCitizens];
    }
    
    if (this.selectedAttachmentType === 'system' && this.selectedSystemDoc) {
        this.currentAnnouncement.attachmentName = this.selectedSystemDoc;
    } else if (this.selectedAttachmentType === 'none') {
        this.currentAnnouncement.attachmentName = undefined;
    } else if (this.selectedAttachmentType === 'upload' && this.uploadedFile) {
        this.currentAnnouncement.attachmentName = this.uploadedFile.name;
    }

    if (this.isEditMode) {
      const index = this.announcements.findIndex(a => a.id === this.currentAnnouncement.id);
      if (index !== -1) {
         this.announcements[index] = { ...this.announcements[index], ...this.currentAnnouncement } as Announcement;
      }
      this.showToast('Announcement updated successfully.', 'success');
    } else {
      const newAnn: Announcement = {
        ...this.currentAnnouncement,
        id: Date.now().toString(),
        type: this.currentAnnouncement.type as AnnouncementType || 'Public',
        title: this.currentAnnouncement.title || '',
        description: this.currentAnnouncement.description || '',
        date: new Date(),
        status: this.currentAnnouncement.status || 'Published'
      } as Announcement;
      this.announcements.unshift(newAnn);
      this.showToast('Announcement created systematically.', 'success');
    }
    
    this.filterAnnouncements();
    this.closeModal();
  }

  deleteAnnouncement(id: string) {
    this.confirmTitle = 'Delete Announcement';
    this.confirmMessage = 'Are you sure you want to permanently delete this announcement? This action cannot be undone.';
    this.confirmType = 'delete';
    this.confirmAction = () => {
      this.announcements = this.announcements.filter(a => a.id !== id);
      this.filterAnnouncements();
      this.showToast('Announcement removed.', 'success');
    };
    this.isConfirmModalOpen = true;
  }

  publishAnnouncement(ann: Announcement) {
    this.confirmTitle = 'Publish Announcement';
    this.confirmMessage = 'Ready to share this with your division? Citizens will receive notifications once published.';
    this.confirmType = 'publish';
    this.confirmAction = () => {
       ann.status = 'Published';
       ann.date = new Date();
       this.filterAnnouncements();
       this.showToast('Announcement published to citizens.', 'success');
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

  openResultsModal(ann: Announcement) {
    this.resultsAnnouncement = ann;
    this.isResultsModalOpen = true;
  }

  closeResultsModal() {
    this.isResultsModalOpen = false;
    this.resultsAnnouncement = null;
  }

  exportResults() {
    if (!this.resultsAnnouncement) return;
    
    let csvContent = "data:text/csv;charset=utf-8,";
    
    if (this.resultsAnnouncement.type === 'Poll') {
      csvContent += "Option,Votes,Percentage\n";
      this.resultsAnnouncement.options?.forEach(opt => {
        csvContent += `"${opt.text}",${opt.votes},${opt.percentage}%\n`;
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
}
