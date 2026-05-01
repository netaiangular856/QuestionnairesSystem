import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ReportsApiService } from '../../services/reports-api.service';
import { DashboardReportDto } from '../../shared/models/questionnaire.models';
import { PermissionCodes } from '../../shared/models/permission-codes';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [TranslatePipe, RouterLink],
  template: `
    <div class="page-workspace">
      <div class="page-head">
        <h1>{{ 'dashboard.title' | t }}</h1>
        <p class="page-head__sub">{{ 'dashboard.lead' | t }}</p>
      </div>

      @if (canReport()) {
        @if (reportBusy()) {
          <p class="dash-muted">{{ 'common.loading' | t }}</p>
        } @else if (report()) {
          <div class="dash-quick">
            <a [routerLink]="surveysLink()" class="dash-tile dash-tile--a">
              <span class="dash-tile__v">{{ report()!.totalSurveys }}</span>
              <span class="dash-tile__l">{{ 'q.reports.totalSurveys' | t }}</span>
            </a>
            <a [routerLink]="surveysLink()" class="dash-tile dash-tile--b">
              <span class="dash-tile__v">{{ report()!.publishedSurveys }}</span>
              <span class="dash-tile__l">{{ 'q.reports.published' | t }}</span>
            </a>
            <a routerLink="/reports" class="dash-tile dash-tile--c">
              <span class="dash-tile__v">{{ report()!.totalResponses }}</span>
              <span class="dash-tile__l">{{ 'q.reports.responses' | t }}</span>
            </a>
            <a [routerLink]="plansLink()" class="dash-tile dash-tile--d">
              <span class="dash-tile__v">{{ report()!.openActionPlans }}</span>
              <span class="dash-tile__l">{{ 'q.reports.openPlans' | t }}</span>
            </a>
          </div>
        }
      }
    </div>
  `,
  styles: [
    `
      .dash-muted {
        color: var(--color-muted);
      }
      .dash-quick {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
        gap: 0.85rem;
        margin-top: 1.25rem;
      }
      .dash-tile {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        padding: 1rem 1.05rem;
        text-decoration: none;
        color: inherit;
        border: 2px solid #1c1917;
        background: #fffef8;
        transition: transform 0.15s var(--ease-out, ease);
      }
      .dash-tile:hover {
        transform: translate(-2px, -2px);
      }
      .dash-tile__v {
        font-size: 1.65rem;
        font-weight: 900;
        letter-spacing: -0.03em;
        line-height: 1;
      }
      .dash-tile__l {
        font-size: 0.78rem;
        font-weight: 700;
        color: #57534e;
        text-transform: uppercase;
        letter-spacing: 0.04em;
      }
      .dash-tile--a {
        box-shadow: 4px 4px 0 #1c1917;
      }
      .dash-tile--b {
        background: #e0e7ff;
        border-color: #4338ca;
        box-shadow: 4px 4px 0 #4338ca;
      }
      .dash-tile--c {
        border-radius: 14px;
        background: #ecfccb;
        border-color: #3f6212;
        box-shadow: 0 6px 0 #3f6212;
      }
      .dash-tile--d {
        background: linear-gradient(145deg, #cffafe, #fff);
        border-style: dashed;
        border-color: #0e7490;
      }
    `,
  ],
})
export class DashboardPageComponent implements OnInit {
  private readonly reports = inject(ReportsApiService);
  private readonly auth = inject(AuthService);

  readonly canReport = signal(this.auth.hasPermission(PermissionCodes.ReportView));
  readonly canSurveyView = signal(this.auth.hasPermission(PermissionCodes.SurveyView));
  readonly canPlansView = signal(this.auth.hasPermission(PermissionCodes.ActionPlanView));
  readonly report = signal<DashboardReportDto | null>(null);
  readonly reportBusy = signal(false);

  surveysLink(): string {
    return this.canSurveyView() ? '/surveys' : '/reports';
  }

  plansLink(): string {
    return this.canPlansView() ? '/action-plans' : '/reports';
  }

  ngOnInit(): void {
    if (!this.canReport()) return;
    this.reportBusy.set(true);
    this.reports.getDashboard().subscribe({
      next: (d) => {
        this.report.set(d);
        this.reportBusy.set(false);
      },
      error: () => this.reportBusy.set(false),
    });
  }
}
