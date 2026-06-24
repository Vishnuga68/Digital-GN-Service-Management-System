import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { RouterModule } from '@angular/router';

export interface DocumentCategory {
  categoryId: number;
  categoryName: string;
  count?: number;
}

export interface DocumentItem {
  documentId: number;
  title: string;
  categoryName: string;
  categoryId: number;
  fileType: string;
  fileName: string;
  filePath: string;
  size: string;
  createdAt: Date;
  downloads: number;
  featured?: boolean;
}

@Component({
  selector: 'app-document-handling-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './document-handling-page.component.html',
  styleUrls: ['./document-handling-page.component.scss']
})
export class DocumentHandlingPageComponent implements OnInit {
  isLoading = true;

  categories: DocumentCategory[] = [];
  documents: DocumentItem[] = [];
  filteredDocuments: DocumentItem[] = [];

  activeCategory: string = 'All';
  searchQuery: string = '';

  // Upload/Manage Modals
  // Upload Modal
  isUploadModalOpen = false;

  newDocTitle = '';
  newDocCategory = '';
  newDocFile: any = null;


  // Alert Modal
  showAlertModal = false;
  alertMessage = '';
  alertType: 'success' | 'error' = 'success';

  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5058/api';

  ngOnInit(): void {
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
          this.categories = data.map(c => ({ ...c, count: 0 }));
          this.updateCategoryCounts();
        },
        error: (err) => console.error('Failed to load categories', err)
      });
  }

  loadDocuments() {
    this.isLoading = true;
    this.http.get<any[]>(`${this.apiUrl}/GNDocument`, { headers: this.getHeaders() })
      .subscribe({
        next: (data) => {
          this.documents = data.map(doc => ({
            documentId: doc.documentId,
            title: doc.title,
            categoryName: doc.categoryName,
            categoryId: doc.categoryId,
            fileType: doc.fileType.toLowerCase(),
            fileName: doc.fileName,
            filePath: doc.filePath,
            size: 'Unknown',
            createdAt: new Date(doc.createdAt),
            downloads: 0
          }));
          this.updateCategoryCounts();
          this.filterDocuments();
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Failed to load documents', err);
          this.isLoading = false;
        }
      });
  }

  updateCategoryCounts() {
    if (this.categories.length && this.documents.length) {
      this.categories.forEach(c => {
        c.count = this.documents.filter(d => d.categoryId === c.categoryId).length;
      });
    }
  }

  setCategory(catName: string) {
    this.activeCategory = catName;
    this.filterDocuments();
  }

  filterDocuments() {
    let filtered = this.documents;

    if (this.activeCategory !== 'All') {
      filtered = filtered.filter(d => d.categoryName === this.activeCategory);
    }

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      filtered = filtered.filter(d => d.title.toLowerCase().includes(q) || d.fileType.includes(q));
    }

    this.filteredDocuments = filtered;
  }

  onSearchChange() {
    this.filterDocuments();
  }

  handleFileInput(event: any) {
    if(event.target.files.length > 0) {
       this.newDocFile = event.target.files[0];
       if(!this.newDocTitle) {
          this.newDocTitle = this.newDocFile.name;
       }
    }
  }

  openUploadModal() {
     this.isUploadModalOpen = true;
     this.newDocTitle = '';
     this.newDocCategory = this.categories.length > 0 ? this.categories[0].categoryName : '';
     this.newDocFile = null;
  }

  closeUploadModal() {
     this.isUploadModalOpen = false;
  }

  // Delete Confirmation Modal
  isDeleteModalOpen = false;
  deleteEntityId: string | number = '';
  deleteEntityName = '';

  uploadDocument() {
     if(!this.newDocTitle || !this.newDocFile || !this.newDocCategory) {
        this.showToast('Please provide all details and select a file.', 'error');
        return;
     }

     const cat = this.categories.find(c => c.categoryName === this.newDocCategory);
     if (!cat) return;

     const formData = new FormData();
     formData.append('Title', this.newDocTitle);
     formData.append('Description', this.newDocTitle);
     formData.append('CategoryId', cat.categoryId.toString());
     formData.append('File', this.newDocFile);
     formData.append('PublishNow', 'true');

     this.http.post(`${this.apiUrl}/GNDocument`, formData, { headers: this.getHeaders() })
       .subscribe({
         next: () => {
           this.showToast('Document uploaded successfully.', 'success');
           this.loadDocuments();
           this.closeUploadModal();
         },
         error: (err) => {
           console.error('Upload failed', err);
           this.showToast('Failed to upload document.', 'error');
         }
       });
  }

  openDeleteDocumentModal(id: number) {
    const doc = this.documents.find(d => d.documentId === id);
    if (!doc) return;
    this.deleteEntityId = id;
    this.deleteEntityName = doc.title;
    this.isDeleteModalOpen = true;
  }


  closeDeleteModal() {
    this.isDeleteModalOpen = false;
  }

  confirmDelete() {
    this.http.delete(`${this.apiUrl}/GNDocument/${this.deleteEntityId}`, { headers: this.getHeaders() })
      .subscribe({
        next: () => {
          this.showToast('Document deleted successfully.', 'success');
          this.loadDocuments();
          this.closeDeleteModal();
        },
        error: () => this.showToast('Failed to delete document.', 'error')
      });
  }


  showToast(message: string, type: 'success' | 'error') {
     this.alertMessage = message;
     this.alertType = type;
     this.showAlertModal = true;
  }

  closeAlert() {
     this.showAlertModal = false;
  }

  getFileIconPath(type: string): string {
    switch (type.toLowerCase()) {
      case 'pdf': return 'assets/icons/file-pdf.png';
      case 'docx':
      case 'word': return 'assets/icons/file-word.png';
      case 'xlsx':
      case 'excel': return 'assets/icons/file-excel.png';
      case 'jpg':
      case 'png':
      case 'image': return 'assets/icons/file-image.png';
      default: return 'assets/icons/file-default.png';
    }
  }
}
