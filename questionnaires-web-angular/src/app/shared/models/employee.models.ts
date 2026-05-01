export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  userName: string;
  email: string;
  password: string;
  nameAr?: string | null;
  nameEn?: string | null;
}

export interface LoginResponseDto {
  accessToken: string;
  expiresAtUtc: string;
  tokenType: string;
}

export interface ChangePasswordRequest {
  email: string;
  currentPassword: string;
  newPassword: string;
}
