import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';

export interface ServiceRequest {
  id: string;
  serviceType: string;
  category: string;
  status: 'Pending' | 'In Progress' | 'Approved' | 'Rejected';
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
      this.http.get<ServiceRequest[]>(`http://localhost:5058/api/CitizenRequest/citizen/${nic}`, { headers })
        .subscribe({
          next: (data) => {
            this.allRequests = data;
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

  // Summary counts
  get totalRequests() { return this.allRequests.length; }
  get pendingRequests() { return this.allRequests.filter(r => r.status === 'Pending').length; }
  get inProgressRequests() { return this.allRequests.filter(r => r.status === 'In Progress').length; }
  get approvedRequests() { return this.allRequests.filter(r => r.status === 'Approved').length; }
  get rejectedRequests() { return this.allRequests.filter(r => r.status === 'Rejected').length; }

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
      const matchStatus = this.selectedStatus === 'All' || req.status === this.selectedStatus;
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
    this.router.navigate(['/citizen/service-requests', id]);
  }
}
