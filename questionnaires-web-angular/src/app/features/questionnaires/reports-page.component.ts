import { Component, OnInit, inject, signal } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import { ReportsApiService } from '../../services/reports-api.service';
import { DashboardReportDto } from '../../shared/models/questionnaire.models';
import { PermissionCodes } from '../../shared/models/permission-codes';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-reports-page',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './reports-page.component.html',
  styleUrl: './reports-page.component.scss',
})
export class ReportsPageComponent implements OnInit {
  private readonly api = inject(ReportsApiService);
  readonly auth = inject(AuthService);

  readonly canExport = this.auth.hasPermission(PermissionCodes.ReportExport);

  readonly data = signal<DashboardReportDto | null>(null);
  readonly failed = signal(false);
  readonly busy = signal(true);

  ngOnInit(): void {
    this.api.getDashboard().subscribe({
      next: (d) => {
        this.data.set(d);
        this.busy.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.busy.set(false);
      },
    });
  }
}
