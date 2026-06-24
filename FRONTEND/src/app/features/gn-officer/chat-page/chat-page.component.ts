import { Component, OnInit, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute } from '@angular/router';

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
  requestId?: string; // If associated with a specific request
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
  
  // Quick replies for GN
  quickReplies = [
    'Your document is ready for collection.',
    'Please visit the office to verify your NIC.',
    'Your request has been approved today.',
    'Could you provide more details regarding your request?'
  ];

  constructor(private route: ActivatedRoute) {}

  ngOnInit(): void {
    this.loadMockConversations();
    this.filterConversations();
    
    // Check if we navigated from a request details page
    this.route.queryParams.subscribe(params => {
      const citizenNIC = params['citizenNIC'];
      const requestId = params['requestId'];

      if (citizenNIC && requestId) {
        // Find specific request-based conversation for this citizen
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
  }

  ngAfterViewChecked() {
    this.scrollToBottom();
  }

  loadMockConversations() {
    const today = new Date();
    const yesterday = new Date(today);
    yesterday.setDate(today.getDate() - 1);

    this.conversations = [
      {
        id: 'c1',
        citizenName: 'Nimal Perera',
        citizenNIC: '199012345678',
        avatar: 'N',
        lastMessage: 'Thank you for the update.',
        lastMessageTime: new Date(today.getTime() - 1000 * 60 * 5),
        unreadCount: 1,
        isOnline: true,
        requestId: 'REQ-2023-001',
        messages: [
          { id: 'm1', sender: 'citizen', text: 'Good morning sir, is my income certificate ready?', time: new Date(today.getTime() - 1000 * 60 * 60), isRead: true, status: 'read' },
          { id: 'm2', sender: 'gn-officer', text: 'Yes, it has been approved. You can collect it tomorrow.', time: new Date(today.getTime() - 1000 * 60 * 30), isRead: true, status: 'read' },
          { id: 'm3', sender: 'citizen', text: 'Thank you for the update.', time: new Date(today.getTime() - 1000 * 60 * 5), isRead: false, status: 'delivered' }
        ]
      },
      {
        id: 'c2',
        citizenName: 'Nimali Perera',
        citizenNIC: '199012345679',
        avatar: 'N',
        lastMessage: 'I have attached the required document.',
        lastMessageTime: new Date(today.getTime() - 1000 * 60 * 60 * 2),
        unreadCount: 0,
        isOnline: false,
        messages: [
          { id: 'm1', sender: 'gn-officer', text: 'Please send a clear photo of your NIC back.', time: yesterday, isRead: true, status: 'read' },
          { id: 'm2', sender: 'citizen', text: 'I have attached the required document.', time: new Date(today.getTime() - 1000 * 60 * 60 * 2), isRead: true, status: 'read', attachment: { name: 'NIC_Back.jpg', type: 'image', url: '#' } }
        ]
      },
      {
        id: 'c3',
        citizenName: 'Ruwan Silva',
        citizenNIC: '197823456789',
        avatar: 'R',
        lastMessage: 'When are you visiting?',
        lastMessageTime: yesterday,
        unreadCount: 0,
        isOnline: false,
        requestId: 'REQ-2023-003',
        messages: [
           { id: 'm1', sender: 'citizen', text: 'When are you visiting?', time: yesterday, isRead: true, status: 'read' }
        ]
      }
    ];
  }

  filterConversations() {
    let result = [...this.conversations];

    // Filter by type (Request vs Common)
    if (this.activeFilter === 'common') {
       result = result.filter(c => !c.requestId);
    } else if (this.activeFilter === 'request') {
       result = result.filter(c => !!c.requestId);
    }

    // Filter by search query
    if (this.searchQuery) {
       const query = this.searchQuery.toLowerCase();
       result = result.filter(c => 
          c.citizenName.toLowerCase().includes(query) || 
          c.citizenNIC.includes(query) ||
          (c.requestId && c.requestId.toLowerCase().includes(query))
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
    // Mark as read
    conv.unreadCount = 0;
    conv.messages.forEach(m => {
      if (m.sender === 'citizen') m.isRead = true;
    });
    this.scrollToBottom();
  }

  scrollToBottom(): void {
    try {
      if (this.myScrollContainer) {
        this.myScrollContainer.nativeElement.scrollTop = this.myScrollContainer.nativeElement.scrollHeight;
      }
    } catch(err) { }
  }

  sendMessage() {
    if ((!this.newMessage.trim() && !this.isUploading) || !this.activeConversation) return;

    const msg: ChatMessage = {
      id: Date.now().toString(),
      sender: 'gn-officer',
      text: this.newMessage.trim(),
      time: new Date(),
      isRead: false,
      status: 'sent'
    };

    this.activeConversation.messages.push(msg);
    this.activeConversation.lastMessage = msg.text;
    this.activeConversation.lastMessageTime = msg.time;
    
    // Sort conversations to bring latest to top
    this.conversations.sort((a,b) => b.lastMessageTime.getTime() - a.lastMessageTime.getTime());
    this.filterConversations();

    this.newMessage = '';
    this.showEmojiPicker = false;
    this.scrollToBottom();

    // Auto update status to delivered/read for mock
    setTimeout(() => { msg.status = 'delivered'; }, 1000);
    setTimeout(() => { msg.status = 'read'; }, 3000);
  }

  sendQuickReply(reply: string) {
    this.newMessage = reply;
    this.sendMessage();
  }

  handleFileInput(event: any) {
    if(event.target.files.length > 0 && this.activeConversation) {
      this.isUploading = true;
      const file = event.target.files[0];
      
      setTimeout(() => {
        const msg: ChatMessage = {
          id: Date.now().toString(),
          sender: 'gn-officer',
          text: '',
          time: new Date(),
          isRead: false,
          status: 'sent',
          attachment: {
            name: file.name,
            type: file.type.includes('image') ? 'image' : 'file',
            url: '#'
          }
        };
        this.activeConversation!.messages.push(msg);
        this.isUploading = false;
        this.scrollToBottom();
      }, 1500);
    }
  }
}
