import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { HttpClient, HttpHeaders } from '@angular/common/http';

@Component({
  selector: 'app-my-profile-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './my-profile-page.component.html',
  styleUrls: ['./my-profile-page.component.scss']
})
export class MyProfilePageComponent implements OnInit {
  private authService = inject(AuthService);
  private http = inject(HttpClient);

  isChangeRubberStampModalOpen = false;
  isChangeSignatureModalOpen = false;
  
  // Digital Details
  rubberStampPath: string | null = null;
  signaturePath: string | null = null;
  rubberStampFile: File | null = null;
  signatureFile: File | null = null;
  rubberStampPreview: string | null = null;
  signaturePreview: string | null = null;
  isUploadingDigitalDetail = false;
  uploadError = '';

  officerDetails = {
    fullName: '-',
    officerID: '-',
    nic: '-',
    division: '-',
    divisionName: '-',
    appointmentDate: null as Date | null,
    address: '-',
    email: '-',
    phone: '-',
    status: '-',
    yearsInService: 0,
    tasksCompleted: 0
  };

  ngOnInit(): void {
    const profileStr = sessionStorage.getItem('userProfile');
    if (profileStr) {
      this.updateProfile(JSON.parse(profileStr));
    }

    this.authService.profile$.subscribe((profile: any) => {
      if (profile) {
        this.updateProfile(profile);
      }
    });
  }

  // --- Digital Details Methods ---

  fetchDigitalDetails(nic: string) {
    const token = sessionStorage.getItem('token');
    if (!token) return;
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    
    this.http.get<any>(`http://localhost:5058/api/gn-officer/digital-details/${nic}`, { headers }).subscribe({
      next: (res) => {
        if (res.rubberStampPath) {
          this.rubberStampPath = res.rubberStampPath.startsWith('data:') ? res.rubberStampPath : `http://localhost:5058${res.rubberStampPath}`;
        }
        if (res.signaturePath) {
          this.signaturePath = res.signaturePath.startsWith('data:') ? res.signaturePath : `http://localhost:5058${res.signaturePath}`;
        }
      },
      error: (err) => console.error('[Digital Details Error]', err)
    });
  }

  toggleChangeRubberStampModal() {
    this.isChangeRubberStampModalOpen = !this.isChangeRubberStampModalOpen;
    if (this.isChangeRubberStampModalOpen) {
      this.rubberStampFile = null;
      this.rubberStampPreview = null;
      this.uploadError = '';
    }
  }

  toggleChangeSignatureModal() {
    this.isChangeSignatureModalOpen = !this.isChangeSignatureModalOpen;
    if (this.isChangeSignatureModalOpen) {
      this.signatureFile = null;
      this.signaturePreview = null;
      this.uploadError = '';
    }
  }

  onRubberStampSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.uploadError = '';
      if (file.size > 500 * 1024) {
        this.uploadError = 'Maximum file size is 500KB.';
        return;
      }
      this.rubberStampFile = file;
      const reader = new FileReader();
      reader.onload = (e: any) => this.rubberStampPreview = e.target.result;
      reader.readAsDataURL(file);
    }
  }

  onSignatureSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.uploadError = '';
      if (file.size > 500 * 1024) {
        this.uploadError = 'Maximum file size is 500KB.';
        return;
      }
      this.signatureFile = file;
      const reader = new FileReader();
      reader.onload = (e: any) => this.signaturePreview = e.target.result;
      reader.readAsDataURL(file);
    }
  }

  uploadRubberStamp() {
    if (!this.rubberStampFile) return;
    this.isUploadingDigitalDetail = true;
    
    const formData = new FormData();
    formData.append('GNOfficerNIC', this.officerDetails.nic);
    formData.append('RubberStampImage', this.rubberStampFile);

    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    this.http.put<any>('http://localhost:5058/api/gn-officer/update-rubber-stamp', formData, { headers }).subscribe({
      next: (res) => {
        this.rubberStampPath = res.path && res.path.startsWith('data:') ? res.path : `http://localhost:5058${res.path}`;
        this.isUploadingDigitalDetail = false;
        this.toggleChangeRubberStampModal();
      },
      error: (err) => {
        this.isUploadingDigitalDetail = false;
        this.uploadError = err.error || 'Failed to upload rubber stamp.';
        console.error(err);
      }
    });
  }

  uploadSignature() {
    if (!this.signatureFile) return;
    this.isUploadingDigitalDetail = true;
    
    const formData = new FormData();
    formData.append('GNOfficerNIC', this.officerDetails.nic);
    formData.append('SignatureImage', this.signatureFile);

    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    this.http.put<any>('http://localhost:5058/api/gn-officer/update-signature', formData, { headers }).subscribe({
      next: (res) => {
        this.signaturePath = res.path && res.path.startsWith('data:') ? res.path : `http://localhost:5058${res.path}`;
        this.isUploadingDigitalDetail = false;
        this.toggleChangeSignatureModal();
      },
      error: (err) => {
        this.isUploadingDigitalDetail = false;
        this.uploadError = err.error || 'Failed to upload signature.';
        console.error(err);
      }
    });
  }

  updateProfile(profile: any) {
    this.officerDetails = {
      fullName: profile.FullName || profile.fullName || '-',
      officerID: profile.Nic || profile.nic || '-',
      nic: profile.Nic || profile.nic || '-',
      division: profile.GnDivisionId || profile.gnDivisionId || '-',
      divisionName: profile.GnDivisionName || profile.gnDivisionName || '-',
      appointmentDate: profile.AppointmentDate ? new Date(profile.AppointmentDate) : null,
      address: profile.OfficeAddress || '-',
      email: profile.Email || profile.email || '-',
      phone: profile.Phone || profile.phone || '-',
      status: profile.Status || profile.status || '-',
      yearsInService: profile.YearsInService || 0,
      tasksCompleted: profile.TasksCompleted || 0
    };

    if (this.officerDetails.nic && this.officerDetails.nic !== '-') {
      this.fetchDigitalDetails(this.officerDetails.nic);
    }
  }
}
