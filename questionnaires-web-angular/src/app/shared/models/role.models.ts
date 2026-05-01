export interface RoleListItemDto {
  id: string;
  nameAr: string;
  nameEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  isSystemRole: boolean;
}

export interface RoleDto extends RoleListItemDto {
  permissionCodes: readonly string[];
}

export interface PermissionDto {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  module: string;
}

export interface RoleFilterRequest {
  page?: number;
  pageSize?: number;
  search?: string | null;
}

export interface CreateRoleRequest {
  nameAr: string;
  nameEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  isSystemRole: boolean;
}

export interface UpdateRoleRequest {
  nameAr: string;
  nameEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
}

export interface AssignRolePermissionsRequest {
  permissionIds: string[];
}
