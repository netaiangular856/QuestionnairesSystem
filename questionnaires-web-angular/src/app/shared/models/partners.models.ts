export enum PartnerType {
  Dealer = 1,
  Partner = 2,
  Vendor = 3,
  Customer = 4,
  Other = 99,
}

export interface PartnerDto {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  type: PartnerType;
  email?: string | null;
  phoneNumber?: string | null;
  contactPerson?: string | null;
  isActive: boolean;
  address?: string | null;
}

export interface PartnerListItemDto {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  type: PartnerType;
  email?: string | null;
  isActive: boolean;
}

export interface CreatePartnerRequest {
  code: string;
  nameAr: string;
  nameEn: string;
  type: PartnerType;
  email?: string | null;
  phoneNumber?: string | null;
  contactPerson?: string | null;
  address?: string | null;
}

export interface UpdatePartnerRequest {
  code: string;
  nameAr: string;
  nameEn: string;
  type: PartnerType;
  email?: string | null;
  phoneNumber?: string | null;
  contactPerson?: string | null;
  isActive: boolean;
  address?: string | null;
}

export interface PartnerFilterRequest {
  search?: string | null;
  type?: PartnerType | null;
  isActive?: boolean | null;
  page: number;
  pageSize: number;
}
