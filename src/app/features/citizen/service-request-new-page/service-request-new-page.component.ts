import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';

interface Service {
  id: string;
  title: string;
  description: string;
  icon?: string;
}

interface Category {
  name: string;
  icon: string;
  services: Service[];
}

@Component({
  selector: 'app-service-request-new-page',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule],
  templateUrl: './service-request-new-page.component.html',
  styleUrls: ['./service-request-new-page.component.scss']
})
export class ServiceRequestNewPageComponent {
  requestForm: FormGroup;
  selectedService: Service | null = null;
  applicantType: string = 'myself';
  isUploading: boolean = false;
  uploadedFiles: string[] = [];

  serviceCategories: Category[] = [
    {
      name: 'Registration',
      icon: '/assets/icons/registry.png',
      services: [
        { id: 'birth', title: 'Birth Registration', description: 'Register a new birth within the division to obtain official records and certificates.', icon: '/assets/icons/birth.png' },
        { id: 'marriage', title: 'Marriage Registration', description: 'Formalize and register a marriage for legal certification and official documentation.', icon: '/assets/icons/marriage.png' },
        { id: 'death', title: 'Death Registration', description: 'Official record-keeping of death for legal certification and processing of final affairs.', icon: '/assets/icons/death.png' },
        { id: 'voter', title: 'Voter Registration (18+)', description: 'Register as a voter after reaching 18 years of age and getting eligibility for elections.', icon: '/assets/icons/vote.png' },
        { id: 'gn-register', title: 'Register to New GN', description: 'Update your official residency information when relocating to a different GN division.', icon: '/assets/icons/gn-transfer.png' }
      ]
    },
    {
      name: 'Document Services',
      icon: '/assets/icons/request.png',
      services: [
        { id: 'doc-request', title: 'Document Request', description: 'Request certified copies of your official documents, certificates, or letters of residency.', icon: '/assets/icons/document-request.png' },
        { id: 'doc-verification', title: 'Document Verification', description: 'Get your official documents verified and stamped for legal or third-party requirements.', icon: '/assets/icons/document-verify.png' }
      ]
    },
    {
      name: 'Complaints',
      icon: '/assets/icons/complaints.png',
      services: [
        { id: 'complaint', title: 'Submit Complaint', description: 'Lodge a formal grievance regarding local services, administrative issues, or community matters.', icon: '/assets/icons/complaint-new.png' }
      ]
    },
    {
      name: 'Other',
      icon: '/assets/icons/other.png',
      services: [
        { id: 'other', title: 'Other Request', description: 'Submit any specialized requests or general inquiries that do not fall into standard categories.', icon: '/assets/icons/other-new.png' }
      ]
    }
  ];

  constructor(private fb: FormBuilder, private router: Router) {
    this.requestForm = this.fb.group({
      fullName: ['', Validators.required],
      nic: ['', Validators.required],
      phone: ['', Validators.required],
      email: ['', [Validators.email]],
      childName: [''],
      childAge: [''],
      elderlyName: [''],
      elderlyNic: [''],
      relationship: [''],
      birthRelationship: [''],
      deathRelationship: [''],
      applyingAs: [''],
      spouseFullName: ['', Validators.required],
      spouseNic: [''],
      spouseDob: [''],
      complaintCategory: ['', Validators.required],
      priorityLevel: ['low'],
      incidentDate: [''],
      incidentLocation: ['', Validators.required],
      documentType: [''],
      subject: ['', Validators.required],
      description: ['', Validators.required],
    });
  }

  selectService(service: Service) {
    this.selectedService = service;
    if (!this.showApplicantType) {
      this.applicantType = 'myself';
    }
  }

  get showApplicantType(): boolean {
    const servicesWithType = ['doc-request', 'doc-verification', 'other'];
    return this.selectedService ? servicesWithType.includes(this.selectedService.id) : false;
  }

  setApplicantType(type: string) {
    this.applicantType = type;
  }

  onFileChange(event: any) {
    const files = event.target.files;
    if (files.length > 0) {
      this.isUploading = true;
      setTimeout(() => {
        for (let i = 0; i < files.length; i++) {
          this.uploadedFiles.push(files[i].name);
        }
        this.isUploading = false;
      }, 1000);
    }
  }

  removeFile(index: number) {
    this.uploadedFiles.splice(index, 1);
  }

  submitRequest() {
    if (this.requestForm.valid && this.selectedService) {
      console.log('Submitting...', {
        service: this.selectedService.id,
        applicantType: this.applicantType,
        data: this.requestForm.value,
        files: this.uploadedFiles
      });
      this.router.navigate(['/citizen/service-requests']);
    } else {
      Object.values(this.requestForm.controls).forEach(control => control.markAsTouched());
    }
  }

  goBack() {
    if (this.selectedService) {
      this.deselectService();
    } else {
      this.router.navigate(['/citizen/service-requests']);
    }
  }

  deselectService() {
    this.selectedService = null;
  }
}
