import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { of, forkJoin, Subscription } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-residence-registry-page',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ReactiveFormsModule],
  templateUrl: './residence-registry-page.component.html',
  styleUrl: './residence-registry-page.component.scss'
})
export class ResidenceRegistryPageComponent implements OnInit {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private profileSub?: Subscription;

  isLoading = true;

  myData: any = {
    fullName: '',
    nic: '',
    dob: '',
    gender: '',
    contact: {
      email: '',
      phone: ''
    },
    familyCard: '',
    pastFamilyCard: '',
    familyCardStatus: '',
    province: '',
    district: '',
    dsDivision: '',
    gnDivision: '',
    occupation: '',
    specialStatus: {
      isNalanpuri: false,
      hasKidneyDisease: false,
      hasCancer: false,
      hasElderlyEligible: false,
      hasVoterId: false,
      lifeStatus: ''
    },
    confirmedWelfare: [] as any[],
    suggestions: [] as any[]
  };

  familyData: any = {
    head: '',
    cardNumber: '',
    status: '',
    gn: '',
    members: [],
    historyMembers: [],
    showHistory: false
  };

  originalNic: string | null = null;
  isViewingMember = false;

  ngOnInit() {
    this.originalNic = sessionStorage.getItem('userNic');
    if (this.originalNic) {
      this.profileSub = this.authService.profile$.subscribe(p => {
        if (p && !this.isViewingMember) {
          this.populateProfileData(p, this.originalNic!);
        }
      });
      // Only do HTTP if profile isn't loaded yet and isViewingMember is false, 
      // but AuthService will handle fetching it.
    } else {
      this.isLoading = false;
    }
  }

  ngOnDestroy() {
    if (this.profileSub) {
      this.profileSub.unsubscribe();
    }
  }

  loadMemberData(nic: string) {
    const token = sessionStorage.getItem('token');

    if (!token || !nic) {
      this.isLoading = false;
      return;
    }

    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    // If viewing the logged in user, authService handles it via subscription. 
    // We only need to fetch via API if viewing someone else's profile.
    if (nic !== this.originalNic || this.isViewingMember) {
      this.http.get<any>(`http://localhost:5058/api/Dashboard/citizen/${nic}`, { headers }).subscribe({
        next: (res) => {
          if (res.success && res.data && res.data.profile) {
            this.populateProfileData(res.data.profile, nic);
          } else {
            this.isLoading = false;
          }
        },
        error: (err) => {
          console.error('Error fetching citizen profile:', err);
          this.isLoading = false;
        }
      });
    }
  }

  populateProfileData(p: any, nic: string) {
    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    this.myData.fullName = p.FullName || p.fullName;
    this.myData.nic = p.Nic || p.nic;
    this.myData.dob = this.parseDate(p.Dob || p.dob);
    this.myData.gender = p.Gender || p.gender;
    this.myData.contact.email = p.Email || p.email || 'Not Provided';
    this.myData.contact.phone = p.Phone || p.phone || 'Not Provided';
    this.myData.familyCard = p.FamilyCardNumber || p.familyCardNumber || 'N/A';
    this.myData.pastFamilyCard = p.PastFamilyCardNumber || p.pastFamilyCardNumber || '';
    this.myData.familyCardStatus = p.FamilyCardStatus || p.familyCardStatus || 'Active';
    this.myData.specialStatus.lifeStatus = p.LifeStatus || p.lifeStatus || 'Alive';
    this.myData.province = 'Northern'; // Hardcoded or map if available
    this.myData.district = 'Jaffna';   // Hardcoded or map if available
    this.myData.dsDivision = p.DsDivisionName || p.dsDivisionName || 'N/A';
    this.myData.gnDivision = p.GnDivisionName || p.gnDivisionName || 'N/A';
    this.myData.occupation = p.Occupation || p.occupation || 'N/A';
    
    this.familyData.cardNumber = p.FamilyCardNumber || p.familyCardNumber || 'N/A';
    this.familyData.status = p.FamilyCardStatus || p.familyCardStatus || 'Active';
    this.familyData.gn = p.GnDivisionName || p.gnDivisionName || 'N/A';
    this.familyData.historyMembers = [];
    this.familyData.showHistory = false;

    this.fetchAdditionalDetails(p.FamilyCardNumber || p.familyCardNumber, nic, headers);
  }

  fetchAdditionalDetails(familyCardNumber: string, nic: string, headers: HttpHeaders) {
    const requests: any = {};

    if (familyCardNumber && familyCardNumber !== 'N/A') {
      requests.family = this.http.get<any>(`http://localhost:5058/api/FamilyCard/${familyCardNumber}/members`, { headers }).pipe(
        catchError(() => of(null))
      );
    } else {
      requests.family = of(null);
    }

    requests.specialStatus = this.http.get<any>(`http://localhost:5058/api/CitizenSpecialStatus/${nic}`, { headers }).pipe(
      catchError(() => of(null))
    );

    forkJoin(requests).subscribe((results: any) => {
      // Process Family Members
      if (results.family && results.family.success) {
        const members = results.family.data;
        const headMember = members.find((m: any) => m.relationshipToHead === 'Head' || m.relationshipToHead === 'Self');
        this.familyData.head = headMember ? headMember.fullName : 'Unknown';
        
        this.familyData.members = members.map((m: any) => ({
          nic: m.nic,
          name: m.fullName,
          relationship: m.relationshipToHead,
          dob: this.parseDate(m.dob),
          status: m.lifeStatus,
          special: 'Loading...' // Could be fetched individually, but keeping simple
        }));
      }

      // Process Special Status / Confirmed Welfare
      if (results.specialStatus) {
        const statusData = results.specialStatus.data || results.specialStatus;
        if (statusData && statusData.success !== false) {
          const innerData = statusData.data || statusData;
          this.myData.confirmedWelfare = innerData.confirmedWelfare || [];
          this.myData.suggestions = innerData.suggestions || [];
        }
      }

      this.isLoading = false;
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

  viewFamilyMemberDetails(member: any) {
    if (!member || !member.nic) return;
    this.isViewingMember = true;
    this.isLoading = true;
    this.loadMemberData(member.nic);
    setTimeout(() => window.scrollTo({ top: 0, behavior: 'smooth' }), 100);
  }

  viewPastFamilyHistory() {
    this.familyData.showHistory = true;
    
    if (!this.myData.pastFamilyCard) {
      this.familyData.historyMembers = [];
      this.familyData.pastHistoryNotFound = true;
      return;
    }
    
    const token = sessionStorage.getItem('token');
    if (!token) return;
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    
    let url = `http://localhost:5058/api/FamilyCard/history/members?pastCard=${this.myData.pastFamilyCard}`;

    this.http.get<any>(url, { headers }).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          if (res.data.length === 0) {
            this.familyData.historyMembers = [];
            this.familyData.pastHistoryNotFound = true;
            return;
          }
          this.familyData.pastHistoryNotFound = false;
          this.familyData.historyMembers = res.data.map((m: any) => ({
            nic: m.nic,
            name: m.fullName,
            relationship: m.relationshipToHead,
            dob: this.parseDate(m.dob),
            status: m.lifeStatus,
            currentCard: m.familyCardNumber,
            pastCard: m.pastFamilyCardNumber
          }));
          this.familyData.showHistory = true;
        }
      },
      error: (err) => {
        console.error('Error fetching family history:', err);
        alert('Failed to load family card history.');
      }
    });
  }

  backToMyProfile() {
    this.isViewingMember = false;
    this.isLoading = true;
    if (this.originalNic) {
      const p = this.authService['profileSubject'].value;
      if (p) {
        this.populateProfileData(p, this.originalNic);
      } else {
        // Fallback
        this.authService.fetchProfile().subscribe();
      }
    }
    setTimeout(() => window.scrollTo({ top: 0, behavior: 'smooth' }), 100);
  }

  isVoterEligible(dob: any, lifeStatus?: string): boolean {
    const isAlive = lifeStatus !== undefined ? (lifeStatus === 'Alive' || lifeStatus === 'Active') : (this.myData.specialStatus?.lifeStatus === 'Alive');
    if (!isAlive) return false;
    if (!dob) return false;
    const parsed = this.parseDate(dob);
    const birthDate = parsed instanceof Date ? parsed : new Date(parsed as string);
    if (isNaN(birthDate.getTime())) return false;
    return (new Date().getFullYear() - birthDate.getFullYear()) >= 18;
  }

  isElderly(dob: string, lifeStatus?: string): boolean {
    const isAlive = lifeStatus !== undefined ? (lifeStatus === 'Alive' || lifeStatus === 'Active') : (this.myData.specialStatus?.lifeStatus === 'Alive');
    if (!isAlive) return false;
    return this.calculateAge(dob) >= 60;
  }

  getWelfarePrograms(): any[] {
    const confirmedWelfare = this.myData.confirmedWelfare || [];
    const suggestions = this.myData.suggestions || [];
    
    const findConfirmed = (nameSub: string) => confirmedWelfare.some((w: any) => w.programName?.toLowerCase().includes(nameSub.toLowerCase()));
    const findSuggestion = (typeKey: string) => suggestions.some((s: any) => s.type === typeKey);
    const isVoter = this.isVoterEligible(this.myData.dob);

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
}
