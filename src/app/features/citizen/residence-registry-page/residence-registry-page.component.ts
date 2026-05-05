import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';

@Component({
  selector: 'app-residence-registry-page',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ReactiveFormsModule],
  templateUrl: './residence-registry-page.component.html',
  styleUrl: './residence-registry-page.component.scss'
})
export class ResidenceRegistryPageComponent {
  // Mock Data
  myData = {
    fullName: 'Nimal Perera',
    nic: '199015234567',
    dob: '1990-05-15',
    gender: 'Male',
    contact: {
      email: 'nimal.p@example.com',
      phone: '+94 77 123 4567'
    },
    familyCard: 'FC-COL-001234',
    province: 'Western',
    district: 'Colombo',
    dsDivision: 'Colombo 03',
    gnDivision: 'Kollupitiya',
    occupation: 'Software Engineer',
    specialStatus: {
      isNalanpuri: true,
      hasKidneyDisease: false,
      hasCancer: false,
      hasOtherDisability: false
    }
  };

  familyData = {
    head: 'Sunila Perera',
    cardNumber: 'FC-COL-001234',
    gn: 'Colombo 03',
    members: [
      { name: 'Sunila Perera', relationship: 'Father (Head)', dob: '1960-10-12', status: 'Active', special: 'None' },
      { name: 'Sujatha Perera', relationship: 'Mother', dob: '1965-02-28', status: 'Active', special: 'Nalanpuri' },
      { name: 'Nimal Perera', relationship: 'Self', dob: '1990-05-15', status: 'Active', special: 'Nalanpuri' },
      { name: 'Chaminda Perera', relationship: 'Brother', dob: '2005-08-10', status: 'Active', special: 'None' },
      { name: 'Dulcie Perera', relationship: 'Sister', dob: '2012-11-20', status: 'Active', special: 'None' }
    ]
  };

  calculateAge(dob: string): number {
    const birthDate = new Date(dob);
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const m = today.getMonth() - birthDate.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }
    return age;
  }

  isVoterEligible(dob: string): boolean {
    return this.calculateAge(dob) >= 18;
  }

  isElderly(dob: string): boolean {
    return this.calculateAge(dob) >= 60;
  }
}
