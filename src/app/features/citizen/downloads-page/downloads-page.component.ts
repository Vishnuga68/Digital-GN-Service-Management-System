import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export type FileCategory = 'Certificates' | 'Letters' | 'Applications' | 'Others';

export interface DownloadForm {
  id: string;
  title: string;
  description: string;
  category: FileCategory;
  fileType: 'PDF' | 'DOCX' | 'ICS';
  fileSize?: string;
  uploadDate: Date;
  isNew?: boolean;
}

@Component({
  selector: 'app-downloads-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './downloads-page.component.html',
  styleUrl: './downloads-page.component.scss'
})
export class DownloadsPageComponent implements OnInit {
  searchQuery: string = '';
  selectedCategory: string = 'All';
  isLoading: boolean = true;
  
  categories: string[] = ['All', 'Certificates', 'Letters', 'Applications', 'Others'];
  
  forms: DownloadForm[] = [];
  filteredForms: DownloadForm[] = [];

  ngOnInit(): void {
    this.loadMockForms();
    this.filterForms();
  }

  loadMockForms() {
    this.isLoading = true;
    
    // Simulate API delay
    setTimeout(() => {
      this.forms = [
        {
          id: '1',
          title: 'Birth Certificate Application Form',
          description: 'Official form required to apply for a new or duplicate birth certificate.',
          category: 'Certificates',
          fileType: 'PDF',
          fileSize: '1.2 MB',
          uploadDate: new Date('2024-01-15'),
          isNew: true
        },
        {
          id: '2',
          title: 'Residence Verification Letter Template',
          description: 'A standard template for the residence verification process by GN.',
          category: 'Letters',
          fileType: 'DOCX',
          fileSize: '450 KB',
          uploadDate: new Date('2023-12-10')
        },
        {
          id: '3',
          title: 'Income Certificate Request Form',
          description: 'Form to declare household income for official certification.',
          category: 'Certificates',
          fileType: 'PDF',
          fileSize: '850 KB',
          uploadDate: new Date('2024-02-01'),
          isNew: true
        },
        {
          id: '4',
          title: 'Land Ownership Confirmation Form',
          description: 'Required for reporting land ownership details for legal verification.',
          category: 'Others',
          fileType: 'PDF',
          fileSize: '2.1 MB',
          uploadDate: new Date('2023-11-20')
        },
        {
          id: '5',
          title: 'Grama Niladhari Character Certificate',
          description: 'Application for requesting a character certificate for employment.',
          category: 'Certificates',
          fileType: 'PDF',
          fileSize: '1.1 MB',
          uploadDate: new Date('2024-03-10')
        },
        {
          id: '6',
          title: 'Sri Lanka Holidays 2026 Calendar',
          description: 'Official calendar including all 2026 public, bank, and mercantile holidays in Sri Lanka.',
          category: 'Others',
          fileType: 'ICS',
          fileSize: '15 KB',
          uploadDate: new Date('2026-01-01'),
          isNew: true
        }
      ];
      this.isLoading = false;
      this.filterForms();
    }, 1200);
  }

  filterForms() {
    this.filteredForms = this.forms.filter(form => {
      const matchesSearch = form.title.toLowerCase().includes(this.searchQuery.toLowerCase()) || 
                            form.description.toLowerCase().includes(this.searchQuery.toLowerCase());
      const matchesCategory = this.selectedCategory === 'All' || form.category === this.selectedCategory;
      return matchesSearch && matchesCategory;
    });
  }

  onSearchChange() {
    this.filterForms();
  }

  onCategoryChange() {
    this.filterForms();
  }

  downloadFile(form: DownloadForm) {
    // Analytics/API track before mock download
    console.log(`Downloading: ${form.title}`);
    // In a real app, this would trigger window.location.href or blob download
    alert(`Starting download for ${form.title} (${form.fileType})`);
  }
}
