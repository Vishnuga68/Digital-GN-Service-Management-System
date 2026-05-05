import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class TranslationService {
  private http = inject(HttpClient);
  
  private currentLangSubject = new BehaviorSubject<string>('en');
  public currentLang$ = this.currentLangSubject.asObservable();
  
  private translationsSubject = new BehaviorSubject<any>({});
  public translations$ = this.translationsSubject.asObservable();

  constructor() {
    this.setLanguage('en');
  }

  setLanguage(lang: string) {
    this.currentLangSubject.next(lang);
    this.http.get(`assets/i18n/${lang}.json`).subscribe({
      next: (data) => {
        this.translationsSubject.next(data);
      },
      error: (err) => console.error('Failed to load translations', err)
    });
  }

  get currentLang(): string {
    return this.currentLangSubject.value;
  }
}
