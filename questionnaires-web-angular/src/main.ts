import { registerLocaleData } from '@angular/common';
import localeArSA from '@angular/common/locales/ar-SA';
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { I18nService } from './app/shared/services/i18n.service';

/** Required when using DatePipe with locale `ar-SA` (e.g. recommendations page). */
registerLocaleData(localeArSA, 'ar-SA');

bootstrapApplication(AppComponent, appConfig)
  .then((ref) => {
    ref.injector.get(I18nService).applyDocumentLang();
  })
  .catch((err) => console.error(err));
