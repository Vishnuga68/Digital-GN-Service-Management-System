import { Component, OnInit, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';

interface ChatMessage {
  id: string;
  senderId: string;
  senderType: 'citizen' | 'officer';
  content: string;
  timestamp: Date;
  file?: {
    name: string;
    url: string;
    type: 'document' | 'image';
  };
}

@Component({
  selector: 'app-chat-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './chat-page.component.html',
  styleUrl: './chat-page.component.scss'
})
export class ChatPageComponent implements OnInit, AfterViewChecked {
  @ViewChild('scrollContainer') private scrollContainer!: ElementRef;
  
  requestId: string | null = null;
  dbRequestId: string | null = null;
  requestStatus: string | null = 'In Progress';
  isDirectMessage: boolean = false;
  
  isLoading: boolean = true;
  newMessage: string = '';
  isUploading: boolean = false;
  officerTyping: boolean = false;

  messages: ChatMessage[] = [];
  activeConversationId: number | null = null;
  canSendMessages: boolean = true;
  selectedFile: File | null = null;

  constructor(
    private route: ActivatedRoute,
    private location: Location,
    private http: HttpClient
  ) {}

  private getHeaders(): HttpHeaders {
    const token = sessionStorage.getItem('token');
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      if (params['requestId']) {
        this.dbRequestId = params['requestId'];
        this.requestId = params['requestNumber'] || params['requestId'];
        this.requestStatus = 'In Progress';
        this.isDirectMessage = false;
        this.loadRequestSpecificChat();
      } else {
        this.requestId = null;
        this.requestStatus = null;
        this.isDirectMessage = true;
        this.loadDirectMessageChat();
      }
    });
  }

  ngAfterViewChecked() {
    this.scrollToBottom();
  }

  loadDirectMessageChat() {
    this.isLoading = true;
    this.http.get<any>('http://localhost:5058/api/Chat/common', { headers: this.getHeaders() })
      .subscribe({
        next: (res) => {
          if (res && res.success && res.data) {
            this.activeConversationId = res.data.conversationId;
            this.canSendMessages = res.data.canSendMessages;
            this.loadMessages();
          } else {
            this.isLoading = false;
          }
        },
        error: (err) => {
          console.error('[Chat loadDirectMessageChat Error]', err);
          this.isLoading = false;
        }
      });
  }

  loadRequestSpecificChat() {
    this.isLoading = true;
    this.http.get<any>(`http://localhost:5058/api/Chat/request/${this.dbRequestId}`, { headers: this.getHeaders() })
      .subscribe({
        next: (res) => {
          if (res && res.success && res.data) {
            this.activeConversationId = res.data.conversationId;
            this.canSendMessages = res.data.canSendMessages;
            // Extract request details or category name if available
            this.loadMessages();
          } else {
            this.isLoading = false;
          }
        },
        error: (err) => {
          console.error('[Chat loadRequestSpecificChat Error]', err);
          this.isLoading = false;
        }
      });
  }

  loadMessages() {
    if (!this.activeConversationId) return;
    this.http.get<any>(`http://localhost:5058/api/Chat/conversations/${this.activeConversationId}/messages?page=1&pageSize=100`, { headers: this.getHeaders() })
      .subscribe({
        next: (res) => {
          if (res && res.success && res.data) {
            this.messages = res.data.map((m: any) => ({
              id: m.messageId.toString(),
              senderId: m.senderNic,
              senderType: m.senderRole.toLowerCase() === 'gnofficer' ? 'officer' : 'citizen',
              content: m.messageText || '',
              timestamp: new Date(m.sentDate),
              file: m.attachments && m.attachments.length > 0 ? {
                name: m.attachments[0].originalFileName,
                url: 'http://localhost:5058' + m.attachments[0].relativeFilePath,
                type: m.attachments[0].contentType.includes('image') ? 'image' : 'document'
              } : undefined
            }));
            this.markAsRead();
          }
          this.isLoading = false;
          this.scrollToBottom();
        },
        error: (err) => {
          console.error('[Chat loadMessages Error]', err);
          this.isLoading = false;
        }
      });
  }

  markAsRead() {
    if (!this.activeConversationId) return;
    this.http.put<any>(`http://localhost:5058/api/Chat/conversations/${this.activeConversationId}/read`, {}, { headers: this.getHeaders() })
      .subscribe({
        next: () => {},
        error: (err) => console.error('[Chat markAsRead Error]', err)
      });
  }

  scrollToBottom(): void {
    try {
      if (this.scrollContainer) {
        this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
      }
    } catch(err) { }
  }

  sendMessage() {
    if ((!this.newMessage.trim() && !this.selectedFile) || !this.activeConversationId || !this.canSendMessages) return;

    this.isUploading = this.selectedFile ? true : false;
    const fileToUpload = this.selectedFile;
    this.selectedFile = null;

    const messageText = this.newMessage.trim() || `Sent an attachment: ${fileToUpload?.name}`;
    const body = {
      messageText: messageText
    };

    this.http.post<any>(`http://localhost:5058/api/Chat/conversations/${this.activeConversationId}/messages`, body, { headers: this.getHeaders() })
      .subscribe({
        next: (res) => {
          if (res && res.success && res.data) {
            const m = res.data;
            const newMsg: ChatMessage = {
              id: m.messageId.toString(),
              senderId: m.senderNic,
              senderType: m.senderRole.toLowerCase() === 'gnofficer' ? 'officer' : 'citizen',
              content: m.messageText || '',
              timestamp: new Date(m.sentDate),
              file: m.attachments && m.attachments.length > 0 ? {
                name: m.attachments[0].originalFileName,
                url: 'http://localhost:5058' + m.attachments[0].relativeFilePath,
                type: m.attachments[0].contentType.includes('image') ? 'image' : 'document'
              } : undefined
            };

            if (fileToUpload) {
              const formData = new FormData();
              formData.append('file', fileToUpload);

              this.http.post<any>(`http://localhost:5058/api/Chat/conversations/${this.activeConversationId}/messages/${m.messageId}/attachments`, formData, { headers: this.getHeaders() })
                .subscribe({
                  next: (attachRes) => {
                    this.isUploading = false;
                    if (attachRes && attachRes.success && attachRes.data) {
                      newMsg.file = {
                        name: attachRes.data.originalFileName,
                        url: 'http://localhost:5058' + attachRes.data.relativeFilePath,
                        type: attachRes.data.contentType.includes('image') ? 'image' : 'document'
                      };
                    }
                    this.messages.push(newMsg);
                    this.newMessage = '';
                    this.scrollToBottom();
                  },
                  error: (err) => {
                    console.error('[Chat uploadFile Error]', err);
                    this.isUploading = false;
                    this.messages.push(newMsg);
                    this.newMessage = '';
                    this.scrollToBottom();
                  }
                });
            } else {
              this.messages.push(newMsg);
              this.newMessage = '';
              this.scrollToBottom();
            }
          } else {
            this.isUploading = false;
          }
        },
        error: (err) => {
          console.error('[Chat sendMessage Error]', err);
          this.isUploading = false;
        }
      });
  }

  handleKeyDown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  triggerFileUpload() {
    if (!this.activeConversationId || !this.canSendMessages) return;
    
    const fileInput = document.createElement('input');
    fileInput.type = 'file';
    fileInput.accept = '.pdf,.jpg,.jpeg,.png,.doc,.docx,.txt';
    fileInput.onchange = (event: any) => {
      if (event.target.files && event.target.files.length > 0) {
        this.selectedFile = event.target.files[0];
      }
    };
    fileInput.click();
  }

  clearSelectedFile() {
    this.selectedFile = null;
  }

  goBack() {
    this.location.back();
  }
}
