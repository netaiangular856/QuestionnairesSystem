import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from '../../core/auth/auth.service';
import { PublicSurveyApiService } from '../../services/public-survey-api.service';
import { PublicSurveyListItemDto } from '../../shared/models/questionnaire.models';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';

@Component({
  selector: 'app-portal-public-page',
  standalone: true,
  imports: [RouterLink, TranslatePipe, DatePipe],
  templateUrl: './portal-public-page.component.html',
  styleUrl: './portal-public-page.component.scss',
})
export class PortalPublicPageComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);
  private readonly api = inject(PublicSurveyApiService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  catalogBusy = true;
  catalog: PublicSurveyListItemDto[] = [];

  ngOnInit(): void {
    queueMicrotask(() => this.scrollToFragmentFromUrl());
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.scrollToFragmentFromUrl());

    this.api.listCatalog(1, 50).subscribe({
      next: (p) => {
        this.catalog = [...(p.items ?? [])];
        this.catalogBusy = false;
      },
      error: () => {
        this.catalog = [];
        this.catalogBusy = false;
      },
    });
  }

  isLoggedIn(): boolean {
    return this.auth.isAuthenticated();
  }

  toggleLang(): void {
    this.i18n.toggleLang();
  }

  jumpToSection(fragment: string, event: Event): void {
    if (!this.isOnPortalPath()) return;
    event.preventDefault();
    void this.router.navigate(['/portal'], { fragment, replaceUrl: true }).then(() => this.scrollToId(fragment));
  }

  private isOnPortalPath(): boolean {
    const path = this.router.url.split(/[?#]/)[0];
    return path === '/portal';
  }

  private scrollToFragmentFromUrl(): void {
    const fragment = this.router.parseUrl(this.router.url).fragment;
    if (fragment) queueMicrotask(() => this.scrollToId(fragment));
  }

  private scrollToId(id: string): void {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  surveyTitle(s: PublicSurveyListItemDto): string {
    return this.i18n.lang() === 'ar' ? s.titleAr : s.titleEn;
  }

  surveyDesc(s: PublicSurveyListItemDto): string | null {
    const d = this.i18n.lang() === 'ar' ? s.descriptionAr : s.descriptionEn;
    return d?.trim() || null;
  }
}
