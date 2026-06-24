import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { DocumentVaultService, DocumentVaultResponseDTO } from '../../services/document-vault/document-vault.service';

@Component({
  selector: 'app-document-vault-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './document-vault-page.component.html',
  styleUrls: ['./document-vault-page.component.scss']
})
export class DocumentVaultPageComponent implements OnInit {
  isLoading = true;
  documents: DocumentVaultResponseDTO[] = [];
  filteredDocuments: DocumentVaultResponseDTO[] = [];
  
  categories: string[] = [];
  activeCategory: string = 'All';

  // Upload Modal
  isUploadModalOpen = false;
  newDocTitle = '';
  newDocCategory = '';
  newDocDescription = '';
  newDocFile: File | null = null;

  // Manage Categories Modal
  isManageCategoriesModalOpen = false;
  newCategoryInput = '';
  editingCategoryIndex: number | null = null;

  // Delete Modal
  isDeleteModalOpen = false;
  documentToDeleteId: number | null = null;

  // Alert
  showAlertModal = false;
  alertMessage = '';
  alertType: 'success' | 'error' = 'success';

  private vaultService = inject(DocumentVaultService);

  ngOnInit(): void {
    this.loadCategories();
    this.loadDocuments();
  }

  loadCategories() {
    this.vaultService.getCategories().subscribe({
      next: (data) => {
        this.categories = data.categories || [];
      },
      error: (err) => console.error('Failed to load categories', err)
    });
  }

  loadDocuments() {
    this.isLoading = true;
    this.vaultService.getMyDocuments().subscribe({
      next: (data) => {
        this.documents = data;
        this.filterDocuments();
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load vault documents', err);
        this.isLoading = false;
      }
    });
  }

  setCategory(catName: string) {
    this.activeCategory = catName;
    this.filterDocuments();
  }

  filterDocuments() {
    if (this.activeCategory === 'All') {
      this.filteredDocuments = this.documents;
    } else {
      this.filteredDocuments = this.documents.filter(d => d.categoryName === this.activeCategory);
    }
  }

  handleFileInput(event: any) {
    if(event.target.files.length > 0) {
       this.newDocFile = event.target.files[0];
       if(!this.newDocTitle && this.newDocFile) {
          this.newDocTitle = this.newDocFile.name;
       }
    }
  }

  openUploadModal() {
     this.isUploadModalOpen = true;
     this.newDocTitle = '';
     this.newDocCategory = this.categories.length > 0 ? this.categories[0] : '';
     this.newDocDescription = '';
     this.newDocFile = null;
  }

  closeUploadModal() {
     this.isUploadModalOpen = false;
  }

  uploadDocument() {
     if(!this.newDocTitle || !this.newDocFile || !this.newDocCategory) {
        this.showToast('Please provide all details and select a file.', 'error');
        return;
     }

     const formData = new FormData();
     formData.append('DocumentName', this.newDocTitle);
     formData.append('CategoryName', this.newDocCategory);
     formData.append('Description', this.newDocDescription || this.newDocTitle);
     formData.append('File', this.newDocFile);

     this.vaultService.uploadDocument(formData).subscribe({
       next: () => {
         this.showToast('Document uploaded to Vault successfully.', 'success');
         this.loadDocuments();
         this.closeUploadModal();
       },
       error: (err) => {
         console.error('Upload failed', err);
         this.showToast('Failed to upload document to Vault.', 'error');
       }
     });
  }

  downloadDocument(doc: DocumentVaultResponseDTO) {
     if (doc.filePath) {
       window.open(`http://localhost:5058${doc.filePath}`, '_blank');
     } else {
       this.showToast(`Download path not available for ${doc.documentName}`, 'error');
     }
  }

  deleteDocument(id: number) {
     this.documentToDeleteId = id;
     this.isDeleteModalOpen = true;
  }

  closeDeleteModal() {
     this.isDeleteModalOpen = false;
     this.documentToDeleteId = null;
  }

  confirmDelete() {
     if (this.documentToDeleteId !== null) {
       this.vaultService.deleteDocument(this.documentToDeleteId).subscribe({
         next: () => {
           this.showToast('Document deleted successfully.', 'success');
           this.loadDocuments();
           this.closeDeleteModal();
         },
         error: () => {
           this.showToast('Failed to delete document.', 'error');
           this.closeDeleteModal();
         }
       });
     }
  }



  // --- Category Management ---
  openManageCategoriesModal() {
    this.isManageCategoriesModalOpen = true;
    this.newCategoryInput = '';
    this.editingCategoryIndex = null;
  }

  closeManageCategoriesModal() {
    this.isManageCategoriesModalOpen = false;
  }

  addCategory() {
    if (this.newCategoryInput.trim()) {
      if (this.editingCategoryIndex !== null) {
        this.categories[this.editingCategoryIndex] = this.newCategoryInput.trim();
        this.editingCategoryIndex = null;
      } else {
        if (!this.categories.includes(this.newCategoryInput.trim())) {
          this.categories.push(this.newCategoryInput.trim());
        }
      }
      this.saveCategories();
      this.newCategoryInput = '';
    }
  }

  editCategory(index: number) {
    this.newCategoryInput = this.categories[index];
    this.editingCategoryIndex = index;
  }

  removeCategory(index: number) {
    if(confirm('Are you sure you want to remove this category?')) {
      this.categories.splice(index, 1);
      this.saveCategories();
    }
  }

  saveCategories() {
    this.vaultService.updateCategories(this.categories).subscribe({
      next: (res) => {
        this.categories = res.categories || [];
        this.showToast('Categories updated successfully.', 'success');
      },
      error: () => this.showToast('Failed to update categories.', 'error')
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
}
