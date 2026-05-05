import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-gn-registration-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gn-registration-page.component.html',
  styleUrls: ['./gn-registration-page.component.scss']
})
export class GnRegistrationPageComponent implements OnInit {
  newGN = {
    name: '',
    nic: '',
    division: '',
    email: '',
    phone: '',
    tempPassword: ''
  };

  constructor() { }

  ngOnInit(): void {
    this.generateTempPassword();
  }

  generateTempPassword() {
    this.newGN.tempPassword = 'GN@' + Math.floor(1000 + Math.random() * 9000);
  }

  registerGN() {
    alert(`GN Officer ${this.newGN.name} registered successfully!\nTemporary Password: ${this.newGN.tempPassword}`);
    // Reset form
    this.newGN = { name: '', nic: '', division: '', email: '', phone: '', tempPassword: '' };
    this.generateTempPassword();
  }
}
