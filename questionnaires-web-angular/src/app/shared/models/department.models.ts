import { PagedResult } from './api.types';

export interface DepartmentDto {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  parentDepartmentId?: string | null;
  parentDepartmentNameAr?: string | null;
  parentDepartmentNameEn?: string | null;
  employeeCount: number;
  subDepartmentCount: number;
}

export interface DepartmentListItemDto {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  parentDepartmentId?: string | null;
  parentDepartmentNameAr?: string | null;
  parentDepartmentNameEn?: string | null;
  employeeCount: number;
  subDepartmentCount: number;
}

export interface DepartmentTreeNodeDto {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  employeeCount: number;
  children: DepartmentTreeNodeDto[];
}

export interface CreateDepartmentRequest {
  code: string;
  nameAr: string;
  nameEn: string;
  parentDepartmentId?: string | null;
}

export interface UpdateDepartmentRequest {
  code: string;
  nameAr: string;
  nameEn: string;
  parentDepartmentId?: string | null;
}

export interface DepartmentFilterRequest {
  search?: string | null;
  parentDepartmentId?: string | null;
  page: number;
  pageSize: number;
}
