import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { I18nService } from './app/shared/services/i18n.service';

bootstrapApplication(AppComponent, appConfig)
  .then((ref) => {
    ref.injector.get(I18nService).applyDocumentLang();
  })
  .catch((err) => console.error(err));
