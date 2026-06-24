import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';

@Component({
  selector: 'app-gn-registration-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gn-registration-page.component.html',
  styleUrls: ['./gn-registration-page.component.scss']
})
export class GnRegistrationPageComponent implements OnInit {
  private http = inject(HttpClient);

  newGN = {
    name: '',
    nic: '',
    division: '',
    email: '',
    phone: '',
    tempPassword: ''
  };

  gnDivisions: any[] = [];
  gnOfficers: any[] = [];
  activeOfficerForSelectedDivision: string | null = null;
  
  isLoading = false;
  successMessage = '';
  errorMessage = '';

  isNicValidating = false;
  isNameDisabled = false;
  isNicValid = false;
  registeredOfficer: any = null;
  private debounceTimeout: any = null;

  constructor() { }

  ngOnInit(): void {
    this.generateTempPassword();
    this.loadGnDivisions();
    this.loadGnOfficers();
  }

  generateTempPassword() {
    this.newGN.tempPassword = 'GN@' + Math.floor(1000 + Math.random() * 9000);
  }

  loadGnDivisions() {
    const token = sessionStorage.getItem('token');
    if (token) {
      const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
      this.http.get<any[]>('http://localhost:5058/api/GnDivision', { headers }).subscribe({
        next: (res) => {
          this.gnDivisions = Array.isArray(res) ? res : [];
        },
        error: (err) => {
          console.error('[GN Registration] Error loading divisions:', err);
        }
      });
    }
  }

  loadGnOfficers() {
    const token = sessionStorage.getItem('token');
    if (token) {
      const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
      this.http.get<any[]>('http://localhost:5058/api/GnOfficer', { headers }).subscribe({
        next: (res) => {
          this.gnOfficers = Array.isArray(res) ? res : [];
        },
        error: (err) => {
          console.error('[GN Registration] Error loading officers:', err);
        }
      });
    }
  }

  checkDivisionStatus() {
    this.activeOfficerForSelectedDivision = null;
    if (!this.newGN.division) return;
    
    // Find if an officer is currently assigned to this division
    const match = this.gnOfficers.find(o => o.gnDivisionId === this.newGN.division);
    if (match) {
      this.activeOfficerForSelectedDivision = match.fullName;
    }
  }

  onNicChange() {
    const nic = this.newGN.nic ? this.newGN.nic.trim() : '';
    
    // Clear any pending timeout
    if (this.debounceTimeout) {
      clearTimeout(this.debounceTimeout);
    }
    
    // Reset validation state
    this.isNicValid = false;
    this.newGN.name = '';
    this.isNameDisabled = false;
    this.errorMessage = '';

    if (!nic) {
      this.isNicValidating = false;
      return;
    }

    this.isNicValidating = true;

    // Debounce for 500ms
    this.debounceTimeout = setTimeout(() => {
      this.validateCitizenNic(nic);
    }, 500);
  }

  validateCitizenNic(nic: string) {
    const token = sessionStorage.getItem('token');
    const headers = token ? new HttpHeaders().set('Authorization', `Bearer ${token}`) : new HttpHeaders();

    this.http.get<any>(`http://localhost:5058/api/CitizenMaster/${nic}`, { headers }).subscribe({
      next: (res) => {
        this.isNicValidating = false;
        if (res && res.fullName) {
          this.newGN.name = res.fullName;
          this.isNameDisabled = true;
          this.isNicValid = true;
        } else {
          this.handleInvalidNic();
        }
      },
      error: (err) => {
        this.isNicValidating = false;
        console.error('[GN Registration] NIC validation error:', err);
        this.handleInvalidNic();
      }
    });
  }

  handleInvalidNic() {
    this.isNicValid = false;
    this.newGN.name = '';
    this.isNameDisabled = false;
    // Show validation modal: "Citizen not found in Citizen Master."
    this.showModal(
      'Citizen Validation Failed',
      'Citizen not found in Citizen Master.',
      'error',
      'ok'
    );
  }

  // Modal Alert States
  isModalOpen = false;
  modalTitle = '';
  modalMessage = '';
  modalType: 'success' | 'warning' | 'error' | 'info' = 'info';
  modalActions: 'ok' | 'confirm' = 'ok';

  registerGN(forceReassign = false) {
    this.isLoading = true;
    this.successMessage = '';
    this.errorMessage = '';

    const token = sessionStorage.getItem('token');
    if (!token) {
      this.showModal('Registration Failed', 'Authorization token is missing. Please log in again.', 'error', 'ok');
      this.isLoading = false;
      return;
    }

    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    
    const payload = {
      nic: this.newGN.nic,
      fullName: this.newGN.name,
      gnDivisionId: this.newGN.division || null,
      email: this.newGN.email,
      phone: this.newGN.phone,
      password: this.newGN.tempPassword,
      forceReassign: forceReassign
    };

    this.http.post<any>('http://localhost:5058/api/Account/gn-officer', payload, { headers }).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          const divisionObj = this.gnDivisions.find(d => d.id === this.newGN.division);
          const divisionName = divisionObj ? `${divisionObj.id} - ${divisionObj.name}` : this.newGN.division;

          if (res.message === 'existing_account_detected') {
            this.registeredOfficer = {
              name: this.newGN.name,
              nic: this.newGN.nic,
              username: this.newGN.nic,
              tempPassword: '',
              divisionName: divisionName
            };
            this.showModal('Existing Account Detected', 'User already has an account. Added new role successfully.', 'warning', 'ok');
          } else {
            this.registeredOfficer = {
              name: this.newGN.name,
              nic: this.newGN.nic,
              username: this.newGN.nic,
              tempPassword: this.newGN.tempPassword,
              divisionName: divisionName
            };
            this.showModal('Registration Successful', 'GN Officer registered successfully.', 'success', 'ok');
          }
          
          // Reset form and generate a new temporary password
          this.newGN = { name: '', nic: '', division: '', email: '', phone: '', tempPassword: '' };
          this.generateTempPassword();
          this.activeOfficerForSelectedDivision = null;
          this.isNicValid = false;
          this.isNameDisabled = false;
          // Reload officer list to keep states completely in-sync
          this.loadGnOfficers();
        } else {
          // Handle backend validation failures dynamically
          if (res.message === 'citizen_not_found') {
            // Case 1
            this.showModal('Citizen Not Found', 'Citizen record not found in Citizen Master. GN Officer can only be created for existing citizens.', 'error', 'ok');
          } else if (res.message === 'duplicate_account') {
            // Case 4
            this.showModal('Duplicate Account', 'This user already has an active system account.', 'error', 'ok');
          } else if (res.message === 'active_assignment_exists') {
            // Case 2 Warning
            this.showModal(
              'Active Officer Assignment Alert',
              'This GN Division already has an active GN Officer. Continuing will deactivate the current assignment.',
              'warning',
              'confirm'
            );
          } else {
            // Case 5 fallback
            this.showModal('Registration Failed', 'Registration failed. Please try again.', 'error', 'ok');
          }
        }
      },
      error: (err) => {
        this.isLoading = false;
        console.error('[GN Registration Error]', err);
        // Case 5 fallback
        this.showModal('Registration Failed', 'Registration failed. Please try again.', 'error', 'ok');
      }
    });
  }

  showModal(title: string, message: string, type: 'success' | 'warning' | 'error' | 'info', actions: 'ok' | 'confirm' = 'ok') {
    this.modalTitle = title;
    this.modalMessage = message;
    this.modalType = type;
    this.modalActions = actions;
    this.isModalOpen = true;
  }

  closeModal() {
    this.isModalOpen = false;
  }

  confirmForceReassign() {
    this.isModalOpen = false;
    this.registerGN(true);
  }
}
