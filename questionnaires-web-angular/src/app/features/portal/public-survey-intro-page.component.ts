import { Component, OnInit, inject, signal } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PublicSurveyApiService } from '../../services/public-survey-api.service';
import { PublicSurveyPageDto } from '../../shared/models/questionnaire.models';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';

@Component({
  selector: 'app-public-survey-intro-page',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './public-survey-intro-page.component.html',
  styleUrl: './public-survey-intro-page.component.scss',
})
export class PublicSurveyIntroPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(PublicSurveyApiService);
  private readonly sanitizer = inject(DomSanitizer);
  readonly i18n = inject(I18nService);

  readonly busy = signal(true);
  readonly survey = signal<PublicSurveyPageDto | null>(null);
  readonly articleHtml = signal<SafeHtml | null>(null);

  ngOnInit(): void {
    const code = this.route.snapshot.paramMap.get('code')?.trim();
    if (!code) {
      void this.router.navigate(['/portal']);
      return;
    }

    this.api.getByCode(code).subscribe({
      next: (s) => {
        this.survey.set(s);
        const ar = this.i18n.lang() === 'ar';
        const bodyRaw = (ar ? s.publicArticleBodyAr : s.publicArticleBodyEn)?.trim();
        const hasArticle = s.publicArticleEnabled && !!bodyRaw;

        if (!hasArticle) {
          void this.router.navigate(['/portal', 's', code, 'fill'], { replaceUrl: true });
          return;
        }

        this.setArticleHtml(bodyRaw ?? '');
        this.busy.set(false);
      },
      error: () => {
        this.busy.set(false);
        void this.router.navigate(['/portal']);
      },
    });
  }

  articleTitle(s: PublicSurveyPageDto): string {
    const ar = this.i18n.lang() === 'ar';
    const t = (ar ? s.publicArticleTitleAr : s.publicArticleTitleEn)?.trim();
    return t || (ar ? s.titleAr : s.titleEn);
  }

  surveyDisplayTitle(s: PublicSurveyPageDto): string {
    return this.i18n.lang() === 'ar' ? s.titleAr : s.titleEn;
  }

  toggleLang(): void {
    this.i18n.toggleLang();
    const s = this.survey();
    if (!s) return;
    const ar = this.i18n.lang() === 'ar';
    const bodyRaw = (ar ? s.publicArticleBodyAr : s.publicArticleBodyEn)?.trim();
    this.setArticleHtml(bodyRaw ?? '');
  }

  private setArticleHtml(html: string): void {
    this.articleHtml.set(this.sanitizer.bypassSecurityTrustHtml(html));
  }
}
