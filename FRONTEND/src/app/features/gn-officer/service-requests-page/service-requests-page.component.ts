import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';

export interface ServiceRequest {
  id: string;
  citizenName: string;
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
  constructor(private router: Router) {}
  
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
    // Simulate loading
    setTimeout(() => {
      this.allRequests = [
        { id: 'REQ-2023-001', citizenName: 'Saman Kumara', serviceType: 'Income Certificate', category: 'Certificates', status: 'Approved', submittedDate: new Date('2023-10-01T10:00:00'), lastUpdated: new Date('2023-10-05T14:30:00') },
        { id: 'REQ-2023-002', citizenName: 'Nimali Perera', serviceType: 'Residence Registration', category: 'Certificates', status: 'In Progress', submittedDate: new Date('2023-10-12T09:15:00'), lastUpdated: new Date('2023-10-14T11:20:00') },
        { id: 'REQ-2023-003', citizenName: 'Ruwan Silva', serviceType: 'Business Permit', category: 'Permits', status: 'Pending', submittedDate: new Date('2023-10-20T16:45:00'), lastUpdated: new Date('2023-10-20T16:45:00') },
        { id: 'REQ-2023-004', citizenName: 'Kamal Jayawardena', serviceType: 'Noise Complaint', category: 'Complaints', status: 'Rejected', submittedDate: new Date('2023-09-15T08:30:00'), lastUpdated: new Date('2023-09-18T10:00:00') },
        { id: 'REQ-2023-005', citizenName: 'Amara Weerasinghe', serviceType: 'Land Boundary Inquiry', category: 'Inquiries', status: 'Approved', submittedDate: new Date('2023-08-22T11:00:00'), lastUpdated: new Date('2023-08-28T09:45:00') },
        { id: 'REQ-2023-006', citizenName: 'Sunil Rathnayake', serviceType: 'Marriage Certificate Copy', category: 'Certificates', status: 'Pending', submittedDate: new Date('2023-10-24T14:10:00'), lastUpdated: new Date('2023-10-24T14:10:00') },
        { id: 'REQ-2023-007', citizenName: 'Nadeesha Madushani', serviceType: 'Building Permit', category: 'Permits', status: 'In Progress', submittedDate: new Date('2023-10-10T13:20:00'), lastUpdated: new Date('2023-10-15T15:30:00') }
      ];
      this.applyFilters();
      this.isLoading = false;
    }, 1500); // 1.5s loading simulation
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
        const reqTime = req.submittedDate.getTime();
        matchDate = reqTime >= start && reqTime <= endOfEndDay;
      }

      return matchStatus && matchCategory && matchDate;
    });
    
    // Default sorting: newest first by submittedDate
    this.filteredRequests.sort((a, b) => b.submittedDate.getTime() - a.submittedDate.getTime());
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
