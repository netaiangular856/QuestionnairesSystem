import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EmployeesApiService } from '../../services/employees-api.service';
import { DepartmentsApiService } from '../../services/departments-api.service';
import { EmployeeListItemDto, EmployeeFilterRequest, CreateEmployeeRequest, UpdateEmployeeRequest, EmployeeDto } from '../../shared/models/employees.models';
import { DepartmentListItemDto } from '../../shared/models/department.models';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';
import { ToastService } from '../../core/services/toast.service';
import { PagedResult } from '../../shared/models/api.types';

type ViewMode = 'table' | 'cards';

@Component({
  selector: 'app-employees-list',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './employees-list.component.html',
})
export class EmployeesListComponent implements OnInit {
  private readonly api = inject(EmployeesApiService);
  private readonly departmentsApi = inject(DepartmentsApiService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(I18nService);

  readonly result = signal<PagedResult<EmployeeListItemDto> | null>(null);
  readonly departmentOptions = signal<DepartmentListItemDto[]>([]);
  readonly loading = signal(false);
  readonly viewMode = signal<ViewMode>('table');

  filter: EmployeeFilterRequest = {
    search: '',
    page: 1,
    pageSize: 10,
  };

  readonly createOpen = signal(false);
  readonly createBusy = signal(false);
  createModel: CreateEmployeeRequest = this.emptyCreateModel();

  readonly editOpen = signal(false);
  readonly editBusy = signal(false);
  readonly editId = signal<string | null>(null);
  editModel: UpdateEmployeeRequest = this.emptyUpdateModel();

  ngOnInit(): void {
    this.load();
    this.loadDepartments();
  }

  private loadDepartments(): void {
    this.departmentsApi
      .getPagedList({ search: null, parentDepartmentId: null, page: 1, pageSize: 500 })
      .subscribe({
        next: (r) => this.departmentOptions.set([...r.items]),
        error: () => this.departmentOptions.set([]),
      });
  }

  load(): void {
    this.loading.set(true);
    this.api.getPagedList(this.filter).subscribe({
      next: (res) => {
        this.result.set(res);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.show('Error loading employees', 'error');
      },
    });
  }

  onSearch(): void {
    this.filter.page = 1;
    this.load();
  }

  nextPage(): void {
    const r = this.result();
    if (!r || !r.hasNextPage) return;
    this.filter.page++;
    this.load();
  }

  prevPage(): void {
    if (this.filter.page > 1) {
      this.filter.page--;
      this.load();
    }
  }

  setViewMode(mode: ViewMode): void {
    this.viewMode.set(mode);
  }

  openCreate(): void {
    this.createModel = this.emptyCreateModel();
    this.createOpen.set(true);
  }

  saveCreate(): void {
    if (!this.createModel.employeeNumber || !this.createModel.nameAr || !this.createModel.nameEn) {
      this.toast.show(this.i18n.t('users.toast.createRequired'), 'error');
      return;
    }

    this.createBusy.set(true);
    this.api.create(this.createModel).subscribe({
      next: () => {
        this.createBusy.set(false);
        this.createOpen.set(false);
        this.toast.show(this.i18n.t('employees.toast.created'), 'success');
        this.load();
      },
      error: () => {
        this.createBusy.set(false);
        this.toast.show('Failed to create employee', 'error');
      },
    });
  }

  openEdit(id: string): void {
    this.editId.set(id);
    this.editBusy.set(true);
    this.editOpen.set(true);
    this.api.getById(id).subscribe({
      next: (e) => {
        this.editModel = {
          employeeNumber: e.employeeNumber,
          nameAr: e.nameAr,
          nameEn: e.nameEn,
          email: e.email,
          phoneNumber: e.phoneNumber,
          jobTitleAr: e.jobTitleAr,
          jobTitleEn: e.jobTitleEn,
          departmentId: e.departmentId,
          isActive: e.isActive,
        };
        this.editBusy.set(false);
      },
      error: () => {
        this.editBusy.set(false);
        this.editOpen.set(false);
        this.toast.show('Failed to load employee', 'error');
      },
    });
  }

  saveEdit(): void {
    const id = this.editId();
    if (!id) return;

    this.editBusy.set(true);
    this.api.update(id, this.editModel).subscribe({
      next: () => {
        this.editBusy.set(false);
        this.editOpen.set(false);
        this.toast.show(this.i18n.t('employees.toast.updated'), 'success');
        this.load();
      },
      error: () => {
        this.editBusy.set(false);
        this.toast.show('Failed to update employee', 'error');
      },
    });
  }

  private emptyCreateModel(): CreateEmployeeRequest {
    return {
      employeeNumber: '',
      nameAr: '',
      nameEn: '',
      email: '',
      phoneNumber: '',
      jobTitleAr: '',
      jobTitleEn: '',
      departmentId: null,
    };
  }

  private emptyUpdateModel(): UpdateEmployeeRequest {
    return {
      ...this.emptyCreateModel(),
      isActive: true,
    };
  }
}
