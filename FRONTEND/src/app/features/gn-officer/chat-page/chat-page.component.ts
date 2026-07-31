import { Component, OnInit, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';

export interface ChatMessage {
  id: string;
  sender: 'citizen' | 'gn-officer';
  text: string;
  time: Date;
  isRead: boolean;
  status: 'sent' | 'delivered' | 'read';
  attachment?: {
    name: string;
    type: string;
    url: string;
  };
}

export interface Conversation {
  id: string;
  citizenName: string;
  citizenNIC: string;
  avatar: string;
  lastMessage: string;
  lastMessageTime: Date;
  unreadCount: number;
  isOnline: boolean;
  requestId?: string;
  requestNumber?: string;
  canSendMessages: boolean;
  messages: ChatMessage[];
}

@Component({
  selector: 'app-chat-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './chat-page.component.html',
  styleUrls: ['./chat-page.component.scss']
})
export class ChatPageComponent implements OnInit, AfterViewChecked {
  @ViewChild('scrollMe') private myScrollContainer!: ElementRef;
  
  conversations: Conversation[] = [];
  filteredConversations: Conversation[] = [];
  searchQuery = '';
  
  activeConversation: Conversation | null = null;
  newMessage = '';
  isUploading = false;
  showEmojiPicker = false;
  activeFilter: 'all' | 'common' | 'request' = 'all';
  selectedFile: File | null = null;
  
  quickReplies = [
    'Your document is ready for collection.',
    'Please visit the office to verify your NIC.',
    'Your request has been approved today.',
    'Could you provide more details regarding your request?'
  ];

  constructor(private route: ActivatedRoute, private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    const token = sessionStorage.getItem('token');
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }

  ngOnInit(): void {
    this.loadConversations(() => {
      this.route.queryParams.subscribe(params => {
        const citizenNIC = params['citizenNIC'];
        const requestId = params['requestId'];

        if (citizenNIC && requestId) {
          const conv = this.conversations.find(c => 
            c.citizenNIC === citizenNIC && c.requestId === requestId
          );
          if (conv) {
            this.activeFilter = 'request';
            this.selectConversation(conv);
            this.filterConversations();
          }
        } else if (requestId) {
          const conv = this.conversations.find(c => c.requestId === requestId);
          if (conv) {
            this.activeFilter = 'request';
            this.selectConversation(conv);
            this.filterConversations();
          }
        } else if (citizenNIC) {
          const conv = this.conversations.find(c => c.citizenNIC === citizenNIC && !c.requestId);
          if (conv) {
            this.activeFilter = 'common';
            this.selectConversation(conv);
            this.filterConversations();
          }
        }
      });
    });
  }

  ngAfterViewChecked() {
    this.scrollToBottom();
  }

  loadConversations(callback?: () => void) {
    this.http.get<any>('http://localhost:5058/api/Chat/conversations', { headers: this.getHeaders() })
      .subscribe({
        next: (res) => {
          if (res && res.success && res.data) {
            this.conversations = res.data.map((c: any) => ({
              id: c.conversationId.toString(),
              citizenName: c.citizenName || 'Citizen',
              citizenNIC: c.citizenNic,
              avatar: (c.citizenName || 'C').charAt(0).toUpperCase(),
              lastMessage: c.lastMessage || '',
              lastMessageTime: new Date(c.lastMessageDate),
              unreadCount: c.unreadCount,
              isOnline: false,
              requestId: c.requestId ? c.requestId.toString() : undefined,
              requestNumber: c.requestNumber,
              canSendMessages: c.canSendMessages,
              messages: []
            }));
            this.filterConversations();
            if (callback) callback();
          }
        },
        error: (err) => {
          console.error('[Chat loadConversations Error]', err);
        }
      });
  }

  filterConversations() {
    let result = [...this.conversations];

    if (this.activeFilter === 'common') {
       result = result.filter(c => !c.requestId);
    } else if (this.activeFilter === 'request') {
       result = result.filter(c => !!c.requestId);
    }

    if (this.searchQuery) {
       const query = this.searchQuery.toLowerCase();
       result = result.filter(c => 
          c.citizenName.toLowerCase().includes(query) || 
          c.citizenNIC.includes(query) ||
          (c.requestId && c.requestId.toLowerCase().includes(query)) ||
          (c.requestNumber && c.requestNumber.toLowerCase().includes(query))
       );
    }

    this.filteredConversations = result;
  }

  setFilter(filter: 'all' | 'common' | 'request') {
    this.activeFilter = filter;
    this.filterConversations();
  }

  selectConversation(conv: Conversation) {
    this.activeConversation = conv;
    this.http.get<any>(`http://localhost:5058/api/Chat/conversations/${conv.id}/messages?page=1&pageSize=100`, { headers: this.getHeaders() })
      .subscribe({
        next: (res) => {
          if (res && res.success && res.data) {
            conv.messages = res.data.map((m: any) => ({
              id: m.messageId.toString(),
              sender: m.senderRole.toLowerCase() === 'gnofficer' ? 'gn-officer' : 'citizen',
              text: m.messageText || '',
              time: new Date(m.sentDate),
              isRead: m.isRead,
              status: m.isRead ? 'read' : 'delivered',
              attachment: m.attachments && m.attachments.length > 0 ? {
                name: m.attachments[0].originalFileName,
                type: m.attachments[0].contentType.includes('image') ? 'image' : 'file',
                url: 'http://localhost:5058' + m.attachments[0].relativeFilePath
              } : undefined
            }));
            this.markAsRead(conv);
          }
          this.scrollToBottom();
        },
        error: (err) => {
          console.error('[Chat selectConversation Error]', err);
        }
      });
  }

  markAsRead(conv: Conversation) {
    this.http.put<any>(`http://localhost:5058/api/Chat/conversations/${conv.id}/read`, {}, { headers: this.getHeaders() })
      .subscribe({
        next: () => {
          conv.unreadCount = 0;
        },
        error: (err) => console.error('[Chat markAsRead Error]', err)
      });
  }

  scrollToBottom(): void {
    try {
      if (this.myScrollContainer) {
        this.myScrollContainer.nativeElement.scrollTop = this.myScrollContainer.nativeElement.scrollHeight;
      }
    } catch(err) { }
  }

  sendMessage() {
    if ((!this.newMessage.trim() && !this.selectedFile) || !this.activeConversation || !this.activeConversation.canSendMessages) return;

    this.isUploading = this.selectedFile ? true : false;
    const fileToUpload = this.selectedFile;
    this.selectedFile = null;

    const messageText = this.newMessage.trim() || `Sent an attachment: ${fileToUpload?.name}`;
    const body = {
      messageText: messageText
    };

    const conv = this.activeConversation;

    this.http.post<any>(`http://localhost:5058/api/Chat/conversations/${conv.id}/messages`, body, { headers: this.getHeaders() })
      .subscribe({
        next: (res) => {
          if (res && res.success && res.data) {
            const m = res.data;
            const msg: ChatMessage = {
              id: m.messageId.toString(),
              sender: 'gn-officer',
              text: m.messageText || '',
              time: new Date(m.sentDate),
              isRead: m.isRead,
              status: 'sent',
              attachment: m.attachments && m.attachments.length > 0 ? {
                name: m.attachments[0].originalFileName,
                type: m.attachments[0].contentType.includes('image') ? 'image' : 'file',
                url: 'http://localhost:5058' + m.attachments[0].relativeFilePath
              } : undefined
            };

            if (fileToUpload) {
              const formData = new FormData();
              formData.append('file', fileToUpload);

              this.http.post<any>(`http://localhost:5058/api/Chat/conversations/${conv.id}/messages/${m.messageId}/attachments`, formData, { headers: this.getHeaders() })
                .subscribe({
                  next: (attachRes) => {
                    this.isUploading = false;
                    if (attachRes && attachRes.success && attachRes.data) {
                      msg.attachment = {
                        name: attachRes.data.originalFileName,
                        type: attachRes.data.contentType.includes('image') ? 'image' : 'file',
                        url: 'http://localhost:5058' + attachRes.data.relativeFilePath
                      };
                    }
                    conv.messages.push(msg);
                    conv.lastMessage = msg.text || '[Attachment]';
                    conv.lastMessageTime = msg.time;
                    this.conversations.sort((a,b) => b.lastMessageTime.getTime() - a.lastMessageTime.getTime());
                    this.filterConversations();

                    this.newMessage = '';
                    this.showEmojiPicker = false;
                    this.scrollToBottom();
                  },
                  error: (err) => {
                    console.error('[Chat uploadFile Error]', err);
                    this.isUploading = false;
                    conv.messages.push(msg);
                    conv.lastMessage = msg.text || '[Attachment]';
                    conv.lastMessageTime = msg.time;
                    this.conversations.sort((a,b) => b.lastMessageTime.getTime() - a.lastMessageTime.getTime());
                    this.filterConversations();

                    this.newMessage = '';
                    this.showEmojiPicker = false;
                    this.scrollToBottom();
                  }
                });
            } else {
              conv.messages.push(msg);
              conv.lastMessage = msg.text || '[Attachment]';
              conv.lastMessageTime = msg.time;
              this.conversations.sort((a,b) => b.lastMessageTime.getTime() - a.lastMessageTime.getTime());
              this.filterConversations();

              this.newMessage = '';
              this.showEmojiPicker = false;
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

  sendQuickReply(reply: string) {
    this.newMessage = reply;
    this.sendMessage();
  }

  handleFileInput(event: any) {
    const conv = this.activeConversation;
    if (!conv) return;

    if (event.target.files.length > 0 && conv.canSendMessages) {
      this.selectedFile = event.target.files[0];
    }
  }

  clearSelectedFile() {
    this.selectedFile = null;
  }
}
