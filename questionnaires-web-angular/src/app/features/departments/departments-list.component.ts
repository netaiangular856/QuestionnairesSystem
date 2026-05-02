import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DepartmentsApiService } from '../../services/departments-api.service';
import {
  DepartmentListItemDto,
  DepartmentTreeNodeDto,
  DepartmentFilterRequest,
  CreateDepartmentRequest,
  UpdateDepartmentRequest,
  DepartmentDto,
} from '../../shared/models/department.models';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';
import { ToastService } from '../../core/services/toast.service';
import { PagedResult } from '../../shared/models/api.types';

import { OrganizationChartModule } from 'primeng/organizationchart';
import { TreeNode, PrimeTemplate } from 'primeng/api';

type ViewMode = 'table' | 'cards' | 'tree';

@Component({
  selector: 'app-departments-list',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe, OrganizationChartModule, PrimeTemplate],
  templateUrl: './departments-list.component.html',
  styleUrl: './departments-list.component.scss',
})
export class DepartmentsListComponent implements OnInit {
  private readonly api = inject(DepartmentsApiService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(I18nService);

  readonly viewMode = signal<ViewMode>('cards');
  readonly loading = signal(false);
  readonly result = signal<PagedResult<DepartmentListItemDto> | null>(null);
  readonly treeData = signal<readonly DepartmentTreeNodeDto[]>([]);
  readonly allDepartments = signal<readonly DepartmentListItemDto[]>([]);
  
  // Tree Controls State
  readonly zoom = signal(1);
  readonly orientation = signal<'vertical' | 'horizontal'>('vertical');
  readonly isCompact = signal(false);
  readonly isFullscreen = signal(false);
  readonly expandedNodes = signal<Set<string>>(new Set());

  // Panning state (Transform-based for full freedom)
  readonly translateX = signal(0);
  readonly translateY = signal(0);
  private isDragging = false;
  private startX = 0;
  private startY = 0;
  private initialTranslateX = 0;
  private initialTranslateY = 0;

  readonly orgChartData = computed<TreeNode[]>(() => {
    const data = this.treeData();
    return data.map(node => this.mapToPrimeTreeNode(node));
  });

  private mapToPrimeTreeNode(node: DepartmentTreeNodeDto): TreeNode {
    const label = this.i18n.lang() === 'ar' ? node.nameAr : node.nameEn;
    // If no nodes are in expandedNodes set, default to collapsed (except maybe root)
    // If expandAll was called, all IDs will be in the set.
    const isExpanded = this.expandedNodes().has(node.id); 
    
    return {
      expanded: isExpanded,
      type: 'person',
      label: label,
      data: {
        id: node.id,
        nameAr: node.nameAr,
        nameEn: node.nameEn,
        employeeCount: node.employeeCount,
        code: node.code,
        parentDepartmentId: null // We'll need to know if it's root
      },
      children: node.children?.map(c => this.mapToPrimeTreeNode(c)) || []
    };
  }

  // Tree Control Actions
  zoomIn() { this.zoom.update(z => Math.min(z + 0.1, 2)); }
  zoomOut() { this.zoom.update(z => Math.max(z - 0.1, 0.5)); }
  resetZoom() { 
    this.zoom.set(1);
    this.translateX.set(0);
    this.translateY.set(0);
  }

  onWheel(event: WheelEvent) {
    if (this.viewMode() !== 'tree') return;
    
    // Zoom with Ctrl + Wheel
    if (event.ctrlKey) {
      event.preventDefault();
      if (event.deltaY < 0) {
        this.zoomIn();
      } else {
        this.zoomOut();
      }
    }
  }

  // Panning Handlers (Transform-based for freedom in all directions)
  startDragging(event: MouseEvent) {
    if (this.viewMode() !== 'tree') return;
    const viewport = event.currentTarget as HTMLElement;
    this.isDragging = true;
    viewport.classList.add('is-grabbing');
    
    this.startX = event.pageX;
    this.startY = event.pageY;
    this.initialTranslateX = this.translateX();
    this.initialTranslateY = this.translateY();
  }

  stopDragging(event: MouseEvent) {
    this.isDragging = false;
    const viewport = event.currentTarget as HTMLElement;
    viewport.classList.remove('is-grabbing');
  }

  onDrag(event: MouseEvent) {
    if (!this.isDragging) return;
    event.preventDefault();
    
    const deltaX = (event.pageX - this.startX);
    const deltaY = (event.pageY - this.startY);
    
    this.translateX.set(this.initialTranslateX + deltaX);
    this.translateY.set(this.initialTranslateY + deltaY);
  }
  
  toggleOrientation() {
    this.orientation.update(o => o === 'vertical' ? 'horizontal' : 'vertical');
  }

  toggleFullscreen() {
    this.isFullscreen.update(f => !f);
  }

  expandAll() {
    const allIds = new Set<string>();
    const traverse = (nodes: readonly DepartmentTreeNodeDto[]) => {
      nodes.forEach(n => {
        allIds.add(n.id);
        if (n.children) traverse(n.children);
      });
    };
    traverse(this.treeData());
    this.expandedNodes.set(allIds);
  }

  collapseAll() {
    this.expandedNodes.set(new Set());
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
      // Expand the first level by default
      if (data.length > 0) {
        this.expandedNodes.set(new Set([data[0].id]));
      }
    });
    this.api.getPagedList({ page: 1, pageSize: 1000 }).subscribe((res) => this.allDepartments.set(res.items));
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

  toggleNode(nodeId: string): void {
    this.expandedNodes.update((set) => {
      const newSet = new Set(set);
      if (newSet.has(nodeId)) newSet.delete(nodeId);
      else newSet.add(nodeId);
      return newSet;
    });
  }

  isExpanded(nodeId: string): boolean {
    return this.expandedNodes().has(nodeId);
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
}
