import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { SearchService } from '../../../core/services/search.service';

export interface DocumentCategory {
  categoryId: number;
  categoryName: string;
}

export interface GNDocument {
  documentId: number;
  title: string;
  description: string;
  categoryId: number;
  categoryName: string;
  fileName: string;
  filePath: string;
  fileType: string;
  status: string;
  publishDate: Date;
  uploadDate: Date;
  isNew?: boolean;
}

@Component({
  selector: 'app-document-handling-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './document-handling-page.component.html',
  styleUrl: './document-handling-page.component.scss'
})
export class DocumentHandlingPageComponent implements OnInit {
  searchQuery: string = '';
  selectedCategory: string = 'All Categories';
  isLoading: boolean = true;
  
  categories: any[] = [{ categoryId: 'All', categoryName: 'All Categories' }];
  
  forms: GNDocument[] = [];
  filteredForms: GNDocument[] = [];

  private http = inject(HttpClient);
  private searchService = inject(SearchService);
  private apiUrl = 'http://localhost:5058/api';

  ngOnInit(): void {
    this.searchService.searchQuery$.subscribe(query => {
      this.searchQuery = query;
      this.filterForms();
    });
    this.loadCategories();
    this.loadDocuments();
  }

  getHeaders() {
    const token = sessionStorage.getItem('token');
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }

  loadCategories() {
    this.http.get<DocumentCategory[]>(`${this.apiUrl}/DocumentCategory`, { headers: this.getHeaders() })
      .subscribe({
        next: (data) => {
          this.categories = [{ categoryId: 'All', categoryName: 'All Categories' }, ...data];
        },
        error: (err) => console.error('Failed to load categories', err)
      });
  }

  loadDocuments() {
    this.isLoading = true;
    this.http.get<any[]>(`${this.apiUrl}/GNDocument`, { headers: this.getHeaders() })
      .subscribe({
        next: (data) => {
          this.forms = data.map(doc => ({
            ...doc,
            uploadDate: new Date(doc.publishDate || doc.createdAt)
          }));
          this.isLoading = false;
          this.filterForms();
        },
        error: (err) => {
          console.error('Failed to load documents', err);
          this.isLoading = false;
        }
      });
  }

  filterForms() {
    this.filteredForms = this.forms.filter(form => {
      const matchesSearch = form.title.toLowerCase().includes(this.searchQuery.toLowerCase()) || 
                            form.description.toLowerCase().includes(this.searchQuery.toLowerCase());
      const matchesCategory = this.selectedCategory === 'All Categories' || form.categoryName === this.selectedCategory;
      return matchesSearch && matchesCategory;
    });
  }

  onSearchChange() {
    this.filterForms();
  }

  onCategoryChange() {
    this.filterForms();
  }

  downloadFile(form: GNDocument) {
    if (form.filePath) {
      window.open(`http://localhost:5058${form.filePath}`, '_blank');
    } else {
      alert(`Download path not available for ${form.title}`);
    }
  }
}
