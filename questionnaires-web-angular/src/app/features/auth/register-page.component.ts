import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ApiBusinessError } from '../../shared/utils/api-helpers';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';

@Component({
  selector: 'app-register-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './register-page.component.html',
  styleUrl: './register-page.component.scss',
})
export class RegisterPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly i18n = inject(I18nService);

  readonly form = this.fb.nonNullable.group({
    userName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(128)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(256)]],
    nameAr: [''],
    nameEn: [''],
  });

  error: string | null = null;
  busy = false;
  showPassword = false;

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.busy = true;
    this.error = null;
    const v = this.form.getRawValue();
    this.auth
      .register({
        userName: v.userName.trim(),
        email: v.email.trim(),
        password: v.password,
        nameAr: v.nameAr.trim() || null,
        nameEn: v.nameEn.trim() || null,
      })
      .subscribe({
        next: () => void this.router.navigateByUrl('/dashboard'),
        error: (err: unknown) => {
          this.busy = false;
          if (err instanceof ApiBusinessError && err.errors.length) {
            this.error = err.errors.join(' · ');
          } else {
            this.error = this.i18n.t('register.error');
          }
        },
      });
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  toggleLang(): void {
    this.i18n.toggleLang();
  }
}
