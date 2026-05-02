export interface EmployeeDto {
  id: string;
  employeeNumber: string;
  nameAr: string;
  nameEn: string;
  email?: string | null;
  phoneNumber?: string | null;
  jobTitleAr?: string | null;
  jobTitleEn?: string | null;
  departmentId?: string | null;
  departmentNameAr?: string | null;
  departmentNameEn?: string | null;
  isActive: boolean;
  userId?: string | null;
  userName?: string | null;
}

export interface EmployeeListItemDto {
  id: string;
  employeeNumber: string;
  nameAr: string;
  nameEn: string;
  email?: string | null;
  jobTitleAr?: string | null;
  jobTitleEn?: string | null;
  departmentNameAr?: string | null;
  departmentNameEn?: string | null;
  isActive: boolean;
}

export interface CreateEmployeeRequest {
  employeeNumber: string;
  nameAr: string;
  nameEn: string;
  email?: string | null;
  phoneNumber?: string | null;
  jobTitleAr?: string | null;
  jobTitleEn?: string | null;
  departmentId?: string | null;
}

export interface UpdateEmployeeRequest {
  employeeNumber: string;
  nameAr: string;
  nameEn: string;
  email?: string | null;
  phoneNumber?: string | null;
  jobTitleAr?: string | null;
  jobTitleEn?: string | null;
  departmentId?: string | null;
  isActive: boolean;
}

export interface EmployeeFilterRequest {
  search?: string | null;
  departmentId?: string | null;
  isActive?: boolean | null;
  page: number;
  pageSize: number;
}
