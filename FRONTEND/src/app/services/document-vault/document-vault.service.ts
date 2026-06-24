import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DocumentVaultResponseDTO {
  vaultDocumentId: number;
  nic: string;
  roleId: string;
  categoryName: string;
  documentName: string;
  originalFileName: string;
  filePath: string;
  fileType: string;
  description: string;
  uploadedAt: string;
}

export interface VaultCategoryResponseDTO {
  categories: string[];
}

@Injectable({
  providedIn: 'root'
})
export class DocumentVaultService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5058/api/DocumentVault';
  private catApiUrl = 'http://localhost:5058/api/DocumentVaultCategory';

  private getHeaders(): HttpHeaders {
    const token = sessionStorage.getItem('token');
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }

  getMyDocuments(): Observable<DocumentVaultResponseDTO[]> {
    return this.http.get<DocumentVaultResponseDTO[]>(this.apiUrl, { headers: this.getHeaders() });
  }

  uploadDocument(formData: FormData): Observable<DocumentVaultResponseDTO> {
    return this.http.post<DocumentVaultResponseDTO>(`${this.apiUrl}/upload`, formData, { headers: this.getHeaders() });
  }

  deleteDocument(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`, { headers: this.getHeaders() });
  }

  getCategories(): Observable<VaultCategoryResponseDTO> {
    return this.http.get<VaultCategoryResponseDTO>(this.catApiUrl, { headers: this.getHeaders() });
  }

  updateCategories(categories: string[]): Observable<VaultCategoryResponseDTO> {
    return this.http.post<VaultCategoryResponseDTO>(this.catApiUrl, { categories }, { headers: this.getHeaders() });
  }
}
