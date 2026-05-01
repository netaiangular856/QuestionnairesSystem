export interface UserProfileDto {
  id: string;
  userName: string;
  nameAr: string | null;
  nameEn: string | null;
  email: string;
  isActive: boolean;
  employeeId: string | null;
  lastLoginUtc: string | null;
  avatarUrl: string | null;
  roleNames: readonly string[];
}
