import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

interface PlacedElement {
  elementId?: number;
  elementType: string;
  pageNumber: number;
  x: number; // Normalized (0.0 to 1.0)
  y: number; // Normalized (0.0 to 1.0)
  width: number; // Normalized (0.0 to 1.0)
  height: number; // Normalized (0.0 to 1.0)
  rotation: number;
  layerOrder: number;
  textValue?: string;
}

@Component({
  selector: 'app-verification-editor-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './verification-editor-page.component.html',
  styleUrls: ['./verification-editor-page.component.css']
})
export class VerificationEditorPageComponent implements OnInit {
  @ViewChild('pdfCanvas') pdfCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('canvasContainer') canvasContainer!: ElementRef<HTMLDivElement>;

  requestId: string | null = null;
  requestNumber: string = '';
  requestDetails: any = null;
  isLoading = true;
  supportingDocuments: any[] = [];
  mainDocument: any = null;
  applicantType: string = 'N/A';
  customVerificationText: string = 'VERIFIED DOCUMENT';
  relationshipToChild: string | null = null;
  isFinalizeModalOpen = false;
  finalizeRemarks = '';
  isErrorModalOpen = false;
  errorModalMessage = '';

  // Document rendering fields
  isImageDocument = false;
  documentImageUrl = '';
  pdfDocument: any = null;
  currentPage = 1;
  totalPages = 1;
  containerWidth = 595; // A4 standard width in points
  containerHeight = 842; // A4 standard height in points

  // Placed elements state
  placedElements: PlacedElement[] = [];
  selectedElement: PlacedElement | null = null;
  elementCounter = 0;

  // Digital details & mock formats
  mySignature: string | null = null;
  myStamp: string | null = null;
  officerNic = '';
  officerName = 'GN Officer';
  formattedDate = '';

  // Interactivity state
  isDragging = false;
  isResizing = false;
  draggedElement: PlacedElement | null = null;
  dragStartX = 0;
  dragStartY = 0;
  elementStartX = 0;
  elementStartY = 0;
  elementStartW = 0;
  elementStartH = 0;

  // Live preview
  isPreviewOpen = false;
  previewPdfUrl: SafeResourceUrl | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient,
    private sanitizer: DomSanitizer
  ) {
    this.formattedDate = new Date().toISOString().split('T')[0];
  }

  ngOnInit(): void {
    // Get parameters
    this.requestId = this.route.snapshot.paramMap.get('id');

    this.officerNic = sessionStorage.getItem('userNic') || '';

    // Parse user profile
    const profileStr = sessionStorage.getItem('userProfile');
    if (profileStr) {
      const profile = JSON.parse(profileStr);
      if (!this.officerNic) {
        this.officerNic = profile.Nic || profile.nic || '';
      }
      this.officerName = profile.FullName || profile.fullName || 'GN Officer';
    }

    if (this.requestId) {
      this.fetchRequestDetails();
    } else {
      this.isLoading = false;
    }
  }

  fetchRequestDetails() {
    this.isLoading = true;
    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    this.http.get<any>(`http://localhost:5058/api/CitizenRequest/details/${this.requestId}`, { headers }).subscribe({
      next: (data) => {
        this.requestDetails = data;
        this.requestNumber = data.requestId;

        // Map status to user friendly text
        const statusMap: any = {
          'Pending': 'Pending',
          'SUBMITTED': 'Submitted',
          'UNDER_REVIEW': 'Under Review',
          'CHANGES_REQUESTED': 'Changes Requested',
          'RESUBMITTED': 'Resubmitted',
          'VERIFIED': 'Verified',
          'Verified': 'Verified',
          'Approved': 'Approved',
          'Accepted': 'Approved',
          'Rejected': 'Rejected',
          'REVOKED': 'Revoked'
        };
        this.requestDetails.statusText = statusMap[data.currentStatus] || data.currentStatus;

        // Parse applicant type and relationship from dynamic fields
        if (data.dynamicFields) {
          data.dynamicFields.forEach((field: any) => {
            const labelLower = field.label ? field.label.trim().toLowerCase() : '';
            if (labelLower === 'applicant type') {
              let val = field.value;
              if (val && val.trim().toLowerCase() === 'myself') {
                val = 'Account Holder';
              } else if (val && (val.trim().toLowerCase() === 'child under 18' || val.trim().toLowerCase() === 'child')) {
                val = 'Minor Child (Under 18)';
              }
              this.applicantType = val;
            } else if (labelLower.includes('relationship')) {
              this.relationshipToChild = field.value;
            }
          });
        }
        
        // Separate main document and supporting documents
        this.mainDocument = data.attachments?.find((a: any) => a.documentRole === 'MAIN_DOCUMENT' || (a.isActive && a.isPrimary)) || null;
        this.supportingDocuments = data.attachments?.filter((a: any) => a.documentRole === 'SUPPORTING_DOCUMENT' && a.isActive) || [];

        const mainAttachment = this.mainDocument;
        if (mainAttachment) {
          const fileUrl = mainAttachment.fileUrl;
          const extension = fileUrl.split('.').pop()?.toLowerCase();
          
          if (extension === 'png' || extension === 'jpg' || extension === 'jpeg') {
            this.isImageDocument = true;
            this.documentImageUrl = `http://localhost:5058${fileUrl}`;
            this.totalPages = 1;
            this.isLoading = false;
          } else {
            this.isImageDocument = false;
            const pdfUrl = `http://localhost:5058${fileUrl}`;
            this.loadPdfDocument(pdfUrl);
          }

          // Fetch saved elements
          this.fetchSavedPlacements();
        } else {
          this.isLoading = false;
          alert('No main document found for this verification request.');
        }

        // Fetch officer digital details
        this.fetchOfficerDigitalDetails();
      },
      error: (err) => {
        this.isLoading = false;
        console.error('[Error fetching request details]', err);
        alert('Failed to load request details.');
      }
    });
  }

  fetchOfficerDigitalDetails() {
    if (!this.officerNic) return;
    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    this.http.get<any>(`http://localhost:5058/api/gn-officer/digital-details/${this.officerNic}`, { headers }).subscribe({
      next: (res) => {
        this.mySignature = res.signaturePath ? (res.signaturePath.startsWith('data:') ? res.signaturePath : `http://localhost:5058${res.signaturePath}`) : null;
        this.myStamp = res.rubberStampPath ? (res.rubberStampPath.startsWith('data:') ? res.rubberStampPath : `http://localhost:5058${res.rubberStampPath}`) : null;
      },
      error: (err) => console.error('[Error loading digital details]', err)
    });
  }

  fetchSavedPlacements() {
    if (!this.requestNumber) return;
    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    this.http.get<any>(`http://localhost:5058/api/CitizenRequest/verification/${this.requestNumber}/placement`, { headers }).subscribe({
      next: (res) => {
        if (res.success && res.elements) {
          this.placedElements = res.elements.map((e: any) => ({
            elementId: e.elementId || ++this.elementCounter,
            elementType: e.elementType,
            pageNumber: e.pageNumber,
            x: e.x,
            y: e.y,
            width: e.width,
            height: e.height,
            rotation: e.rotation,
            layerOrder: e.layerOrder,
            textValue: e.textValue
          }));
          this.elementCounter = Math.max(this.elementCounter, ...this.placedElements.map(e => e.elementId || 0));
        }
      },
      error: (err) => console.error('[Error loading placements]', err)
    });
  }

  loadPdfDocument(pdfUrl: string) {
    this.loadPdfJs().then((pdfjs) => {
      pdfjs.getDocument(pdfUrl).promise.then(
        (pdf: any) => {
          this.pdfDocument = pdf;
          this.totalPages = pdf.numPages;
          this.renderPdfPage(this.currentPage);
        },
        (err: any) => {
          this.isLoading = false;
          console.error('[PDF loading error]', err);
          alert('Failed to load PDF document.');
        }
      );
    });
  }

  loadPdfJs(): Promise<any> {
    if ((window as any).pdfjsLib) {
      return Promise.resolve((window as any).pdfjsLib);
    }
    return new Promise((resolve) => {
      const script = document.createElement('script');
      script.src = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/2.16.105/pdf.min.js';
      script.onload = () => {
        const pdfjs = (window as any).pdfjsLib;
        pdfjs.GlobalWorkerOptions.workerSrc = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/2.16.105/pdf.worker.min.js';
        resolve(pdfjs);
      };
      document.head.appendChild(script);
    });
  }

  renderPdfPage(pageNum: number) {
    if (!this.pdfDocument) return;
    this.isLoading = true;

    this.pdfDocument.getPage(pageNum).then((page: any) => {
      const canvas = this.pdfCanvas.nativeElement;
      const ctx = canvas.getContext('2d');
      
      // Target display scale
      const viewport = page.getViewport({ scale: 1.25 });
      canvas.height = viewport.height;
      canvas.width = viewport.width;
      
      this.containerWidth = viewport.width;
      this.containerHeight = viewport.height;

      const renderContext = {
        canvasContext: ctx,
        viewport: viewport
      };

      page.render(renderContext).promise.then(() => {
        this.isLoading = false;
      });
    });
  }

  onImageLoaded(event: any) {
    const img = event.target;
    // Set standard viewport dimensions based on image file aspect ratio
    const maxWidth = 700;
    const ratio = maxWidth / img.naturalWidth;
    this.containerWidth = maxWidth;
    this.containerHeight = img.naturalHeight * ratio;
    this.isLoading = false;
  }

  changePage(dir: number) {
    const targetPage = this.currentPage + dir;
    if (targetPage >= 1 && targetPage <= this.totalPages) {
      this.currentPage = targetPage;
      this.selectedElement = null;
      if (!this.isImageDocument) {
        this.renderPdfPage(targetPage);
      }
    }
  }

  isElementPlaced(type: string): boolean {
    return this.placedElements.some(e => e.elementType === type);
  }

  // --- Element Placement Logic ---

  addElement(type: string) {
    if (['SIGNATURE', 'RUBBER_STAMP', 'QR_CODE', 'VERIFIED_DATE', 'GN_OFFICER_NAME'].includes(type)) {
      if (this.isElementPlaced(type)) {
        return; // prevent duplicate addition
      }
    }

    const defaultWidth = 0.20; // 20% of page width
    const defaultHeight = type === 'SIGNATURE' || type === 'RUBBER_STAMP' || type === 'QR_CODE' ? 0.15 : 0.05;

    const newElement: PlacedElement = {
      elementId: ++this.elementCounter,
      elementType: type,
      pageNumber: this.currentPage,
      x: 0.15, // center-ish default
      y: 0.15,
      width: defaultWidth,
      height: defaultHeight,
      rotation: 0,
      layerOrder: this.placedElements.length + 1,
      textValue: type === 'VERIFICATION_TEXT' ? this.customVerificationText : 
                 type === 'GN_OFFICER_NAME' ? this.officerName : 
                 type === 'VERIFIED_DATE' ? this.formattedDate : ''
    };

    this.placedElements.push(newElement);
    this.selectedElement = newElement;
  }

  deleteElement(el: PlacedElement, event: MouseEvent) {
    event.stopPropagation();
    this.placedElements = this.placedElements.filter(e => e.elementId !== el.elementId);
    if (this.selectedElement?.elementId === el.elementId) {
      this.selectedElement = null;
    }
  }

  isTextElement(el: PlacedElement): boolean {
    return el.elementType === 'VERIFICATION_TEXT' || 
           el.elementType === 'GN_OFFICER_NAME' || 
           el.elementType === 'VERIFIED_DATE';
  }

  updateTextValue() {
    // text bound to model directly
  }

  adjustLayer(dir: number) {
    if (!this.selectedElement) return;
    this.selectedElement.layerOrder = Math.max(1, this.selectedElement.layerOrder + dir);
  }

  // --- Interactivity (Drag & Resize) ---

  onMouseDownElement(el: PlacedElement, event: MouseEvent) {
    event.stopPropagation();
    this.selectedElement = el;
    this.draggedElement = el;
    this.isDragging = true;
    this.isResizing = false;

    this.dragStartX = event.clientX;
    this.dragStartY = event.clientY;

    this.elementStartX = el.x * this.containerWidth;
    this.elementStartY = el.y * this.containerHeight;
  }

  onMouseDownResize(el: PlacedElement, event: MouseEvent) {
    event.stopPropagation();
    this.selectedElement = el;
    this.draggedElement = el;
    this.isResizing = true;
    this.isDragging = false;

    this.dragStartX = event.clientX;
    this.dragStartY = event.clientY;

    this.elementStartW = el.width * this.containerWidth;
    this.elementStartH = el.height * this.containerHeight;
  }

  onMouseMove(event: MouseEvent) {
    if (!this.isDragging && !this.isResizing) return;
    if (!this.draggedElement) return;

    const deltaX = event.clientX - this.dragStartX;
    const deltaY = event.clientY - this.dragStartY;

    if (this.isDragging) {
      const newX = Math.max(0, Math.min(this.containerWidth - (this.draggedElement.width * this.containerWidth), this.elementStartX + deltaX));
      const newY = Math.max(0, Math.min(this.containerHeight - (this.draggedElement.height * this.containerHeight), this.elementStartY + deltaY));
      
      this.draggedElement.x = newX / this.containerWidth;
      this.draggedElement.y = newY / this.containerHeight;
    }

    if (this.isResizing) {
      const minSize = 25; // Min size in pixels
      const newW = Math.max(minSize, this.elementStartW + deltaX);
      const newH = Math.max(minSize, this.elementStartH + deltaY);

      this.draggedElement.width = Math.min(this.containerWidth, newW) / this.containerWidth;
      this.draggedElement.height = Math.min(this.containerHeight, newH) / this.containerHeight;
    }
  }

  onMouseUp() {
    this.isDragging = false;
    this.isResizing = false;
    this.draggedElement = null;
  }

  // --- Backend Actions ---

  savePlacements(): Promise<boolean> {
    return new Promise((resolve) => {
      const token = sessionStorage.getItem('token');
      const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
      const body = {
        elements: this.placedElements.map(e => ({
          elementType: e.elementType,
          pageNumber: e.pageNumber,
          x: e.x,
          y: e.y,
          width: e.width,
          height: e.height,
          rotation: e.rotation,
          layerOrder: e.layerOrder,
          textValue: e.textValue
        }))
      };

      this.http.post<any>(`http://localhost:5058/api/CitizenRequest/verification/${this.requestNumber}/placement`, body, { headers }).subscribe({
        next: (res) => {
          console.log('[Placements saved]', res);
          resolve(true);
        },
        error: (err) => {
          console.error('[Error saving placements]', err);
          this.showErrorMessage('Failed to save placements.');
          resolve(false);
        }
      });
    });
  }

  previewVerification() {
    this.isPreviewOpen = true;
    this.previewPdfUrl = null;

    this.savePlacements().then((success) => {
      if (!success) {
        this.isPreviewOpen = false;
        return;
      }

      const token = sessionStorage.getItem('token');
      const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
      const body = {
        elements: this.placedElements.map(e => ({
          elementType: e.elementType,
          pageNumber: e.pageNumber,
          x: e.x,
          y: e.y,
          width: e.width,
          height: e.height,
          rotation: e.rotation,
          layerOrder: e.layerOrder,
          textValue: e.textValue
        }))
      };

      this.http.post<any>(`http://localhost:5058/api/CitizenRequest/verification/${this.requestNumber}/preview`, body, { headers }).subscribe({
        next: (res) => {
          if (res.success && res.previewUrl) {
            const fullUrl = `http://localhost:5058${res.previewUrl}?t=${Date.now()}`;
            this.previewPdfUrl = this.sanitizer.bypassSecurityTrustResourceUrl(fullUrl);
          } else {
            this.isPreviewOpen = false;
            this.showErrorMessage('Failed to generate preview PDF.');
          }
        },
        error: (err) => {
          this.isPreviewOpen = false;
          console.error('[Error generating preview]', err);
          this.showErrorMessage('Failed to generate preview PDF.');
        }
      });
    });
  }

  closePreview() {
    this.isPreviewOpen = false;
    this.previewPdfUrl = null;
  }

  finalizeVerification() {
    this.isFinalizeModalOpen = true;
  }

  cancelFinalize() {
    this.isFinalizeModalOpen = false;
  }

  confirmFinalize() {
    this.isFinalizeModalOpen = false;
    this.executeFinalization();
  }

  executeFinalization() {
    this.isLoading = true;
    this.savePlacements().then((success) => {
      if (!success) {
        this.isLoading = false;
        return;
      }

      const token = sessionStorage.getItem('token');
      const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
      const body = { remarks: this.finalizeRemarks };

      this.http.post<any>(`http://localhost:5058/api/CitizenRequest/verification/${this.requestNumber}/finalize`, body, { headers }).subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.success) {
            this.isPreviewOpen = false;
            this.router.navigate(['/gn-officer/requests', this.requestId], { queryParams: { success: 'true' } });
          } else {
            this.showErrorMessage(res.message || 'Failed to finalize verification.');
          }
        },
        error: (err) => {
          this.isLoading = false;
          console.error('[Error finalising verification]', err);
          this.showErrorMessage(err.error?.message || 'Failed to finalize verification.');
        }
      });
    });
  }

  showErrorMessage(msg: string) {
    this.errorModalMessage = msg;
    this.isErrorModalOpen = true;
  }

  closeErrorModal() {
    this.isErrorModalOpen = false;
  }

  goBack() {
    this.router.navigate(['/gn-officer/requests', this.requestId]);
  }
}
