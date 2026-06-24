import { Component, OnInit, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';

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
  requestStatus: string | null = 'In Progress';
  isDirectMessage: boolean = false;
  
  isLoading: boolean = true;
  newMessage: string = '';
  isUploading: boolean = false;
  officerTyping: boolean = false;

  messages: ChatMessage[] = [];

  constructor(
    private route: ActivatedRoute,
    private location: Location
  ) {}

  ngOnInit(): void {
    // Check if there is a Request ID in query params
    this.route.queryParams.subscribe(params => {
      if (params['requestId']) {
        this.requestId = params['requestId'];
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
    // Simulate empty state for direct messaging or general queries
    setTimeout(() => {
      this.messages = [];
      this.isLoading = false;
    }, 800);
  }

  loadRequestSpecificChat() {
    this.isLoading = true;
    
    // Simulate API call for request-specific chat
    setTimeout(() => {
      this.messages = [
        {
          id: '1',
          senderId: 'gn-1',
          senderType: 'officer',
          content: `Hello! I am reviewing your request (${this.requestId}) for the Grama Niladhari Certificate. Could you please provide a clearer copy of your NIC?`,
          timestamp: new Date(Date.now() - 1000 * 60 * 60 * 2) // 2 hours ago
        },
        {
          id: '2',
          senderId: 'cit-1',
          senderType: 'citizen',
          content: 'Sure, I will upload it right away.',
          timestamp: new Date(Date.now() - 1000 * 60 * 50) // 50 mins ago
        },
        {
          id: '3',
          senderId: 'cit-1',
          senderType: 'citizen',
          content: 'Here is the scanned copy of my NIC.',
          timestamp: new Date(Date.now() - 1000 * 60 * 45), // 45 mins ago
          file: {
            name: 'NIC_Front.pdf',
            url: '#',
            type: 'document'
          }
        },
        {
          id: '4',
          senderId: 'gn-1',
          senderType: 'officer',
          content: 'Thank you. The document is clear. I will process your request.',
          timestamp: new Date(Date.now() - 1000 * 60 * 10) // 10 mins ago
        }
      ];
      this.isLoading = false;
      this.scrollToBottom();
    }, 1500);
  }

  scrollToBottom(): void {
    try {
      if (this.scrollContainer) {
        this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
      }
    } catch(err) { }
  }

  sendMessage() {
    if (!this.newMessage.trim()) return;

    const newMsg: ChatMessage = {
      id: Date.now().toString(),
      senderId: 'cit-1',
      senderType: 'citizen',
      content: this.newMessage.trim(),
      timestamp: new Date()
    };

    this.messages.push(newMsg);
    this.newMessage = '';
    
    // Simulate officer typing and reply
    this.simulateOfficerReply();
  }

  handleKeyDown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  triggerFileUpload() {
    this.isUploading = true;
    setTimeout(() => {
      this.isUploading = false;
      // Simulate file upload success
      const fileMsg: ChatMessage = {
        id: Date.now().toString(),
        senderId: 'cit-1',
        senderType: 'citizen',
        content: 'I have attached a new document.',
        timestamp: new Date(),
        file: {
          name: 'Additional_Doc.pdf',
          url: '#',
          type: 'document'
        }
      };
      this.messages.push(fileMsg);
      this.scrollToBottom();
    }, 1000);
  }

  simulateOfficerReply() {
    this.officerTyping = true;
    setTimeout(() => {
      this.officerTyping = false;
      
      let replyContent = 'Noted. Is there anything else you need help with regarding this request?';
      if (this.isDirectMessage) {
        replyContent = 'Thank you for reaching out. How can I assist you today?';
      }

      this.messages.push({
        id: Date.now().toString(),
        senderId: 'gn-1',
        senderType: 'officer',
        content: replyContent,
        timestamp: new Date()
      });
      this.scrollToBottom();
    }, 3500);
  }

  goBack() {
    this.location.back();
  }
}
