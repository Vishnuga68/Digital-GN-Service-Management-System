import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-my-profile-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './my-profile-page.component.html',
  styleUrls: ['./my-profile-page.component.scss']
})
export class MyProfilePageComponent implements OnInit {
  private authService = inject(AuthService);

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
  }
}
