import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';

@Component({
  selector: 'app-change-password-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './change-password-page.component.html',
  styleUrl: './change-password-page.component.scss',
})
export class ChangePasswordPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(I18nService);

  readonly busy = signal(false);

  readonly pwdForm = this.fb.nonNullable.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
  });

  session(): ReturnType<AuthService['sessionSnapshot']> {
    return this.auth.sessionSnapshot();
  }

  submitPassword(): void {
    const s = this.session();
    const email = s?.email;
    if (!email) {
      this.toast.show(this.i18n.t('profile.passwordFailed'), 'error');
      return;
    }
    if (this.pwdForm.invalid) {
      this.pwdForm.markAllAsTouched();
      this.toast.show(this.i18n.t('profile.passwordRequired'), 'error');
      return;
    }
    this.busy.set(true);
    const v = this.pwdForm.getRawValue();
    this.auth
      .changePassword({
        email,
        currentPassword: v.currentPassword,
        newPassword: v.newPassword,
      })
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.pwdForm.reset();
          this.toast.show(this.i18n.t('profile.passwordSaved'), 'success');
        },
        error: () => {
          this.busy.set(false);
          this.toast.show(this.i18n.t('profile.passwordFailed'), 'error');
        },
      });
  }
}
