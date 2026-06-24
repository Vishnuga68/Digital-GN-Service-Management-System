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
  
  // Optional Actions
  actionLabel?: string;
  actionLink?: string;
  attachmentName?: string;
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
  tabs: string[] = ['All', 'Personal', 'Public', 'Polls', 'Surveys'];
  
  announcements: Announcement[] = [];
  filteredAnnouncements: Announcement[] = [];

  ngOnInit(): void {
    this.loadMockData();
    this.filterAnnouncements();
  }

  loadMockData() {
    this.announcements = [
      {
        id: '1',
        type: 'Personal',
        title: 'Document Verification Required',
        description: 'Please visit the GN office tomorrow between 9 AM and 11 AM to verify your submitted NIC copy for the residence certificate.',
        date: new Date(Date.now() - 1000 * 60 * 60 * 2), // 2 hours ago
        priority: true,
        isRead: false,
        actionLabel: 'Acknowledge'
      },
      {
        id: '2',
        type: 'Personal',
        title: 'GN Officer Home Visit Scheduled',
        description: 'The GN officer will visit your home address for the character assessment process on Friday at 4:00 PM.',
        date: new Date(Date.now() - 1000 * 60 * 60 * 24), // 1 day ago
        priority: true,
        isRead: true,
        actionLabel: 'View Details'
      },
      {
        id: '3',
        type: 'Public',
        title: 'Community Shramadana Campaign',
        description: 'The monthly village cleanup campaign will take place this Sunday at the Community Center. All households are requested strictly to participate.',
        date: new Date(Date.now() - 1000 * 60 * 60 * 48), // 2 days ago
        attachmentName: 'Schedule_Details.pdf'
      },
      {
        id: '4',
        type: 'Poll',
        title: 'New Street Light Placement Preference',
        description: 'Where should the new solar streetlights be prioritized in our division?',
        date: new Date(Date.now() - 1000 * 60 * 60 * 72), // 3 days ago
        totalVotes: 145,
        options: [
          { id: 'opt1', text: 'Main Junction', votes: 80, percentage: 55 },
          { id: 'opt2', text: 'School Lane', votes: 45, percentage: 31 },
          { id: 'opt3', text: 'Temple Road', votes: 20, percentage: 14 }
        ]
      },
      {
        id: '5',
        type: 'Public',
        title: 'Water Supply Interruption Notice',
        description: 'The main water supply will be interrupted tomorrow from 8:00 AM to 5:00 PM due to pipeline maintenance work by the Water Board.',
        date: new Date(Date.now() - 1000 * 60 * 60 * 96) // 4 days ago
      },
      {
        id: '6',
        type: 'Survey',
        title: 'Annual Livelihood Assessment Survey 2024',
        description: 'Please complete this mandatory survey to update the divisional secretariat records regarding employment and income sources.',
        date: new Date(Date.now() - 1000 * 60 * 60 * 120), // 5 days ago
        estimatedTime: '5 mins'
      }
    ];
  }

  setTab(tab: string) {
    this.activeTab = tab;
    this.filterAnnouncements();
  }

  filterAnnouncements() {
    let filtered = [...this.announcements];

    switch (this.activeTab) {
      case 'Personal':
        filtered = filtered.filter(a => a.type === 'Personal');
        break;
      case 'Public':
        filtered = filtered.filter(a => a.type === 'Public');
        break;
      case 'Polls':
        filtered = filtered.filter(a => a.type === 'Poll');
        break;
      case 'Surveys':
        filtered = filtered.filter(a => a.type === 'Survey');
        break;
      default:
        // 'All' - Personal usually at the top
        filtered.sort((a, b) => {
          if (a.type === 'Personal' && b.type !== 'Personal') return -1;
          if (a.type !== 'Personal' && b.type === 'Personal') return 1;
          return b.date.getTime() - a.date.getTime(); // Sort by latest date
        });
        break;
    }
    
    // Sort logic within specific tabs (latest first)
    if (this.activeTab !== 'All') {
       filtered.sort((a, b) => b.date.getTime() - a.date.getTime());
    }

    this.filteredAnnouncements = filtered;
  }

  submitPoll(announcement: Announcement, optionId: string) {
    if (announcement.userVotedId) return; // Prevent double voting
    
    announcement.userVotedId = optionId;
    if(announcement.totalVotes !== undefined) announcement.totalVotes++;
    
    // Recalculate mock percentages
    const option = announcement.options?.find(o => o.id === optionId);
    if (option && option.votes !== undefined && announcement.totalVotes !== undefined) {
      option.votes++;
      if (announcement.options) {
        announcement.options.forEach(opt => {
          if (opt.votes !== undefined && announcement.totalVotes !== undefined) {
             opt.percentage = Math.round((opt.votes / announcement.totalVotes) * 100);
          }
        });
      }
    }
  }

  acknowledge(announcement: Announcement) {
    announcement.isRead = true;
    // Call API to mark as read
  }
}
