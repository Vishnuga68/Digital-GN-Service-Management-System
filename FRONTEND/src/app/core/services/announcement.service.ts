import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';


export interface GnAnnouncementResponseDTO {
  announcementId: number;
  gnOfficerNic: string;
  gnDivisionId: string;
  announcementType: string;
  title: string;
  description: string;
  documentId?: number;
  documentName?: string;
  documentUrl?: string;
  externalSurveyUrl?: string;
  pollQuestion?: string;
  publishDate: Date | null;
  expiryDate: Date;
  createdAt: Date;
  createdByName?: string;
  modifiedByName?: string;
  status: string;
  hasVoted?: boolean;
  userVotedOptionId?: number;
  pollOptions: GnPollOptionResponseDTO[];
}

export interface GnPollOptionResponseDTO {
  optionId: number;
  optionText: string;
  votes: number;
}

export interface GnAnnouncementRequestDTO {
  announcementType: string;
  title: string;
  description?: string;
  documentId?: number;
  externalSurveyUrl?: string;
  pollQuestion?: string;
  publishDate: string | Date | null;
  expiryDate: string | Date;
  pollOptions?: string[];
}

export interface GnDocumentResponseDTO {
  documentId: number;
  title: string;
  fileName: string;
}

@Injectable({
  providedIn: 'root'
})
export class AnnouncementService {
  private apiUrl = 'http://localhost:5058/api/GnAnnouncements';

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    const token = sessionStorage.getItem('token');
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }

  getGnAnnouncements(): Observable<GnAnnouncementResponseDTO[]> {
    return this.http.get<GnAnnouncementResponseDTO[]>(`${this.apiUrl}/gn`, { headers: this.getHeaders() });
  }

  getCitizenAnnouncements(): Observable<GnAnnouncementResponseDTO[]> {
    return this.http.get<GnAnnouncementResponseDTO[]>(`${this.apiUrl}/citizen`, { headers: this.getHeaders() });
  }

  getAnnouncementById(id: number): Observable<GnAnnouncementResponseDTO> {
    return this.http.get<GnAnnouncementResponseDTO>(`${this.apiUrl}/${id}`, { headers: this.getHeaders() });
  }

  createAnnouncement(request: GnAnnouncementRequestDTO): Observable<GnAnnouncementResponseDTO> {
    return this.http.post<GnAnnouncementResponseDTO>(this.apiUrl, request, { headers: this.getHeaders() });
  }

  updateAnnouncement(id: number, request: GnAnnouncementRequestDTO): Observable<GnAnnouncementResponseDTO> {
    return this.http.put<GnAnnouncementResponseDTO>(`${this.apiUrl}/${id}`, request, { headers: this.getHeaders() });
  }

  deleteAnnouncement(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`, { headers: this.getHeaders() });
  }

  submitVote(id: number, optionId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/vote`, { optionId }, { headers: this.getHeaders() });
  }

  getResults(id: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${id}/results`, { headers: this.getHeaders() });
  }

  getPublishedDocuments(): Observable<GnDocumentResponseDTO[]> {
    return this.http.get<GnDocumentResponseDTO[]>(`http://localhost:5058/api/GNDocument?status=Published`, { headers: this.getHeaders() });
  }
}
