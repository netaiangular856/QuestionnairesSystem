import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-forbidden-page',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  template: `
    <div class="wrap card">
      <h1>{{ 'forbidden.title' | t }}</h1>
      <p>{{ 'forbidden.lead' | t }}</p>
      <a routerLink="/dashboard" class="btn-primary link">{{ 'forbidden.back' | t }}</a>
    </div>
  `,
  styles: [
    `
      .wrap {
        max-width: 520px;
        margin: 2rem auto;
        padding: 1.5rem;
        text-align: center;
      }
      .link {
        display: inline-block;
        margin-top: 1rem;
        text-decoration: none;
        padding: 0.5rem 1rem;
        border-radius: var(--radius-md, 12px);
      }
    `,
  ],
})
export class ForbiddenPageComponent {}
