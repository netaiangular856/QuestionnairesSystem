import { Injectable, signal } from '@angular/core';
import { AppLang, TRANSLATIONS } from '../i18n/translations';

@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly currentLang = signal<AppLang>('ar');

  lang(): AppLang {
    return this.currentLang();
  }

  isRtl(): boolean {
    return this.currentLang() === 'ar';
  }

  /** Call once on app bootstrap so document matches default language. */
  applyDocumentLang(): void {
    const lang = this.currentLang();
    document.documentElement.lang = lang;
    document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
  }

  toggleLang(): void {
    const next = this.currentLang() === 'ar' ? 'en' : 'ar';
    this.currentLang.set(next);
    document.documentElement.lang = next;
    document.documentElement.dir = next === 'ar' ? 'rtl' : 'ltr';
  }

  t(key: string): string {
    return TRANSLATIONS[this.currentLang()][key] ?? key;
  }
}
