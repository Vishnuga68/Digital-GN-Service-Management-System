import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-residence-registry-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './residence-registry-page.component.html',
  styleUrls: ['./residence-registry-page.component.scss']
})
export class ResidenceRegistryPageComponent implements OnInit {
  isLoading = true;

  // GN Level Analytics
  analytics = {
    totalCitizens: 1245,
    families: 342,
    welfareEligible: 120,
    seniorCitizens: 185
  };

  // Search and Filter
  searchQuery = '';
  selectedFilter = 'All';

  citizens: any[] = [];
  filteredCitizens: any[] = [];

  // Selected Citizen for Details View
  selectedCitizen: any = null;

  // Add Citizen Modal
  showAddModal = false;
  isEgnSelected = false;
  isAccountCreated = false;
  generatedCredentials = {
    username: '',
    password: ''
  };

  newCitizen = {
    fullName: '',
    nic: '',
    gender: 'Male',
    dob: '',
    maritalStatus: 'Single',
    occupation: '',
    address: '',
    contact: '',
    welfareDetails: 'None',
    hasEgnAccount: false
  };

  ngOnInit(): void {
    setTimeout(() => {
      this.loadMockData();
      this.filterCitizens();
      this.isLoading = false;
    }, 1000);
  }

  loadMockData() {
    this.citizens = [
      {
         id: 'C001',
         fullName: 'Saman Kumara',
         nic: '198512345678',
         gender: 'Male',
         dob: '1985-05-10',
         maritalStatus: 'Married',
         religion: 'Buddhism',
         occupation: 'Teacher',
         address: 'No 45, Temple Road, Colombo.',
         gsDivision: 'GN-01',
         dsDivision: 'Colombo 03',
         district: 'Colombo',
         province: 'Western',
         contact: '0771234567',
         welfareDetails: 'Eligible for Samurdhi',
         hasEgnAccount: true,
         familyMembers: [
            { name: 'Nimali Perera', relation: 'Spouse', nic: '198712345678', occupation: 'Housewife' },
            { name: 'Kasun Kumara', relation: 'Child', nic: '201012345678', occupation: 'Student' }
         ]
      },
      {
         id: 'C002',
         fullName: 'Kamala Silva',
         nic: '196012345678',
         gender: 'Female',
         dob: '1960-08-22',
         maritalStatus: 'Widowed',
         religion: 'Christianity',
         occupation: 'Retired',
         address: 'No 12, Church Street, Colombo.',
         gsDivision: 'GN-01',
         dsDivision: 'Colombo 03',
         district: 'Colombo',
         province: 'Western',
         contact: '0712345678',
         welfareDetails: 'Elderly Allowance Recipient',
         hasEgnAccount: false,
         familyMembers: []
      },
      {
         id: 'C003',
         fullName: 'Ruwan Jayawardena',
         nic: '199512345678',
         gender: 'Male',
         dob: '1995-12-01',
         maritalStatus: 'Single',
         religion: 'Buddhism',
         occupation: 'Software Engineer',
         address: 'No 78, Galle Road, Colombo.',
         gsDivision: 'GN-01',
         dsDivision: 'Colombo 03',
         district: 'Colombo',
         province: 'Western',
         contact: '0751234567',
         welfareDetails: 'None',
         hasEgnAccount: true,
         familyMembers: [
            { name: 'Sunil Jayawardena', relation: 'Father', nic: '196512345678', occupation: 'Farmer' }
         ]
      }
    ];
  }

  filterCitizens() {
    let filtered = this.citizens;

    if (this.selectedFilter === 'Welfare') {
       filtered = filtered.filter(c => c.welfareDetails !== 'None');
    }

    if (this.searchQuery.trim()) {
       const q = this.searchQuery.toLowerCase();
       filtered = filtered.filter(c => 
          c.fullName.toLowerCase().includes(q) || 
          c.nic.includes(q) ||
          c.address.toLowerCase().includes(q)
       );
    }
    this.filteredCitizens = filtered;
  }

  viewDetails(citizen: any) {
    this.selectedCitizen = citizen;
  }

  backToList() {
    this.selectedCitizen = null;
  }

  getRelationshipClass(rel: string): string {
    if (!rel) return 'rel-tag default';
    const r = rel.toLowerCase();
    if (r.includes('spouse') || r.includes('wife') || r.includes('husband')) return 'rel-tag spouse';
    if (r.includes('child') || r.includes('son') || r.includes('daughter')) return 'rel-tag child';
    if (r.includes('parent') || r.includes('father') || r.includes('mother')) return 'rel-tag parent';
    return 'rel-tag default';
  }

  openAddModal() {
    this.showAddModal = true;
    this.resetForm();
  }

  closeAddModal() {
    this.showAddModal = false;
  }

  resetForm() {
    this.newCitizen = {
      fullName: '',
      nic: '',
      gender: 'Male',
      dob: '',
      maritalStatus: 'Single',
      occupation: '',
      address: '',
      contact: '',
      welfareDetails: 'None',
      hasEgnAccount: false
    };
    this.isEgnSelected = false;
    this.isAccountCreated = false;
    this.generatedCredentials = { username: '', password: '' };
  }

  generateCredentials() {
    if (!this.newCitizen.nic) return;
    
    // Generate username from NIC + random string
    const randomStr = Math.random().toString(36).substring(2, 5);
    this.generatedCredentials.username = `egn_${this.newCitizen.nic}_${randomStr}`;
    
    // Generate temporary password
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$';
    let password = '';
    for (let i = 0; i < 8; i++) {
      password += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    this.generatedCredentials.password = password;
    this.isAccountCreated = true;
    this.newCitizen.hasEgnAccount = true;
  }

  saveCitizen() {
    const id = 'C00' + (this.citizens.length + 1);
    const citizenToSave = { 
      ...this.newCitizen, 
      id,
      familyMembers: [] 
    };
    this.citizens.unshift(citizenToSave);
    this.filterCitizens();
    this.closeAddModal();
  }
}
