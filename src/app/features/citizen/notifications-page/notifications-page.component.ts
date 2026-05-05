import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-notifications-page',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './notifications-page.component.html',
  styleUrls: ['./notifications-page.component.scss']
})
export class NotificationsPageComponent {
  activeFilter = 'all';
  searchQuery = '';
  isSelectionMode = false;
  selectedIds: Set<number> = new Set();
  activeDeleteConfirmId: number | null = null;
  isBulkDeleteConfirmVisible = false;
  isSidebarExpanded = true;
  isProfileModalOpen = false;
  isNotificationModalOpen = false;
  citizenName = 'John Doe';
  userImageUrl = ''; // Default profile will be used

  notifications = [
    {
      id: 1,
      type: 'approved',
      title: 'Income Certificate Approved',
      message: 'Your income certificate request has been approved.',
      time: '2 hours ago',
      read: false
    },
    {
      id: 2,
      type: 'rejected',
      title: 'Residence Certificate Rejected',
      message: 'Your request was rejected due to missing documents.',
      time: '5 hours ago',
      read: false
    },
    {
      id: 7,
      type: 'pending',
      title: 'Address Change Pending',
      message: 'Your address change request is being reviewed by the GN officer.',
      time: 'Just now',
      read: false
    },
    {
      id: 8,
      type: 'approved',
      title: 'Digital ID Issued',
      message: 'Your new digital identity has been issued successfully.',
      time: '3 hours ago',
      read: false
    },
    {
      id: 3,
      type: 'mention',
      title: 'You were mentioned in a request',
      message: 'GN officer mentioned you in Request #1023.',
      time: 'Yesterday',
      read: true
    },
    {
      id: 10,
      type: 'mention',
      title: 'New Comment on Appointment',
      message: 'Officer Sunil added a note to your Tuesday appointment.',
      time: 'Yesterday',
      read: false
    },
    {
      id: 4,
      type: 'info',
      title: 'System Maintenance Notice',
      message: 'The system will be unavailable on Sunday from 10 PM.',
      time: '2 days ago',
      read: true
    },
    {
      id: 5,
      type: 'approved',
      title: 'Address Verification Success',
      message: 'Your address has been verified successfully.',
      time: '3 days ago',
      read: true
    },
    {
      id: 6,
      type: 'info',
      title: 'New Service Available',
      message: 'You can now request Birth Certificates online.',
      time: '1 week ago',
      read: true
    },
    {
      id: 11,
      type: 'info',
      title: 'Holiday Notice',
      message: 'The GN office will be closed this Friday for Poya Day.',
      time: '2 days ago',
      read: true
    }
  ];

  categoryMapping: Record<string, string> = {
    'approved': 'Approval',
    'rejected': 'Rejection',
    'mention': 'Mention',
    'info': 'Information',
    'pending': 'Status Update'
  };

  toggleSidebar() {
    this.isSidebarExpanded = !this.isSidebarExpanded;
  }

  toggleProfileModal() {
    this.isProfileModalOpen = !this.isProfileModalOpen;
  }

  toggleEditProfileModal() {
    this.isProfileModalOpen = false;
    // Navigation logic for edit profile would go here
  }

  toggleNotificationModal() {
    // On this page, the modal might be redundant, but we keep it for shell consistency
    this.isNotificationModalOpen = !this.isNotificationModalOpen;
  }

  get unreadCount() {
    return this.notifications.filter(n => !n.read).length;
  }

  switchLanguage(lang: string) {
    console.log('Switching language to:', lang);
  }

  logout() {
    console.log('Logging out...');
  }

  setFilter(filter: string) {
    this.activeFilter = filter;
  }

  get filteredNotifications() {
    let filtered = this.notifications;

    // Filter by Tab
    if (this.activeFilter === 'unread') {
      filtered = filtered.filter(n => !n.read);
    } else if (this.activeFilter === 'mention') {
      filtered = filtered.filter(n => n.type === 'mention');
    }

    // Filter by Search
    if (this.searchQuery) {
      const query = this.searchQuery.toLowerCase();
      filtered = filtered.filter(n => 
        n.title.toLowerCase().includes(query) || 
        n.message.toLowerCase().includes(query)
      );
    }

    return filtered;
  }

  get groupedNotifications() {
    const filtered = this.filteredNotifications;
    const groups: { label: string, items: any[] }[] = [
      { label: 'Today', items: [] },
      { label: 'Yesterday', items: [] },
      { label: 'Earlier', items: [] }
    ];

    filtered.forEach(n => {
      const timeLower = n.time.toLowerCase();
      if (timeLower.includes('hour') || timeLower.includes('minute') || timeLower.includes('now')) {
        groups[0].items.push({ ...n, category: this.categoryMapping[n.type] || 'Uncategorized' });
      } else if (timeLower === 'yesterday') {
        groups[1].items.push({ ...n, category: this.categoryMapping[n.type] || 'Uncategorized' });
      } else {
        groups[2].items.push({ ...n, category: this.categoryMapping[n.type] || 'Uncategorized' });
      }
    });

    return groups.filter(g => g.items.length > 0);
  }

  markAsRead(id: number) {
    const notif = this.notifications.find(n => n.id === id);
    if (notif) notif.read = true;
  }

  markAllAsRead() {
    this.notifications.forEach(n => n.read = true);
  }

  deleteNotification(id: number) {
    this.notifications = this.notifications.filter(n => n.id !== id);
  }

  viewNotification(id: number) {
    console.log('Viewing notification:', id);
    this.markAsRead(id);
    // Add navigation or modal logic here if needed
  }

  toggleSelectionMode() {
    this.isSelectionMode = !this.isSelectionMode;
    if (!this.isSelectionMode) {
      this.selectedIds.clear();
    }
  }

  toggleSelect(id: number, event?: Event) {
    if (event) {
      event.stopPropagation();
    }
    if (this.selectedIds.has(id)) {
      this.selectedIds.delete(id);
    } else {
      this.selectedIds.add(id);
    }
  }

  isSelected(id: number): boolean {
    return this.selectedIds.has(id);
  }

  isAllSelected(): boolean {
    const currentNotifs = this.filteredNotifications;
    return currentNotifs.length > 0 && currentNotifs.every(n => this.selectedIds.has(n.id));
  }

  selectAll(event: any) {
    const checked = event.target.checked;
    if (checked) {
      this.filteredNotifications.forEach(n => this.selectedIds.add(n.id));
    } else {
      this.selectedIds.clear();
    }
  }

  showDeleteConfirm(id: number, event: Event) {
    event.stopPropagation();
    this.activeDeleteConfirmId = id;
  }

  hideDeleteConfirm(event?: Event) {
    if (event) event.stopPropagation();
    this.activeDeleteConfirmId = null;
  }

  showBulkDeleteConfirm(event: Event) {
    event.stopPropagation();
    if (this.selectedIds.size > 0) {
      this.isBulkDeleteConfirmVisible = true;
    }
  }

  hideBulkDeleteConfirm(event?: Event) {
    if (event) event.stopPropagation();
    this.isBulkDeleteConfirmVisible = false;
  }

  confirmBulkDelete() {
    this.notifications = this.notifications.filter(n => !this.selectedIds.has(n.id));
    this.selectedIds.clear();
    this.isSelectionMode = false;
    this.isBulkDeleteConfirmVisible = false;
  }
}
