import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { catchError, map } from 'rxjs/operators';
import { of, forkJoin } from 'rxjs';

@Component({
  selector: 'app-residence-registry-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './residence-registry-page.component.html',
  styleUrls: ['./residence-registry-page.component.scss']
})
export class ResidenceRegistryPageComponent implements OnInit {
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);

  isLoading = true;
  gnDivisionId = '';
  gnDivisionName = '';
  dsDivisionName = '';

  // GN Level Analytics
  analytics = {
    totalCitizens: 0,
    families: 0
  };

  // Search and Filter
  searchQuery = '';
  selectedFilter = 'All';

  citizens: any[] = [];
  filteredCitizens: any[] = [];
  voters: any[] = [];
  filteredVoters: any[] = [];

  // Selected Citizen for Details View
  selectedCitizen: any = null;
  selectedFamilyData: any = {
    head: '',
    cardNumber: '',
    status: '',
    gn: '',
    members: [],
    historyMembers: [],
    showHistory: false
  };

  // Add Citizen Modal
  showAddModal = false;
  isNicValidating = false;
  isNicValid = false;
  isSubmitting = false;

  newCitizen = {
    nic: '',
    fullName: '',
    address: '',
    email: '',
    contact: ''
  };

  // Premium Alert Modal States
  isAlertModalOpen = false;
  alertModalTitle = '';
  alertModalMessage = '';
  alertModalType: 'success' | 'warning' | 'error' = 'success';
  registeredCitizen: any = null;
  private debounceTimeout: any = null;

  ngOnInit(): void {
    this.loadOfficerProfile();
  }

  loadOfficerProfile() {
    const token = sessionStorage.getItem('token');
    const nic = sessionStorage.getItem('userNic') || '';
    
    if (token && nic) {
      const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
      this.http.get<any>(`http://localhost:5058/api/Dashboard/gn/${nic}`, { headers }).subscribe({
        next: (res) => {
          if (res.success && res.data) {
            const profile = res.data.profile;
            this.gnDivisionId = profile.GnDivisionId || profile.gnDivisionId || '';
            this.loadDivisionData();
          } else {
            this.isLoading = false;
          }
        },
        error: (err) => {
          console.error('[ResidenceRegistry] Error fetching officer profile:', err);
          this.isLoading = false;
        }
      });
    } else {
      this.isLoading = false;
    }
  }

  loadDivisionData() {
    if (!this.gnDivisionId) {
      this.isLoading = false;
      return;
    }

    const token = sessionStorage.getItem('token');
    if (!token) return;

    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    this.isLoading = true;

    // Fetch Stats in real-time
    this.http.get<any>(`http://localhost:5058/api/CitizenMaster/division/stats?divisionId=${encodeURIComponent(this.gnDivisionId)}`, { headers }).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.analytics.totalCitizens = res.data.totalCitizens;
          this.analytics.families = res.data.totalFamilies;
        }
      },
      error: (err) => console.error('[ResidenceRegistry] Error loading stats:', err)
    });

    // Fetch Citizens directly from CitizenMaster table
    this.http.get<any>(`http://localhost:5058/api/CitizenMaster/division?divisionId=${encodeURIComponent(this.gnDivisionId)}`, { headers }).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success && res.data) {
          this.citizens = res.data.map((c: any) => ({
            ...c,
            dob: this.parseDate(c.dob)
          }));
          this.filterCitizens();
          this.loadVoters();

          // Auto-open citizen details if NIC is passed as query param
          const targetNic = this.route.snapshot.queryParams['nic'];
          if (targetNic) {
            this.selectCitizenByNic(targetNic);
          }
        }
      },
      error: (err) => {
        this.isLoading = false;
        console.error('[ResidenceRegistry] Error loading citizens:', err);
      }
    });

    // Fetch GN Division and DS Division details
    this.http.get<any>(`http://localhost:5058/api/GnDivision/${encodeURIComponent(this.gnDivisionId)}`, { headers }).subscribe({
      next: (gnRes) => {
        if (gnRes && gnRes.name) {
          this.gnDivisionName = gnRes.name;
          if (gnRes.dsDivisionId) {
            this.http.get<any>(`http://localhost:5058/api/DsDivision/${encodeURIComponent(gnRes.dsDivisionId)}`, { headers }).subscribe({
              next: (dsRes) => {
                if (dsRes && dsRes.name) {
                  this.dsDivisionName = dsRes.name;
                }
              }
            });
          }
        }
      }
    });
  }

  filterCitizens() {
    if (this.selectedFilter === 'Voters') {
       if (!this.voters || this.voters.length === 0) {
         this.loadVoters();
       } else {
         this.filterVoters();
       }
       return;
    }

    let filtered = this.citizens;

    if (this.selectedFilter === 'Welfare') {
       filtered = filtered.filter(c => c.hasEgnAccount === true);
    }

    if (this.searchQuery.trim()) {
       const q = this.searchQuery.toLowerCase();
       filtered = filtered.filter(c => 
          c.fullName.toLowerCase().includes(q) || 
          c.nic.includes(q) ||
          (c.address && c.address.toLowerCase().includes(q))
       );
    }
    this.filteredCitizens = filtered;
  }

  selectCitizenByNic(nic: string) {
    const found = this.citizens.find(c => c.nic === nic);
    if (found) {
      this.viewDetails(found);
    } else {
      const token = sessionStorage.getItem('token');
      if (!token) return;
      const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
      this.http.get<any>(`http://localhost:5058/api/CitizenMaster/${nic}`, { headers }).subscribe({
        next: (res) => {
          if (res && res.fullName) {
            const citizenDivision = res.gnDivisionId || res.GnDivisionId;
            if (citizenDivision === this.gnDivisionId) {
              res.dob = this.parseDate(res.dob);
              this.viewDetails(res);
            }
          }
        },
        error: (err) => console.error('Error fetching citizen by NIC:', err)
      });
    }
  }

  viewDetails(citizen: any) {
    this.isLoading = true;

    // Parse dob just in case
    const parsedDob = this.parseDate(citizen.dob);

    const tempCitizen = {
      ...citizen,
      dob: parsedDob,
      contact: {
        email: citizen.email || 'Not Provided',
        phone: citizen.phone || 'Not Provided'
      },
      province: 'Northern',
      district: 'Jaffna',
      dsDivision: this.dsDivisionName || 'N/A',
      gnDivision: this.gnDivisionName || this.gnDivisionId,
      familyCard: citizen.familyCardNumber || 'N/A',
      pastFamilyCard: citizen.pastFamilyCardNumber || '',
      familyCardStatus: citizen.familyCardStatus || 'Active',
      specialStatus: {
        lifeStatus: citizen.lifeStatus || 'Alive',
        isNalanpuri: false,
        hasKidneyDisease: false,
        hasCancer: false,
        hasElderlyEligible: false,
        hasVoterId: false
      },
      suggestions: [] as any[],
      confirmedWelfare: [] as any[]
    };

    const tempFamilyData = {
      head: 'Unknown',
      cardNumber: citizen.familyCardNumber || 'N/A',
      status: citizen.familyCardStatus || 'Active',
      gn: this.gnDivisionId,
      members: [] as any[],
      historyMembers: [] as any[],
      showHistory: false
    };
    
    const token = sessionStorage.getItem('token');
    if (!token) {
      this.isLoading = false;
      return;
    }
    
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    const requests: any = {};

    if (citizen.familyCardNumber && citizen.familyCardNumber !== 'N/A') {
      requests.family = this.http.get<any>(`http://localhost:5058/api/FamilyCard/${citizen.familyCardNumber}/members`, { headers }).pipe(
        catchError(() => of(null))
      );
    } else {
      requests.family = of(null);
    }

    requests.specialStatus = this.http.get<any>(`http://localhost:5058/api/CitizenSpecialStatus/${citizen.nic}`, { headers }).pipe(
      catchError(() => of(null))
    );

    forkJoin(requests).subscribe((results: any) => {
      // Process Family Members
      if (results.family && results.family.success) {
        const members = results.family.data;
        const headMember = members.find((m: any) => m.relationshipToHead === 'Head' || m.relationshipToHead === 'Self');
        tempFamilyData.head = headMember ? headMember.fullName : 'Unknown';
        
        tempFamilyData.members = members.map((m: any) => ({
          nic: m.nic,
          name: m.fullName,
          relationship: m.relationshipToHead,
          dob: this.parseDate(m.dob),
          status: m.lifeStatus
        }));
      }

      // Process Special Status / Suggestions & Confirmed Welfare
      if (results.specialStatus) {
        const statusData = results.specialStatus.data || results.specialStatus;
        if (statusData && statusData.success !== false) {
          const innerData = statusData.data || statusData;
          tempCitizen.suggestions = innerData.suggestions || [];
          tempCitizen.confirmedWelfare = innerData.confirmedWelfare || [];
        }
      }

      this.selectedCitizen = tempCitizen;
      this.selectedFamilyData = tempFamilyData;
      this.isLoading = false;
      setTimeout(() => window.scrollTo({ top: 0, behavior: 'smooth' }), 10);
    });
  }

  parseDate(dateString: any): Date | string | null {
    if (!dateString) return null;
    if (dateString instanceof Date) return dateString;
    const ds = String(dateString);
    const parts = ds.split(' ');
    if (parts.length > 0 && parts[0].includes('/')) {
      const dateParts = parts[0].split('/');
      if (dateParts.length === 3) {
        return new Date(+dateParts[2], +dateParts[1] - 1, +dateParts[0]);
      }
    }
    return ds;
  }

  calculateAge(dob: any): number {
    if (!dob) return 0;
    const parsed = this.parseDate(dob);
    const birthDate = parsed instanceof Date ? parsed : new Date(parsed as string);
    if (isNaN(birthDate.getTime())) return 0;
    
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const m = today.getMonth() - birthDate.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }
    return age;
  }

  isVoterEligible(dob: any, lifeStatus?: string): boolean {
    if (lifeStatus && lifeStatus !== 'Alive' && lifeStatus !== 'Active') return false;
    if (!dob) return false;
    const parsed = this.parseDate(dob);
    const birthDate = parsed instanceof Date ? parsed : new Date(parsed as string);
    if (isNaN(birthDate.getTime())) return false;
    return (new Date().getFullYear() - birthDate.getFullYear()) >= 18;
  }

  isRegisteredVoter(nic: string): boolean {
    if (!nic) return false;
    return this.voters.some(v => v.citizenNic === nic);
  }

  viewFamilyMemberDetails(member: any) {
    if (!member || !member.nic) return;
    
    // Clear currently selected to show loading skeleton
    this.selectedCitizen = null;
    this.isLoading = true;

    let fullCitizen = this.citizens.find(c => c.nic === member.nic);
    
    if (fullCitizen) {
      this.viewDetails(fullCitizen);
    } else {
      const token = sessionStorage.getItem('token');
      if (!token) return;
      const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
      
      this.http.get<any>(`http://localhost:5058/api/CitizenMaster/${member.nic}`, { headers }).subscribe({
        next: (res) => {
          if (res.success && res.data) {
             const data = res.data;
             data.dob = this.parseDate(data.dob);
             this.viewDetails(data);
          } else {
             this.isLoading = false;
          }
        },
        error: (err) => {
          console.error('Error fetching member details', err);
          this.isLoading = false;
        }
      });
    }
  }

  isElderly(dob: string, lifeStatus?: string): boolean {
    if (lifeStatus && lifeStatus !== 'Alive') return false;
    return this.calculateAge(dob) >= 60;
  }

  viewPastFamilyHistory() {
    this.selectedFamilyData.showHistory = true;
    
    if (!this.selectedCitizen?.pastFamilyCard) {
      this.selectedFamilyData.historyMembers = [];
      this.selectedFamilyData.pastHistoryNotFound = true;
      return;
    }
    
    const token = sessionStorage.getItem('token');
    if (!token) return;
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    
    let url = `http://localhost:5058/api/FamilyCard/history/members?pastCard=${this.selectedCitizen.pastFamilyCard}`;

    this.http.get<any>(url, { headers }).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          if (res.data.length === 0) {
            this.selectedFamilyData.historyMembers = [];
            this.selectedFamilyData.pastHistoryNotFound = true;
            return;
          }
          this.selectedFamilyData.pastHistoryNotFound = false;
          this.selectedFamilyData.historyMembers = res.data.map((m: any) => ({
            nic: m.nic,
            name: m.fullName,
            relationship: m.relationshipToHead,
            dob: this.parseDate(m.dob),
            status: m.lifeStatus,
            currentCard: m.familyCardNumber,
            pastCard: m.pastFamilyCardNumber
          }));
          this.selectedFamilyData.showHistory = true;
        }
      },
      error: (err) => {
        console.error('Error fetching family history:', err);
        alert('Failed to load family card history.');
      }
    });
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
      nic: '',
      fullName: '',
      address: '',
      email: '',
      contact: ''
    };
    this.isNicValidating = false;
    this.isNicValid = false;
    this.isSubmitting = false;
    if (this.debounceTimeout) {
      clearTimeout(this.debounceTimeout);
    }
  }

  onNicChange() {
    const nic = this.newCitizen.nic ? this.newCitizen.nic.trim() : '';
    
    // Clear timeout
    if (this.debounceTimeout) {
      clearTimeout(this.debounceTimeout);
    }
    
    // Reset
    this.isNicValid = false;
    this.newCitizen.fullName = '';
    this.newCitizen.address = '';

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
          const citizenDivision = res.gnDivisionId || res.GnDivisionId;
          if (citizenDivision && this.gnDivisionId && citizenDivision !== this.gnDivisionId) {
            this.isNicValid = false;
            this.showAlertModal(
              'Division Mismatch',
              'Citizen does not belong to your GN Division. You can only register citizens from your assigned division.',
              'error'
            );
          } else {
            this.newCitizen.fullName = res.fullName;
            this.newCitizen.address = res.address || 'Address registered in Citizen Master';
            this.isNicValid = true;
          }
        } else {
          this.handleInvalidNic();
        }
      },
      error: (err) => {
        this.isNicValidating = false;
        console.error('[ResidenceRegistry] NIC validation error:', err);
        this.handleInvalidNic();
      }
    });
  }

  handleInvalidNic() {
    this.isNicValid = false;
    this.newCitizen.fullName = '';
    this.newCitizen.address = '';
    this.showAlertModal(
      'Citizen Validation Failed',
      'Citizen not found in Citizen Master.',
      'error'
    );
  }

  submitEgnCitizen() {
    this.isSubmitting = true;
    
    const token = sessionStorage.getItem('token');
    if (!token) {
      this.showAlertModal('Registration Failed', 'Authorization token is missing. Please log in again.', 'error');
      this.isSubmitting = false;
      return;
    }

    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    // Generate credentials
    const tempPassword = 'eGN@' + Math.floor(1000 + Math.random() * 9000);
    const username = this.newCitizen.nic;

    const payload = {
      nic: this.newCitizen.nic,
      fullName: this.newCitizen.fullName,
      username: username,
      email: this.newCitizen.email,
      phone: this.newCitizen.contact,
      password: tempPassword
    };

    this.http.post<any>('http://localhost:5058/api/Account/citizen', payload, { headers }).subscribe({
      next: (res) => {
        this.isSubmitting = false;
        if (res.success) {
          if (res.message === 'existing_account_detected') {
            this.registeredCitizen = {
              name: this.newCitizen.fullName,
              nic: res.data.nic,
              usernameOrEmail: res.data.usernameOrEmail,
              existingRoles: res.data.existingRoles,
              action: res.data.action,
              isExisting: true,
              tempPassword: '',
              existingAccountMessage: `The NIC already has a ${res.data.existingRoles.join(' / ')} account. Use existing login credential.`
            };
            this.showAlertModal(
              'Existing Account Detected',
              `The NIC already has a ${res.data.existingRoles.join(' / ')} account. Use existing login credential.`,
              'warning'
            );
          } else {
            this.registeredCitizen = {
              name: this.newCitizen.fullName,
              nic: this.newCitizen.nic,
              username: this.newCitizen.nic,
              tempPassword: tempPassword,
              isExisting: false
            };
            this.showAlertModal(
              'Successful Registration',
              'Citizen successfully added to eGN services.',
              'success'
            );
          }

          this.closeAddModal();
          this.loadDivisionData();
        } else {
          if (res.message === 'citizen_not_found') {
            this.showAlertModal('Citizen Not Found', 'Citizen not found in Citizen Master.', 'error');
          } else if (res.message === 'duplicate_citizen_role') {
            this.showAlertModal('Citizen already registered', 'Citizen already registered.', 'error');
          } else {
            this.showAlertModal('Registration Failed', res.message || 'Registration failed. Please try again.', 'error');
          }
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        console.error('[ResidenceRegistry Submit Error]', err);
        this.showAlertModal('Registration Failed', 'Registration failed. Please try again.', 'error');
      }
    });
  }

  showAlertModal(title: string, message: string, type: 'success' | 'warning' | 'error') {
    this.alertModalTitle = title;
    this.alertModalMessage = message;
    this.alertModalType = type;
    this.isAlertModalOpen = true;
  }

  closeAlertModal() {
    this.isAlertModalOpen = false;
  }

  getWelfarePrograms(): any[] {
    if (!this.selectedCitizen) return [];
    const confirmedWelfare = this.selectedCitizen.confirmedWelfare || [];
    const suggestions = this.selectedCitizen.suggestions || [];
    
    const findConfirmed = (nameSub: string) => confirmedWelfare.some((w: any) => w.programName?.toLowerCase().includes(nameSub.toLowerCase()));
    const findSuggestion = (typeKey: string) => suggestions.some((s: any) => s.type === typeKey);
    const isVoter = this.isVoterEligible(this.selectedCitizen.dob || this.selectedCitizen.Dob);

    const programs = [
      {
        name: 'Aswesuma Welfare Benefit',
        icon: '/assets/icons/family.png',
        status: findConfirmed('aswesuma') ? 'Eligible' : (findSuggestion('LowIncome') ? 'Suggested' : 'Not Eligible'),
        cardClass: 'card-aswesuma'
      },
      {
        name: 'Public Assistance',
        icon: '/assets/icons/social-services.png',
        status: findConfirmed('public') ? 'Eligible' : (findSuggestion('LowIncome') ? 'Suggested' : 'Not Eligible'),
        cardClass: 'card-public'
      },
      {
        name: 'Elderly Assistance',
        icon: '/assets/icons/elder.png',
        status: findConfirmed('elderly') ? 'Eligible' : (findSuggestion('Elderly') ? 'Suggested' : 'Not Eligible'),
        cardClass: 'card-elderly'
      },
      {
        name: 'Disability Assistance',
        icon: '/assets/icons/diffable.png',
        status: findConfirmed('disability') ? 'Eligible' : 'Not Eligible',
        cardClass: 'card-disability'
      },
      {
        name: 'Kidney Patient Assistance',
        icon: '/assets/icons/kidney.png',
        status: findConfirmed('kidney') ? 'Eligible' : 'Not Eligible',
        cardClass: 'card-kidney'
      },
      {
        name: 'Pregnant Mother Assistance',
        icon: '/assets/icons/baby.png',
        status: findConfirmed('pregnant') || findConfirmed('poshana') ? 'Eligible' : 'Not Eligible',
        cardClass: 'card-pregnant'
      },
      {
        name: 'Housing Assistance',
        icon: '/assets/icons/home.png',
        status: findConfirmed('housing') ? 'Eligible' : 'Not Eligible',
        cardClass: 'card-housing'
      },
      {
        name: 'Voter Registration',
        icon: '/assets/icons/vote.png',
        status: findConfirmed('voter') ? 'Eligible' : (findSuggestion('Voter') ? 'Suggested' : 'Not Eligible'),
        cardClass: 'card-voter'
      }
    ];

    return programs.map(p => {
      let statusClass = 'status-not-eligible';
      if (p.status === 'Eligible') statusClass = 'status-eligible';
      else if (p.status === 'Suggested') statusClass = 'status-suggested';

      return { ...p, statusClass };
    });
  }

  hasActiveWelfare(): boolean {
    return this.getWelfarePrograms().some(p => p.status === 'Eligible');
  }

  hasSuggestions(): boolean {
    return this.getWelfarePrograms().some(p => p.status === 'Suggested');
  }

  getCommonVerificationDetails(confirmedWelfare: any[]): { date: string; officer: string } | null {
    if (!confirmedWelfare) return null;
    const item = confirmedWelfare.find((w: any) => w.lastVerifiedDate || w.verifiedBy);
    if (!item) return null;
    return {
      date: item.lastVerifiedDate || '',
      officer: item.verifiedBy || ''
    };
  }

  loadVoters() {
    const token = sessionStorage.getItem('token');
    if (!token || !this.gnDivisionId) return;
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    
    this.http.get<any>(`http://localhost:5058/api/VoterRegistry/division/${encodeURIComponent(this.gnDivisionId)}`, { headers }).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.voters = res.data;
          this.filterVoters();
        }
      },
      error: (err) => console.error('Error loading voters:', err)
    });
  }

  filterVoters() {
    let filtered = this.voters || [];
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      filtered = filtered.filter(v => 
        (v.citizenName && v.citizenName.toLowerCase().includes(q)) || 
        (v.citizenNic && v.citizenNic.toLowerCase().includes(q))
      );
    }
    this.filteredVoters = filtered;
  }

  exportVoters() {
    if (!this.gnDivisionId) return;
    const token = sessionStorage.getItem('token');
    if (!token) return;
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    
    this.isSubmitting = true;
    this.http.post(`http://localhost:5058/api/VoterRegistry/division/${encodeURIComponent(this.gnDivisionId)}/export`, {}, {
      headers,
      responseType: 'blob'
    }).subscribe({
      next: (blob) => {
        this.isSubmitting = false;
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `voters_export_${this.gnDivisionId}_${new Date().toISOString().slice(0,10)}.csv`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        
        // Reload voters list to update IsExported flags
        this.loadVoters();
        
        this.alertModalTitle = 'Export Successful';
        this.alertModalMessage = 'Approved voter records exported successfully as CSV, and marked as exported.';
        this.alertModalType = 'success';
        this.isAlertModalOpen = true;
      },
      error: (err) => {
        this.isSubmitting = false;
        console.error('Export failed:', err);
        this.alertModalTitle = 'Export Failed';
        this.alertModalMessage = 'Failed to export voter records. No new approved voter records to export or connection error.';
        this.alertModalType = 'error';
        this.isAlertModalOpen = true;
      }
    });
  }
}
