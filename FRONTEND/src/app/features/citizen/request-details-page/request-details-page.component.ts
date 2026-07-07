import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-request-details-page',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './request-details-page.component.html',
  styleUrls: ['./request-details-page.component.scss']
})
export class RequestDetailsPageComponent implements OnInit {
  isLoading = true;
  requestId: string | null = null;
  requestDetails: any = null;

  constructor(private route: ActivatedRoute, private router: Router, private http: HttpClient) {}

  ngOnInit(): void {
    this.requestId = this.route.snapshot.paramMap.get('id');
    
    if (this.requestId) {
      this.fetchRequestDetails(this.requestId);
    } else {
      this.isLoading = false;
    }
  }

  fetchRequestDetails(id: string) {
    this.http.get(`http://localhost:5058/api/CitizenRequest/details/${id}`).subscribe({
      next: (data: any) => {
        this.requestDetails = data;
        this.requestDetails.id = data.requestId;
        
        // Ensure currentStep is set correctly for UI based on status
        if (this.requestDetails.currentStatus === 'Pending') this.requestDetails.currentStep = 1;
        else if (this.requestDetails.currentStatus === 'Verified') this.requestDetails.currentStep = 3;
        else if (this.requestDetails.currentStatus === 'Approved' || this.requestDetails.currentStatus === 'Rejected' || this.requestDetails.currentStatus === 'Accepted') this.requestDetails.currentStep = 4;
        else this.requestDetails.currentStep = 2; // Default In Progress

        // Ensure status property exists for UI binding
        this.requestDetails.status = this.requestDetails.currentStatus;
        
        // Ensure documents mapping
        this.requestDetails.documents = data.attachments?.map((a: any) => ({
          name: a.fileName,
          type: a.fileType?.includes('pdf') ? 'pdf' : 'image',
          date: a.uploadedDate,
          size: a.fileSize || 'Unknown',
          url: a.fileUrl?.startsWith('/') ? `http://localhost:5058${a.fileUrl}` : a.fileUrl
        })) || [];

        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error fetching request details', err);
        this.isLoading = false;
      }
    });
  }

  goBack() {
    this.router.navigate(['/citizen/service-requests']);
  }
}
