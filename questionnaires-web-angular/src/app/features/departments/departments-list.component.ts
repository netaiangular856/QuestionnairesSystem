import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  effect,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DepartmentsApiService } from '../../services/departments-api.service';
import {
  DepartmentListItemDto,
  DepartmentTreeNodeDto,
  DepartmentFilterRequest,
  CreateDepartmentRequest,
  UpdateDepartmentRequest,
} from '../../shared/models/department.models';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';
import { ToastService } from '../../core/services/toast.service';
import { PagedResult } from '../../shared/models/api.types';

// d3-org-chart ships JS only; types are declared locally in d3-org-chart.d.ts
import { OrgChart } from 'd3-org-chart';

type ViewMode = 'table' | 'cards' | 'tree';

const SYNTHETIC_ROOT_ID = '__synthetic_root__';

@Component({
  selector: 'app-departments-list',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './departments-list.component.html',
  styleUrl: './departments-list.component.scss',
})
export class DepartmentsListComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly api = inject(DepartmentsApiService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(I18nService);

  readonly viewMode = signal<ViewMode>('cards');
  readonly loading = signal(false);
  readonly result = signal<PagedResult<DepartmentListItemDto> | null>(null);
  readonly treeData = signal<readonly DepartmentTreeNodeDto[]>([]);
  readonly allDepartments = signal<readonly DepartmentListItemDto[]>([]);
  
  // Tree Controls State
  readonly orientation = signal<'vertical' | 'horizontal'>('vertical');
  readonly isCompact = signal(false);
  readonly isFullscreen = signal(false);
  @ViewChild('orgChartContainer') private orgChartContainer?: ElementRef<HTMLDivElement>;
  private chart?: OrgChart<OrgDeptNode>;
  private containerClickHandler?: (e: MouseEvent) => void;
  private clickHandlerHost?: HTMLDivElement;
  private resizeObserver?: ResizeObserver;

  private readonly renderEffect = effect(() => {
    // Re-render when any of these change (excludes fullscreen — it only resizes)
    const mode = this.viewMode();
    const data = this.treeData();
    const _lang = this.i18n.lang();
    const _orientation = this.orientation();
    const _compact = this.isCompact();

    if (mode !== 'tree') return;
    if (!data?.length) return;

    // Defer to ensure @ViewChild is populated when switching from another view mode
    queueMicrotask(() => {
      if (this.viewMode() !== 'tree') return;
      if (!this.orgChartContainer?.nativeElement) return;
      this.renderOrgChart();
    });
  });

  // Tree Control Actions
  zoomIn() { (this.chart as any)?.zoomIn?.(); }
  zoomOut() { (this.chart as any)?.zoomOut?.(); }
  resetZoom() { (this.chart as any)?.fit?.(); }
  
  toggleOrientation() {
    this.orientation.update(o => o === 'vertical' ? 'horizontal' : 'vertical');
  }

  toggleFullscreen() {
    this.isFullscreen.update(f => !f);
  }

  expandAll() {
    (this.chart as any)?.expandAll?.();
  }

  collapseAll() {
    (this.chart as any)?.collapseAll?.();
  }

  filter: DepartmentFilterRequest = {
    search: '',
    page: 1,
    pageSize: 12,
  };

  readonly createOpen = signal(false);
  readonly createBusy = signal(false);
  createModel: CreateDepartmentRequest = this.emptyCreateModel();

  readonly editOpen = signal(false);
  readonly editBusy = signal(false);
  readonly editId = signal<string | null>(null);
  editModel: UpdateDepartmentRequest = this.emptyUpdateModel();

  ngOnInit(): void {
    this.load();
    this.api.getTree().subscribe((data) => {
      this.treeData.set(data);
    });
    this.api.getPagedList({ page: 1, pageSize: 1000 }).subscribe((res) => this.allDepartments.set(res.items));
  }

  ngAfterViewInit(): void {
    // Resize-aware fit when container size changes (fullscreen / responsive)
    const el = this.orgChartContainer?.nativeElement;
    if (el) {
      this.resizeObserver = new ResizeObserver(() => {
        if (this.viewMode() === 'tree') (this.chart as any)?.fit?.();
      });
      this.resizeObserver.observe(el);
    }
  }

  ngOnDestroy(): void {
    this.renderEffect.destroy();
    this.resizeObserver?.disconnect();
    this.detachContainerClickHandler();
  }

  load(): void {
    if (this.viewMode() === 'tree') {
      this.loadTree();
    } else {
      this.loadPaged();
    }
  }

  loadPaged(): void {
    this.loading.set(true);
    this.api.getPagedList(this.filter).subscribe({
      next: (res) => {
        this.result.set(res);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.show('Error loading departments', 'error');
      },
    });
  }

  loadTree(): void {
    this.loading.set(true);
    this.api.getTree().subscribe({
      next: (res) => {
        this.treeData.set(res);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.show('Error loading department tree', 'error');
      },
    });
  }

  setViewMode(mode: ViewMode): void {
    this.viewMode.set(mode);
    this.load();
  }

  onSearch(): void {
    this.filter.page = 1;
    this.load();
  }

  nextPage(): void {
    const r = this.result();
    if (!r || !r.hasNextPage) return;
    this.filter.page++;
    this.loadPaged();
  }

  prevPage(): void {
    if (this.filter.page > 1) {
      this.filter.page--;
      this.loadPaged();
    }
  }

  openCreate(parentId?: string): void {
    this.createModel = this.emptyCreateModel();
    if (parentId) {
      this.createModel.parentDepartmentId = parentId;
    }
    this.createOpen.set(true);
  }

  saveCreate(): void {
    if (!this.createModel.code || !this.createModel.nameAr || !this.createModel.nameEn) {
      this.toast.show('Please fill required fields', 'error');
      return;
    }

    this.createBusy.set(true);
    this.api.create(this.createModel).subscribe({
      next: () => {
        this.createBusy.set(false);
        this.createOpen.set(false);
        this.toast.show('Department created successfully', 'success');
        this.load();
      },
      error: (err) => {
        this.createBusy.set(false);
        this.toast.show(err.error?.message || 'Error creating department', 'error');
      },
    });
  }

  openEdit(id: string): void {
    this.editId.set(id);
    this.api.getById(id).subscribe({
      next: (dept) => {
        this.editModel = {
          code: dept.code,
          nameAr: dept.nameAr,
          nameEn: dept.nameEn,
          parentDepartmentId: dept.parentDepartmentId,
        };
        this.editOpen.set(true);
      },
      error: () => this.toast.show('Error loading department details', 'error'),
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
        this.toast.show('Department updated successfully', 'success');
        this.load();
      },
      error: (err) => {
        this.editBusy.set(false);
        this.toast.show(err.error?.message || 'Error updating department', 'error');
      },
    });
  }

  deleteDept(id: string): void {
    if (!confirm('Are you sure you want to delete this department?')) return;

    this.api.delete(id).subscribe({
      next: () => {
        this.toast.show('Department deleted successfully', 'success');
        this.load();
      },
      error: (err) => {
        this.toast.show(err.error?.message || 'Error deleting department', 'error');
      },
    });
  }

  private emptyCreateModel(): CreateDepartmentRequest {
    return { code: '', nameAr: '', nameEn: '', parentDepartmentId: null };
  }

  private emptyUpdateModel(): UpdateDepartmentRequest {
    return { code: '', nameAr: '', nameEn: '', parentDepartmentId: null };
  }

  private renderOrgChart(): void {
    const container = this.orgChartContainer?.nativeElement;
    if (!container) return;

    // Ensure clean container (d3-org-chart appends SVG)
    container.innerHTML = '';

    const flatData = this.flattenTree(this.treeData());
    if (!flatData.length) return;

    // Recreate chart each render — d3-org-chart holds DOM refs internally,
    // so recreating after innerHTML clear keeps state consistent.
    this.chart = new OrgChart<OrgDeptNode>();

    const isRtl = this.i18n.isRtl();
    const layout = this.orientation() === 'vertical' ? 'top' : 'left';

    try {
      (this.chart as any)
        .container(container)
        .data(flatData as any)
        .layout(layout)
        .compact(this.isCompact())
        .nodeWidth(() => (this.isCompact() ? 200 : 300))
        .nodeHeight(() => (this.isCompact() ? 70 : 100))
        .childrenMargin(() => (this.isCompact() ? 50 : 70))
        .siblingsMargin(() => (this.isCompact() ? 18 : 28))
        .neighbourMargin(() => (this.isCompact() ? 14 : 22))
        .nodeContent((d: any) => this.nodeHtml(d?.data as OrgDeptNode, isRtl))
        .render();

      // Fit screen after first render (defer so DOM stabilizes)
      setTimeout(() => {
        try { (this.chart as any)?.fit?.(); } catch { /* noop */ }
      }, 80);
    } catch (err) {
      console.error('[d3-org-chart] render failed:', err, { flatData });
    }

    this.attachContainerClickHandler(container);
  }

  private nodeHtml(node: OrgDeptNode, isRtl: boolean): string {
    const name = this.i18n.lang() === 'ar' ? node.nameAr : node.nameEn;
    const titleCreate = this.i18n.t('departments.create');
    const titleEdit = this.i18n.t('common.edit');
    const employeesLabel = this.i18n.t('employees.title');

    const isRoot = node.parentId == null;
    const isSynthetic = node.id === SYNTHETIC_ROOT_ID;

    const initial = (name || '?').trim().charAt(0).toUpperCase();

    const actionsHtml = isSynthetic
      ? `<div class="org-node-card__actions">
           <button type="button" class="org-node-btn js-org-create" data-id="" title="${this.escapeAttr(titleCreate)}" aria-label="${this.escapeAttr(titleCreate)}">
             <svg viewBox="0 0 24 24" width="14" height="14" aria-hidden="true"><path d="M12 5v14M5 12h14" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"/></svg>
           </button>
         </div>`
      : `<div class="org-node-card__actions">
           <button type="button" class="org-node-btn js-org-create" data-id="${this.escapeAttr(node.id)}" title="${this.escapeAttr(titleCreate)}" aria-label="${this.escapeAttr(titleCreate)}">
             <svg viewBox="0 0 24 24" width="14" height="14" aria-hidden="true"><path d="M12 5v14M5 12h14" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"/></svg>
           </button>
           <button type="button" class="org-node-btn js-org-edit" data-id="${this.escapeAttr(node.id)}" title="${this.escapeAttr(titleEdit)}" aria-label="${this.escapeAttr(titleEdit)}">
             <svg viewBox="0 0 24 24" width="14" height="14" aria-hidden="true"><path d="M12 20h9M16.5 3.5a2.121 2.121 0 1 1 3 3L7 19l-4 1 1-4 12.5-12.5z" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
           </button>
         </div>`;

    const avatarHtml = isSynthetic
      ? `<svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
           <path d="M3 21V8l9-5 9 5v13M9 21V12h6v9" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
         </svg>`
      : `<span class="org-node-card__initial">${this.escapeHtml(initial)}</span>`;

    const employeesBadge = !isSynthetic && node.employeeCount > 0
      ? `<span class="org-node-card__badge">
           <svg viewBox="0 0 24 24" width="11" height="11" aria-hidden="true"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/></svg>
           ${node.employeeCount} <span class="org-node-card__badge-label">${this.escapeHtml(employeesLabel)}</span>
         </span>`
      : '';

    return `
      <div class="org-node-card ${isRoot ? 'org-node-card--root' : ''} ${isSynthetic ? 'org-node-card--synthetic' : ''}" dir="${isRtl ? 'rtl' : 'ltr'}">
        <div class="org-node-card__avatar">${avatarHtml}</div>
        <div class="org-node-card__content">
          <div class="org-node-card__name" title="${this.escapeAttr(name)}">${this.escapeHtml(name)}</div>
          <div class="org-node-card__meta">
            ${node.code ? `<span class="org-node-card__code">${this.escapeHtml(node.code)}</span>` : ''}
            ${employeesBadge}
          </div>
        </div>
        ${actionsHtml}
      </div>
    `;
  }

  private flattenTree(nodes: readonly DepartmentTreeNodeDto[]): OrgDeptNode[] {
    const out: OrgDeptNode[] = [];
    const walk = (n: DepartmentTreeNodeDto, parentId: string | null) => {
      out.push({
        id: n.id,
        parentId,
        code: n.code,
        nameAr: n.nameAr,
        nameEn: n.nameEn,
        employeeCount: n.employeeCount ?? 0,
      });
      n.children?.forEach((c) => walk(c, n.id));
    };

    if (nodes.length === 0) return out;

    if (nodes.length === 1) {
      walk(nodes[0], null);
      return out;
    }

    // Multiple top-level departments — d3.stratify requires a single root.
    // Inject a synthetic "Organization" parent so the whole forest renders.
    out.push({
      id: SYNTHETIC_ROOT_ID,
      parentId: null,
      code: '',
      nameAr: this.i18n.t('nav.departments') || 'الأقسام',
      nameEn: this.i18n.t('nav.departments') || 'Departments',
      employeeCount: nodes.reduce((sum, n) => sum + (n.employeeCount ?? 0), 0),
    });
    nodes.forEach((n) => walk(n, SYNTHETIC_ROOT_ID));
    return out;
  }

  private attachContainerClickHandler(container: HTMLDivElement): void {
    // Always (re)attach when the container element changes (e.g., after view toggle)
    if (this.containerClickHandler && this.clickHandlerHost === container) return;

    this.detachContainerClickHandler();

    this.containerClickHandler = (e: MouseEvent) => {
      const target = e.target as Element | null;
      if (!target || typeof target.closest !== 'function') return;

      const createBtn = target.closest('.js-org-create') as HTMLElement | null;
      const editBtn = target.closest('.js-org-edit') as HTMLElement | null;
      const btn = createBtn || editBtn;
      if (!btn) return;

      // Stop d3-org-chart's own node-click (expand/collapse) from firing
      e.preventDefault();
      e.stopPropagation();
      e.stopImmediatePropagation();

      const id = btn.getAttribute('data-id') ?? '';
      console.debug('[org-chart] action click', { kind: createBtn ? 'create' : 'edit', id });

      if (createBtn) {
        // Empty id (synthetic root) → create top-level department
        this.openCreate(id || undefined);
      } else if (editBtn) {
        if (!id) return;
        this.openEdit(id);
      }
    };

    // Capture phase so we run before d3's bubbling handler.
    container.addEventListener('click', this.containerClickHandler, true);
    // Pointerdown captured too — some browsers fire pointerdown handlers before click
    // and d3-zoom may consume the click otherwise.
    container.addEventListener('pointerdown', this.onPointerDownCapture, true);
    this.clickHandlerHost = container;
  }

  private detachContainerClickHandler(): void {
    if (this.clickHandlerHost && this.containerClickHandler) {
      this.clickHandlerHost.removeEventListener('click', this.containerClickHandler, true);
      this.clickHandlerHost.removeEventListener('pointerdown', this.onPointerDownCapture, true);
    }
    this.containerClickHandler = undefined;
    this.clickHandlerHost = undefined;
  }

  // Prevent d3-zoom's pan from starting when pressing on action buttons
  private readonly onPointerDownCapture = (e: PointerEvent) => {
    const target = e.target as Element | null;
    if (!target || typeof target.closest !== 'function') return;
    if (target.closest('.org-node-btn')) {
      e.stopPropagation();
    }
  };

  private escapeHtml(value: string): string {
    return (value ?? '')
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#039;');
  }

  private escapeAttr(value: string): string {
    return this.escapeHtml(value);
  }
}

type OrgDeptNode = {
  id: string;
  parentId: string | null;
  code: string;
  nameAr: string;
  nameEn: string;
  employeeCount: number;
};
