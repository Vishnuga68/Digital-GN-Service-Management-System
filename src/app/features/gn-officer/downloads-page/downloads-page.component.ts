import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface DocumentCategory {
  id: string;
  name: string;
  count: number;
}

export interface DocumentItem {
  id: string;
  title: string;
  category: string;
  type: string;
  size: string;
  date: Date;
  downloads: number;
  featured?: boolean;
}

@Component({
  selector: 'app-downloads-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './downloads-page.component.html',
  styleUrls: ['./downloads-page.component.scss']
})
export class DownloadsPageComponent implements OnInit {
  isLoading = true;

  categories: DocumentCategory[] = [];
  documents: DocumentItem[] = [];
  filteredDocuments: DocumentItem[] = [];

  activeCategory: string = 'All';
  searchQuery: string = '';

  // Upload/Manage Modals
  isUploadModalOpen = false;
  isCategoryModalOpen = false;

  newDocTitle = '';
  newDocCategory = '';
  newDocFile: any = null;

  newCategoryName = '';

  // Alert Modal
  showAlertModal = false;
  alertMessage = '';
  alertType: 'success' | 'error' = 'success';

  ngOnInit(): void {
    setTimeout(() => {
      this.loadMockData();
      this.filterDocuments();
      this.isLoading = false;
    }, 800);
  }

  loadMockData() {
    this.categories = [
      { id: '1', name: 'Application Forms', count: 12 },
      { id: '2', name: 'Guidelines', count: 5 },
      { id: '3', name: 'Reports & Stats', count: 3 },
      { id: '4', name: 'Templates', count: 8 }
    ];

    this.documents = [
      { id: 'd1', title: 'Residence Certificate - Form A', category: 'Application Forms', type: 'pdf', size: '1.2 MB', date: new Date('2023-11-15'), downloads: 450, featured: true },
      { id: 'd2', title: 'Character Certificate Request Form', category: 'Application Forms', type: 'pdf', size: '0.8 MB', date: new Date('2023-10-20'), downloads: 320, featured: true },
      { id: 'd3', title: 'Business Registration Guidelines 2024', category: 'Guidelines', type: 'pdf', size: '3.5 MB', date: new Date('2024-01-05'), downloads: 150 },
      { id: 'd4', title: 'Grama Niladhari Division Map', category: 'Reports & Stats', type: 'image', size: '5.2 MB', date: new Date('2023-08-12'), downloads: 85 },
      { id: 'd5', title: 'Monthly Welfare Applicant Summary Template', category: 'Templates', type: 'excel', size: '150 KB', date: new Date('2024-02-10'), downloads: 40 },
      { id: 'd6', title: 'Land Ownership Dispute Form', category: 'Application Forms', type: 'word', size: '200 KB', date: new Date('2023-09-01'), downloads: 210 }
    ];
  }

  setCategory(cat: string) {
    this.activeCategory = cat;
    this.filterDocuments();
  }

  filterDocuments() {
    let filtered = this.documents;

    if (this.activeCategory !== 'All') {
      filtered = filtered.filter(d => d.category === this.activeCategory);
    }

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      filtered = filtered.filter(d => d.title.toLowerCase().includes(q) || d.type.includes(q));
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
     this.newDocCategory = this.categories.length > 0 ? this.categories[0].name : '';
     this.newDocFile = null;
  }

  closeUploadModal() {
     this.isUploadModalOpen = false;
  }

  // Delete Confirmation Modal
  isDeleteModalOpen = false;
  deleteEntityId = '';
  deleteEntityType: 'document' | 'category' = 'document';
  deleteEntityName = '';

  uploadDocument() {
     if(!this.newDocTitle || !this.newDocFile || !this.newDocCategory) {
        this.showToast('Please provide all details and select a file.', 'error');
        return;
     }

     const newDoc: DocumentItem = {
        id: Date.now().toString(),
        title: this.newDocTitle,
        category: this.newDocCategory,
        type: (this.newDocFile.type.includes('pdf') || this.newDocFile.name.toLowerCase().endsWith('.pdf')) ? 'pdf' : 
              (this.newDocFile.type.includes('image') || (this.newDocFile.name.toLowerCase().endsWith('.jpg') || this.newDocFile.name.toLowerCase().endsWith('.png'))) ? 'image' : 
              (this.newDocFile.type.includes('sheet') || this.newDocFile.name.toLowerCase().endsWith('.xlsx')) ? 'excel' : 
              (this.newDocFile.type.includes('word') || this.newDocFile.name.toLowerCase().endsWith('.docx')) ? 'word' : 'pdf',
        size: (this.newDocFile.size / 1024 / 1024).toFixed(1) + ' MB',
        date: new Date(),
        downloads: 0
     };

     this.documents.unshift(newDoc);
     
     // Update category count
     const cat = this.categories.find(c => c.name === this.newDocCategory);
     if(cat) cat.count++;

     this.filterDocuments();
     this.closeUploadModal();
     this.showToast('Document uploaded successfully.', 'success');
  }

  // REPLACED legacy deleteDocument with modal-based logic
  openDeleteDocumentModal(id: string) {
    const doc = this.documents.find(d => d.id === id);
    if (!doc) return;
    this.deleteEntityId = id;
    this.deleteEntityType = 'document';
    this.deleteEntityName = doc.title;
    this.isDeleteModalOpen = true;
  }

  // REPLACED legacy deleteCategory with modal-based logic
  openDeleteCategoryModal(event: Event, catName: string) {
    event.stopPropagation();
    this.deleteEntityId = catName;
    this.deleteEntityType = 'category';
    this.deleteEntityName = catName;
    this.isDeleteModalOpen = true;
  }

  closeDeleteModal() {
    this.isDeleteModalOpen = false;
  }

  confirmDelete() {
    if (this.deleteEntityType === 'document') {
      const docIndex = this.documents.findIndex(d => d.id === this.deleteEntityId);
      if (docIndex > -1) {
        const cat = this.documents[docIndex].category;
        this.documents.splice(docIndex, 1);
        const categoryObj = this.categories.find(c => c.name === cat);
        if (categoryObj) categoryObj.count--;
        this.filterDocuments();
        this.showToast('Document deleted successfully.', 'success');
      }
    } else {
      this.categories = this.categories.filter(c => c.name !== this.deleteEntityId);
      if (this.activeCategory === this.deleteEntityId) {
        this.setCategory('All');
      }
      this.showToast(`Category "${this.deleteEntityId}" deleted.`, 'success');
    }
    this.closeDeleteModal();
  }

  openCategoryModal() {
     this.isCategoryModalOpen = true;
     this.newCategoryName = '';
  }

  closeCategoryModal() {
     this.isCategoryModalOpen = false;
  }

  addCategory() {
     if(this.newCategoryName.trim()) {
        if(this.categories.some(c => c.name.toLowerCase() === this.newCategoryName.trim().toLowerCase())) {
           this.showToast('Category already exists.', 'error');
           return;
        }

        this.categories.push({
           id: Date.now().toString(),
           name: this.newCategoryName.trim(),
           count: 0
        });
        this.closeCategoryModal();
        this.showToast('Category created.', 'success');
     }
  }

  deleteCategory(catName: string) {
     if(confirm(`Delete category "${catName}"? Documents inside will NOT be automatically deleted but may lack a category.`)) {
         this.categories = this.categories.filter(c => c.name !== catName);
         if(this.activeCategory === catName) this.setCategory('All');
     }
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
    switch (type) {
      case 'pdf': return 'assets/icons/file-pdf.png';
      case 'word': return 'assets/icons/file-word.png';
      case 'excel': return 'assets/icons/file-excel.png';
      case 'image': return 'assets/icons/file-image.png';
      default: return 'assets/icons/file-default.png';
    }
  }
}
