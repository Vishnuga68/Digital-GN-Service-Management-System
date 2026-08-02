import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-verify-document-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './verify-document-page.component.html',
  styleUrls: ['./verify-document-page.component.css']
})
export class VerifyDocumentPageComponent implements OnInit {
  token: string | null = null;
  isLoading = true;
  verificationResult: any = null;

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    this.token = this.route.snapshot.paramMap.get('token');
    if (this.token) {
      this.verifyDocument();
    } else {
      this.isLoading = false;
    }
  }

  verifyDocument() {
    this.isLoading = true;
    this.http.get<any>(`http://localhost:5058/api/CitizenRequest/public/verify/${this.token}`).subscribe({
      next: (res) => {
        this.verificationResult = res;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Verification error', err);
        this.verificationResult = {
          status: 'invalid'
        };
        this.isLoading = false;
      }
    });
  }

  downloadDocument() {
    if (this.verificationResult && this.verificationResult.downloadUrl) {
      const fullUrl = `http://localhost:5058${this.verificationResult.downloadUrl}`;
      window.open(fullUrl, '_blank');
    }
  }
}
