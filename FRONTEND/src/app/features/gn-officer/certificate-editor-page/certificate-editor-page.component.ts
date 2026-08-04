import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { DomSanitizer, SafeHtml, SafeResourceUrl } from '@angular/platform-browser';

interface EditableValues {
  citizenFullName: string;
  citizenNic: string;
  residentialAddress: string;
  requiredFor: string;
  issuedDate: string;
  certificateName?: string;
  residenceFromDate?: string;

  residencePeriod?: string;
  temporaryAddress?: string;
  temporaryFromDate?: string;
  temporaryToDate?: string;
  temporaryResidenceReason?: string;
  deceasedFullName?: string;
  deceasedNic?: string;
  deceasedAddress?: string;
  dateOfDeath?: string;
  placeOfDeath?: string;
  reasonOfDeath?: string;
  occupation?: string;
  monthlyIncome?: string;
  annualIncome?: string;
  certificateBody?: string;
}


@Component({
  selector: 'app-certificate-editor-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './certificate-editor-page.component.html',
  styleUrls: ['./certificate-editor-page.component.css']
})
export class CertificateEditorPageComponent implements OnInit {
  requestId: string | null = null; // requestNumber
  isLoading = true;
  errorMessage = '';

  // Data from backend
  templateId = 0;
  templateName = '';
  language = 'English';
  requestNumber = '';
  citizenName = '';
  citizenNic = '';
  applicantCategory = '';
  status = '';
  templateContent = '';
  rubberStampUrl: string | null = null;
  officerSignatureUrl: string | null = null;
  gnDivisionName = '';
  gnOfficerName = '';
  gnDivisionId = '';
  dsDivisionName = '';
  districtName = '';

  // Family Card Category ID 1 & 2 variables
  categoryId = 0;
  get isFamilyCardCategory(): boolean {
    return (this.categoryId >= 1 && this.categoryId <= 6) || this.categoryId === 53;
  }
  familyCardNumber = '';
  headOfHouseholdName = '';
  headOfHouseholdNic = '';
  houseNumber = '';
  address = '';
  registeredDate = '';
  familyCardStatus = '';
  familyMembersRows = '';
  activeLivingMemberCount = '';
  totalMonthlyIncome = '';

  // Form Model
  editableValues: EditableValues = {
    citizenFullName: '',
    citizenNic: '',
    residentialAddress: '',
    requiredFor: '',
    issuedDate: '',
    certificateName: '',
    residenceFromDate: '',
    residencePeriod: '',
    temporaryAddress: '',
    temporaryFromDate: '',
    temporaryToDate: '',
    temporaryResidenceReason: '',
    deceasedFullName: '',
    deceasedNic: '',
    deceasedAddress: '',
    dateOfDeath: '',
    placeOfDeath: '',
    reasonOfDeath: '',
    occupation: '',
    monthlyIncome: '',
    annualIncome: '',
    certificateBody: ''
  };

  // Original prefilled cached values for reset action
  originalValues: EditableValues = {
    citizenFullName: '',
    citizenNic: '',
    residentialAddress: '',
    requiredFor: '',
    issuedDate: '',
    certificateName: '',
    residenceFromDate: '',
    residencePeriod: '',
    temporaryAddress: '',
    temporaryFromDate: '',
    temporaryToDate: '',
    temporaryResidenceReason: '',
    deceasedFullName: '',
    deceasedNic: '',
    deceasedAddress: '',
    dateOfDeath: '',
    placeOfDeath: '',
    reasonOfDeath: '',
    occupation: '',
    monthlyIncome: '',
    annualIncome: '',
    certificateBody: ''
  };



  // Previews
  renderedHtmlPreview: SafeHtml = '';
  previewPdfUrl: SafeResourceUrl | null = null;
  isPreviewModalOpen = false;
  isSubmitting = false;
  showConfirmGenerateModal = false;
  generateRemarks = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    this.requestId = this.route.snapshot.paramMap.get('id');
    if (this.requestId) {
      this.loadEditorData();
    } else {
      this.errorMessage = 'Invalid request identifier.';
      this.isLoading = false;
    }
  }

  loadEditorData(): void {
    this.isLoading = true;
    this.errorMessage = '';
    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    this.http.get<any>(`http://localhost:5058/api/CitizenRequest/certificate/editor-data/${this.requestId}`, { headers })
      .subscribe({
        next: (data) => {
          this.templateId = data.templateId;
          this.templateName = data.templateName;
          this.language = data.language;
          this.requestNumber = data.requestNumber;
          this.citizenName = data.citizenName;
          this.citizenNic = data.citizenNic;
          this.applicantCategory = data.applicantCategory;
          this.status = data.status;
          this.templateContent = data.templateContent;
          this.rubberStampUrl = data.rubberStampUrl;
          this.officerSignatureUrl = data.officerSignatureUrl;
          this.gnDivisionName = data.gnDivisionName;
          this.gnOfficerName = data.gnOfficerName;
          this.gnDivisionId = data.gnDivisionId || '';
          this.dsDivisionName = data.dsDivisionName || '';
          this.districtName = data.districtName || '';

          // Initialize family card details
          this.categoryId = data.categoryId || 0;
          this.familyCardNumber = data.familyCardNumber || '';
          this.headOfHouseholdName = data.headOfHouseholdName || '';
          this.headOfHouseholdNic = data.headOfHouseholdNic || '';
          this.houseNumber = data.houseNumber || '';
          this.address = data.address || '';
          this.registeredDate = data.registeredDate || '';
          this.familyCardStatus = data.familyCardStatus || '';
          this.familyMembersRows = data.familyMembersRows || '';
          this.activeLivingMemberCount = data.activeLivingMemberCount || '';
          this.totalMonthlyIncome = data.totalMonthlyIncome || '';

          // Initialize editable values
          this.editableValues = {
            citizenFullName: data.editableValues.citizenFullName || '',
            citizenNic: data.editableValues.citizenNic || '',
            residentialAddress: data.editableValues.residentialAddress || '',
            requiredFor: data.editableValues.requiredFor || '',
            issuedDate: data.editableValues.issuedDate || '',
            certificateName: data.editableValues.certificateName || data.templateName || '',
            residenceFromDate: data.editableValues.residenceFromDate || '',
            residencePeriod: data.editableValues.residencePeriod || '',
            temporaryAddress: data.editableValues.temporaryAddress || '',
            temporaryFromDate: data.editableValues.temporaryFromDate || '',
            temporaryToDate: data.editableValues.temporaryToDate || '',
            temporaryResidenceReason: data.editableValues.temporaryResidenceReason || '',
            deceasedFullName: data.editableValues.deceasedFullName || '',
            deceasedNic: data.editableValues.deceasedNic || '',
            deceasedAddress: data.editableValues.deceasedAddress || '',
            dateOfDeath: data.editableValues.dateOfDeath || '',
            placeOfDeath: data.editableValues.placeOfDeath || '',
            reasonOfDeath: data.editableValues.reasonOfDeath || '',
            occupation: data.editableValues.occupation || '',
            monthlyIncome: data.editableValues.monthlyIncome || '',
            annualIncome: data.editableValues.annualIncome || '',
            certificateBody: data.editableValues.certificateBody || ''

          };


          // Cache copy
          this.originalValues = { ...this.editableValues };

          this.updatePreview();
          this.isLoading = false;
        },
        error: (err) => {
          console.error(err);
          this.errorMessage = err.error?.message || 'Failed to load certificate template data. Ensure GN Officer digital details are configured.';
          this.isLoading = false;
        }
      });
  }

  updatePreview(): void {
    if (!this.templateContent) return;

    let html = this.templateContent;

    // 1. Replace src="..." patterns to avoid nesting image tags in preview
    html = this.replaceSrcPlaceholder(html, '{{GovernmentEmblem}}', 'assets/images/national-emblem.jpeg');
    html = this.replaceSrcPlaceholder(html, '{{OfficerSignature}}', this.officerSignatureUrl || '');
    html = this.replaceSrcPlaceholder(html, '{{RubberStamp}}', this.rubberStampUrl || '');
    const qrPlaceholder = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAJQAAACUBAMAAAB2tCuzAAAABlBMVEUAAAD///+l2Z/dAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAB3RJTUUH5gMREQ41B3sCBAAAAB1pVFh0Q29tbWVudAAAAAAAQ3JlYXRlZCB3aXRoIEdJTVBkSubGAAACZElEQVR42u3bTY2DQAwF4F+gAExgAkxAAsSAAJgAE6ABTICBCTAAmMBE/VqapgW6fWd787JpyGvakpe2tI8v9k8PUP9qgApQASpABagAFaACVHzSjD62c/f4LnvV6e04Dtc169Bq/bC1F01VqgIVoAJUgApQASpAxafoqWb09D27lV563QvSjO6zV6233Z15/K5rR2sAFaACVIAKUAEqQAWo+BR906wecZynf/P4XfeK7q1PmjW3N6/1qFnRMwIVoAJUgApQASpABaj4FL1nzepx7h7fZ7eeNevWcT723eP7e+12uFhWgApQASpABagAFaDiU3T39r5nterRaxW9YI/uMzz09k+62/Gk8XupAhWgAlSACtDxVwEd7R0dHR0dHR0dHZ/ioZ6zV53ezr369HbstWrtZ82qR5y7x++6dqy7VwEqQAWoABWgAlSACv9I0TfN6pPmpWodrXvR8Z27x3f5k+6tD9299bNmqgIVoAJUgApQASpAxafoqZ6zu3fP9/w6unv1iHqP6FntZ83qeZ/t1l6qAhWgAlSACtABKv4qoKM9e+vZ2t3+m7M9e+vZ2t3+W6XjU+xt7W6X7nbp+BR71bp2N/fsPnvV6/sH/K5r7Z/i2e5u3V6qAhWgAlSACtABKv4qoKPdZ3v2RnvW7vPfKu2fot2r2j9F7dLt7W6X7uX9W6V7VbtXe6kCVIAKUAEqQAWo+BS9Z+/Z7b/Z26u3n/c356tG/bfevtW+dK8CVIAKUAEqQAWo+BR9+1nzfT/b97PmpXusWnvpnv9hG0EFqAAVoAJUgApQ8Sle2tI+vtif/q/+Acu04D+Z+k76AAAAAElFTkSuQmCC';
    html = this.replaceSrcPlaceholder(html, '{{QrCode}}', qrPlaceholder); // fallback display image for mock QR

    // 2. Replace standard placeholders
    const emblemImg = `<img src="assets/images/national-emblem.jpeg" style="max-height: 120px; max-width: 120px; object-fit: contain; display: block; margin: 0 auto;" />`;
    html = html.replace(/{{GovernmentEmblem}}/gi, emblemImg);
    html = html.replace(/{{CertificateName}}/gi, this.escapeHtml(this.editableValues.certificateName || ''));
    html = html.replace(/{{CertificateTitle}}/gi, this.escapeHtml(this.editableValues.certificateName || ''));
    html = html.replace(/{{CitizenFullName}}/gi, this.escapeHtml(this.editableValues.citizenFullName));
    html = html.replace(/{{CitizenNIC}}/gi, this.escapeHtml(this.editableValues.citizenNic));
    html = html.replace(/{{GnDivisionName}}/gi, this.escapeHtml(this.gnDivisionName));
    html = html.replace(/{{DsDivisionName}}/gi, this.escapeHtml(this.dsDivisionName));
    html = html.replace(/{{DistrictName}}/gi, this.escapeHtml(this.districtName));
    html = html.replace(/{{GnOfficerName}}/gi, this.escapeHtml(this.gnOfficerName));
    html = html.replace(/{{GnDivisionId}}/gi, this.escapeHtml(this.gnDivisionId));
    
    const formattedAddress = this.escapeHtml(this.editableValues.residentialAddress).replace(/\n/g, ', ');
    html = html.replace(/{{ResidentialAddress}}/gi, formattedAddress);
    html = html.replace(/{{RequiredFor}}/gi, this.escapeHtml(this.editableValues.requiredFor));
    html = html.replace(/{{RequestNumber}}/gi, this.escapeHtml(this.requestNumber));
    html = html.replace(/{{IssuedDate}}/gi, this.escapeHtml(this.editableValues.issuedDate));

    html = html.replace(/{{ResidenceFromDate}}/gi, this.escapeHtml(this.editableValues.residenceFromDate || ''));
    html = html.replace(/{{ResidencePeriod}}/gi, this.escapeHtml(this.editableValues.residencePeriod || ''));
    html = html.replace(/{{TemporaryAddress}}/gi, this.escapeHtml(this.editableValues.temporaryAddress || ''));
    html = html.replace(/{{TemporaryFromDate}}/gi, this.escapeHtml(this.editableValues.temporaryFromDate || ''));
    html = html.replace(/{{TemporaryToDate}}/gi, this.escapeHtml(this.editableValues.temporaryToDate || ''));
    html = html.replace(/{{TemporaryResidenceReason}}/gi, this.escapeHtml(this.editableValues.temporaryResidenceReason || ''));
    html = html.replace(/{{DeceasedFullName}}/gi, this.escapeHtml(this.editableValues.deceasedFullName || ''));
    html = html.replace(/{{DeceasedNic}}/gi, this.escapeHtml(this.editableValues.deceasedNic || ''));
    html = html.replace(/{{DeceasedAddress}}/gi, this.escapeHtml(this.editableValues.deceasedAddress || ''));
    html = html.replace(/{{DateOfDeath}}/gi, this.escapeHtml(this.editableValues.dateOfDeath || ''));
    html = html.replace(/{{PlaceOfDeath}}/gi, this.escapeHtml(this.editableValues.placeOfDeath || ''));
    html = html.replace(/{{ReasonOfDeath}}/gi, this.escapeHtml(this.editableValues.reasonOfDeath || ''));
    html = html.replace(/{{Occupation}}/gi, this.escapeHtml(this.editableValues.occupation || ''));
    html = html.replace(/{{CitizenOccupation}}/gi, this.escapeHtml(this.editableValues.occupation || ''));
    html = html.replace(/{{MonthlyIncome}}/gi, this.escapeHtml(this.editableValues.monthlyIncome || ''));
    html = html.replace(/{{AnnualIncome}}/gi, this.escapeHtml(this.editableValues.annualIncome || ''));
    html = html.replace(/{{CertificateBody}}/gi, this.escapeHtml(this.editableValues.certificateBody || ''));
    html = html.replace(/<!-- Certificate content will be generated here -->/gi, this.escapeHtml(this.editableValues.certificateBody || ''));

    if (this.isFamilyCardCategory) {
      html = html.replace(/{{FamilyCardNumber}}/gi, this.escapeHtml(this.familyCardNumber));
      html = html.replace(/{{HeadOfHouseholdName}}/gi, this.escapeHtml(this.headOfHouseholdName));
      html = html.replace(/{{HeadOfHouseholdNic}}/gi, this.escapeHtml(this.headOfHouseholdNic));
      html = html.replace(/{{HouseNumber}}/gi, this.escapeHtml(this.houseNumber));
      html = html.replace(/{{Address}}/gi, this.escapeHtml(this.address));
      html = html.replace(/{{RegisteredDate}}/gi, this.escapeHtml(this.registeredDate));
      html = html.replace(/{{FamilyCardStatus}}/gi, this.escapeHtml(this.familyCardStatus));
      html = html.replace(/{{FamilyMembersRows}}/gi, this.familyMembersRows);
      html = html.replace(/{{ActiveLivingMemberCount}}/gi, this.escapeHtml(this.activeLivingMemberCount));
      html = html.replace(/{{TotalMonthlyIncome}}/gi, this.escapeHtml(this.totalMonthlyIncome));
    }

    // Digital signatures styled fallback img tags
    const stampImg = this.rubberStampUrl 
      ? `<img src="${this.rubberStampUrl}" style="max-height: 120px; max-width: 120px; object-fit: contain; display: block; margin: 0 auto;" />` 
      : `<span style="color:#ef4444; font-size:11px; font-weight:600; border:1px dashed #ef4444; padding:4px; display:inline-block; border-radius:4px;">No Stamp Uploaded</span>`;
    
    const sigImg = this.officerSignatureUrl 
      ? `<img src="${this.officerSignatureUrl}" style="max-height: 80px; max-width: 200px; object-fit: contain; display: block; margin: 0 auto;" />` 
      : `<span style="color:#ef4444; font-size:11px; font-weight:600; border:1px dashed #ef4444; padding:4px; display:inline-block; border-radius:4px;">No Signature Uploaded</span>`;

    const qrImg = `<div style="width: 100px; height: 100px; border: 1.5px solid #cbd5e1; display: flex; align-items: center; justify-content: center; background: #f8fafc; font-size: 8px; color: #64748b; font-weight: bold; border-radius: 4px; text-align: center; padding: 4px; margin: 0 auto;">[Verification QR Code]</div>`;

    html = html.replace(/{{RubberStamp}}/gi, stampImg);
    html = html.replace(/{{OfficerSignature}}/gi, sigImg);
    html = html.replace(/{{QrCode}}/gi, qrImg);

    this.renderedHtmlPreview = this.sanitizer.bypassSecurityTrustHtml(html);
  }

  replaceSrcPlaceholder(html: string, placeholder: string, value: string): string {
    if (!html) return html;
    const escaped = placeholder.replace(/[-\/\\^$*+?.()|[\]{}]/g, '\\$&');
    const regex = new RegExp(`src\\s*=\\s*["']?${escaped}["']?`, 'gi');
    return html.replace(regex, `src="${value}"`);
  }


  escapeHtml(text: string): string {
    if (!text) return '';
    return text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');
  }

  resetFields(): void {
    this.editableValues = { ...this.originalValues };
    this.updatePreview();
  }

  goBack(): void {
    this.router.navigate(['/gn-officer/requests', this.requestId]);
  }

  previewPdf(): void {
    this.errorMessage = '';
    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    this.http.post<any>(`http://localhost:5058/api/CitizenRequest/certificate/preview-pdf/${this.requestId}`, { editableValues: this.editableValues }, { headers })
      .subscribe({
        next: (res) => {
          if (res.success && res.previewUrl) {
            this.previewPdfUrl = this.sanitizer.bypassSecurityTrustResourceUrl(`http://localhost:5058${res.previewUrl}`);
            this.isPreviewModalOpen = true;
          } else {
            this.errorMessage = 'Failed to retrieve preview URL from server.';
          }
        },
        error: (err) => {
          console.error(err);
          this.errorMessage = err.error?.message || 'Failed to generate preview PDF. Ensure signature and stamp details are set up.';
        }
      });
  }

  closePreviewModal(): void {
    this.isPreviewModalOpen = false;
    this.previewPdfUrl = null;
  }

  generateCertificate(): void {
    this.showConfirmGenerateModal = true;
  }

  confirmGenerateCertificate(): void {
    this.showConfirmGenerateModal = false;
    this.isSubmitting = true;
    this.errorMessage = '';
    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    const body = { 
      editableValues: this.editableValues,
      remarks: this.generateRemarks
    };

    this.http.post<any>(`http://localhost:5058/api/CitizenRequest/certificate/generate/${this.requestId}`, body, { headers })
      .subscribe({
        next: (res) => {
          // Do not reset isSubmitting to keep the editor spinner active during transition
          this.router.navigate(['/gn-officer/requests', this.requestId], { state: { showGeneratingLoading: true } });
        },
        error: (err) => {
          console.error(err);
          this.isSubmitting = false;
          this.errorMessage = err.error?.message || 'Failed to generate certificate.';
        }
      });
  }
}
