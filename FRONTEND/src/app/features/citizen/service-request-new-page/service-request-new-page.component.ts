import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';

export interface DynamicCategory {
  categoryId: number;
  categoryName: string;
  guidelineText?: string;
}

export interface DynamicService {
  serviceId: number;
  serviceName: string;
  icon?: string;
  description?: string;
  categories: DynamicCategory[];
}

export interface DynamicHeading {
  parentHeadingId: number;
  parentHeadingName: string;
  icon?: string;
  description?: string;
  services: DynamicService[];
}

@Component({
  selector: 'app-service-request-new-page',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './service-request-new-page.component.html',
  styleUrls: ['./service-request-new-page.component.scss']
})
export class ServiceRequestNewPageComponent implements OnInit {
  headings: DynamicHeading[] = [];

  constructor(private router: Router, private http: HttpClient) {}

  ngOnInit(): void {
    this.loadHeadings();
  }
  loadHeadings() {
    this.http.get<DynamicHeading[]>('http://localhost:5058/api/DynamicForm/headings-with-services')
      .subscribe(res => {
        this.headings = res;
      }, err => console.error('Failed to load headings', err));
  }

  selectService(service: DynamicService) {
    this.router.navigate(['/citizen/service-requests/new', service.serviceId]);
  }

  goBack() {
    this.router.navigate(['/citizen/service-requests']);
  }
}
