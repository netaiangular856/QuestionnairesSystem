import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PartnersApiService } from '../../services/partners-api.service';
import { PartnerListItemDto, PartnerFilterRequest, PartnerType, CreatePartnerRequest, UpdatePartnerRequest } from '../../shared/models/partners.models';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';
import { ToastService } from '../../core/services/toast.service';
import { PagedResult } from '../../shared/models/api.types';

type ViewMode = 'table' | 'cards';

@Component({
  selector: 'app-partners-list',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './partners-list.component.html',
})
export class PartnersListComponent implements OnInit {
  private readonly api = inject(PartnersApiService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(I18nService);

  readonly result = signal<PagedResult<PartnerListItemDto> | null>(null);
  readonly loading = signal(false);
  readonly viewMode = signal<ViewMode>('table');

  filter: PartnerFilterRequest = {
    search: '',
    page: 1,
    pageSize: 10,
  };

  readonly createOpen = signal(false);
  readonly createBusy = signal(false);
  createModel: CreatePartnerRequest = this.emptyCreateModel();

  readonly editOpen = signal(false);
  readonly editBusy = signal(false);
  readonly editId = signal<string | null>(null);
  editModel: UpdatePartnerRequest = this.emptyUpdateModel();

  readonly PartnerType = PartnerType;
  readonly partnerTypes = [
    { value: PartnerType.Dealer, labelKey: 'partners.type.dealer' },
    { value: PartnerType.Partner, labelKey: 'partners.type.partner' },
    { value: PartnerType.Vendor, labelKey: 'partners.type.vendor' },
    { value: PartnerType.Customer, labelKey: 'partners.type.customer' },
    { value: PartnerType.Other, labelKey: 'partners.type.other' },
  ];

  ngOnInit(): void {
    this.load();
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
        this.toast.show('Error loading partners', 'error');
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

  getTypeLabel(type: PartnerType): string {
    const found = this.partnerTypes.find(t => t.value === type);
    return found ? found.labelKey : 'partners.type.other';
  }

  openCreate(): void {
    this.createModel = this.emptyCreateModel();
    this.createOpen.set(true);
  }

  saveCreate(): void {
    if (!this.createModel.code || !this.createModel.nameAr || !this.createModel.nameEn) {
      this.toast.show(this.i18n.t('users.toast.createRequired'), 'error');
      return;
    }

    this.createBusy.set(true);
    this.api.create(this.createModel).subscribe({
      next: () => {
        this.createBusy.set(false);
        this.createOpen.set(false);
        this.toast.show(this.i18n.t('partners.toast.created'), 'success');
        this.load();
      },
      error: () => {
        this.createBusy.set(false);
        this.toast.show('Failed to create partner', 'error');
      },
    });
  }

  openEdit(id: string): void {
    this.editId.set(id);
    this.editBusy.set(true);
    this.editOpen.set(true);
    this.api.getById(id).subscribe({
      next: (p) => {
        this.editModel = {
          code: p.code,
          nameAr: p.nameAr,
          nameEn: p.nameEn,
          type: p.type,
          email: p.email,
          phoneNumber: p.phoneNumber,
          contactPerson: p.contactPerson,
          address: p.address,
          isActive: p.isActive,
        };
        this.editBusy.set(false);
      },
      error: () => {
        this.editBusy.set(false);
        this.editOpen.set(false);
        this.toast.show('Failed to load partner', 'error');
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
        this.toast.show(this.i18n.t('partners.toast.updated'), 'success');
        this.load();
      },
      error: () => {
        this.editBusy.set(false);
        this.toast.show('Failed to update partner', 'error');
      },
    });
  }

  private emptyCreateModel(): CreatePartnerRequest {
    return {
      code: '',
      nameAr: '',
      nameEn: '',
      type: PartnerType.Dealer,
      email: '',
      phoneNumber: '',
      contactPerson: '',
      address: '',
    };
  }

  private emptyUpdateModel(): UpdatePartnerRequest {
    return {
      ...this.emptyCreateModel(),
      isActive: true,
    };
  }
}
