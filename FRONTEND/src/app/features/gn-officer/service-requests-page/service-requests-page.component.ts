import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';

export interface ServiceRequest {
  id: string;
  citizenName: string;
  serviceType: string;
  category: string;
  status: string;
  submittedDate: Date;
  lastUpdated: Date;
}

@Component({
  selector: 'app-service-requests-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './service-requests-page.component.html',
  styleUrls: ['./service-requests-page.component.scss']
})
export class ServiceRequestsPageComponent implements OnInit {
  constructor(private router: Router, private http: HttpClient) {}
  
  isLoading = true;
  
  // Filters
  selectedStatus: 'All' | 'Pending' | 'In Progress' | 'Approved' | 'Rejected' = 'All';
  selectedCategory: string = 'All';
  startDate: string = '';
  endDate: string = '';
  
  // Pagination
  currentPage = 1;
  itemsPerPage = 5;

  // Data
  allRequests: ServiceRequest[] = [];
  filteredRequests: ServiceRequest[] = [];

  categories = ['All', 'Certificates', 'Permits', 'Complaints', 'Inquiries'];
  
  ngOnInit(): void {
    const nic = sessionStorage.getItem('userNic');
    const token = sessionStorage.getItem('token');
    
    if (nic && token) {
      const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
      this.http.get<ServiceRequest[]>(`http://localhost:5058/api/CitizenRequest/gn/${nic}`, { headers })
        .subscribe({
          next: (data) => {
            const statusMap: any = {
              'PENDING': 'Pending',
              'Pending': 'Pending',
              'SUBMITTED': 'Pending',
              'Submitted': 'Pending',
              'UNDER_REVIEW': 'Under Review',
              'Under Review': 'Under Review',
              'CHANGES_REQUESTED': 'Changes Requested',
              'Changes Requested': 'Changes Requested',
              'RESUBMITTED': 'In Progress',
              'Resubmitted': 'In Progress',
              'VERIFIED': 'Verified',
              'Verified': 'Verified',
              'APPROVED': 'Approved',
              'Approved': 'Approved',
              'ACCEPTED': 'Approved',
              'Accepted': 'Approved',
              'REJECTED': 'Rejected',
              'Rejected': 'Rejected',
              'REVOKED': 'Rejected',
              'Revoked': 'Rejected'
            };
            this.allRequests = data.map((r: any) => ({
              ...r,
              submittedDate: r.submittedDate ? new Date(r.submittedDate) : null,
              lastUpdated: r.lastUpdated ? new Date(r.lastUpdated) : null,
              status: statusMap[r.status] || r.status
            }));
            this.applyFilters();
            this.isLoading = false;
          },
          error: (err) => {
            console.error('Failed to fetch requests', err);
            this.isLoading = false;
          }
        });
    } else {
      this.isLoading = false;
    }
  }

  // Filtered requests specifically for calculating counts (ignoring the selectedStatus filter)
  get filteredRequestsForCounts(): ServiceRequest[] {
    return this.allRequests.filter(req => {
      const matchCategory = this.selectedCategory === 'All' || req.category === this.selectedCategory;
      
      let matchDate = true;
      if (this.startDate && this.endDate) {
        const start = new Date(this.startDate).getTime();
        const end = new Date(this.endDate).getTime();
        const endOfEndDay = end + 24 * 60 * 60 * 1000 - 1;
        const reqTime = new Date(req.submittedDate).getTime();
        matchDate = reqTime >= start && reqTime <= endOfEndDay;
      }

      return matchCategory && matchDate;
    });
  }

  // Summary counts
  get totalRequests() { return this.filteredRequestsForCounts.length; }
  get pendingRequests() { return this.filteredRequestsForCounts.filter(r => r.status === 'Pending').length; }
  get inProgressRequests() { return this.filteredRequestsForCounts.filter(r => r.status === 'In Progress' || r.status === 'Under Review' || r.status === 'Verified' || r.status === 'Changes Requested').length; }
  get approvedRequests() { return this.filteredRequestsForCounts.filter(r => r.status === 'Approved').length; }
  get rejectedRequests() { 
    return this.filteredRequestsForCounts.filter(r => 
      r.status === 'Rejected' || r.status === 'REJECTED' || r.status === 'REVOKED' || r.status === 'Revoked'
    ).length; 
  }

  // Filter actions
  setStatusFilter(status: 'All' | 'Pending' | 'In Progress' | 'Approved' | 'Rejected') {
    this.selectedStatus = status;
    this.currentPage = 1;
    this.applyFilters();
  }

  onFilterChange() {
    this.currentPage = 1;
    this.applyFilters();
  }

  applyFilters() {
    this.filteredRequests = this.allRequests.filter(req => {
      const matchStatus = this.selectedStatus === 'All' || 
                          req.status === this.selectedStatus ||
                          (this.selectedStatus === 'In Progress' && 
                           (req.status === 'Under Review' || req.status === 'Verified' || req.status === 'Changes Requested'));
      const matchCategory = this.selectedCategory === 'All' || req.category === this.selectedCategory;
      
      let matchDate = true;
      if (this.startDate && this.endDate) {
        const start = new Date(this.startDate).getTime();
        const end = new Date(this.endDate).getTime();
        // Set end time to end of day
        const endOfEndDay = end + 24 * 60 * 60 * 1000 - 1;
        const reqTime = new Date(req.submittedDate).getTime();
        matchDate = reqTime >= start && reqTime <= endOfEndDay;
      }

      return matchStatus && matchCategory && matchDate;
    });
    
    // Default sorting: newest first by submittedDate
    this.filteredRequests.sort((a, b) => new Date(b.submittedDate).getTime() - new Date(a.submittedDate).getTime());
  }

  // Pagination logic
  get paginatedRequests() {
    const startIndex = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredRequests.slice(startIndex, startIndex + this.itemsPerPage);
  }

  get totalPages() {
    return Math.ceil(this.filteredRequests.length / this.itemsPerPage) || 1;
  }

  getPagesArray(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  min(a: number, b: number): number {
    return Math.min(a, b);
  }

  nextPage() {
    if (this.currentPage < this.totalPages) this.currentPage++;
  }

  prevPage() {
    if (this.currentPage > 1) this.currentPage--;
  }

  goToPage(page: number) {
    this.currentPage = page;
  }

  // Action stub
  viewDetails(id: string) {
    console.log(`Navigating to details for ${id}`);
    this.router.navigate(['/gn-officer/requests', id]);
  }
}
