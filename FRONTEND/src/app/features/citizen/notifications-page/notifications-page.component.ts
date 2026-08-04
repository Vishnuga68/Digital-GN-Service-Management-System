import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { NotificationService, NotificationModel } from '../../../core/services/notification.service';

@Component({
  selector: 'app-notifications-page',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './notifications-page.component.html',
  styleUrls: ['./notifications-page.component.scss']
})
export class NotificationsPageComponent implements OnInit {
  private router = inject(Router);
  notificationService = inject(NotificationService);

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

  ngOnInit() {
    this.notificationService.fetchNotifications().subscribe();
    this.notificationService.notifications$.subscribe(list => {
      this.notifications = list.map((n: NotificationModel) => ({
        id: n.id,
        type: n.type,
        title: n.title,
        message: n.message,
        time: this.formatTime(n.createdAt),
        read: n.isRead,
        actionUrl: n.actionUrl
      }));
    });
  }

  formatTime(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    const diffMs = new Date().getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins} mins ago`;
    if (diffHours < 24) return `${diffHours} hours ago`;
    return date.toLocaleDateString();
  }

  notifications: any[] = [];

  categoryMapping: Record<string, string> = {
    'approved': 'Approval',
    'rejected': 'Rejection',
    'pending': 'Status Update',
    'appointment': 'Appointment',
    'chat': 'New Message',
    'info': 'Information'
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
    this.notificationService.markAsRead(id).subscribe();
  }

  markAllAsRead() {
    this.notificationService.markAllAsRead().subscribe();
  }

  deleteNotification(id: number) {
    this.notificationService.deleteNotification(id).subscribe();
  }

  viewNotification(id: number) {
    const notif = this.notifications.find(n => n.id === id);
    if (notif) {
      this.notificationService.markAsRead(id).subscribe();
      if (notif.actionUrl) {
        this.router.navigateByUrl(notif.actionUrl);
      }
    }
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
    this.selectedIds.forEach(id => {
      this.notificationService.deleteNotification(id).subscribe();
    });
    this.selectedIds.clear();
    this.isSelectionMode = false;
    this.isBulkDeleteConfirmVisible = false;
  }
}
