import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from '../../core/auth/auth.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';

@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './landing-page.component.html',
  styleUrl: './landing-page.component.scss',
})
export class LandingPageComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    queueMicrotask(() => this.scrollToFragmentFromUrl());
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.scrollToFragmentFromUrl());
  }

  isLoggedIn(): boolean {
    return this.auth.isAuthenticated();
  }

  toggleLang(): void {
    this.i18n.toggleLang();
  }

  /** When already on /home, Router may not scroll — handle explicitly. */
  jumpToSection(fragment: string, event: Event): void {
    if (!this.isOnHomePath()) return;
    event.preventDefault();
    void this.router.navigate(['/home'], { fragment, replaceUrl: true }).then(() => this.scrollToId(fragment));
  }

  private isOnHomePath(): boolean {
    const path = this.router.url.split(/[?#]/)[0];
    return path === '/home' || path === '';
  }

  private scrollToFragmentFromUrl(): void {
    const fragment = this.router.parseUrl(this.router.url).fragment;
    if (fragment) queueMicrotask(() => this.scrollToId(fragment));
  }

  private scrollToId(id: string): void {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
}
