export interface AuditLogDto {
  id: string;
  occurredAtUtc: string;
  userId?: string | null;
  userName?: string | null;
  action: string;
  entityType: string;
  entityId: string;
  oldValuesJson?: string | null;
  newValuesJson?: string | null;
  ipAddress?: string | null;
  userAgent?: string | null;
}

export interface AuditLogFilterRequest {
  page?: number;
  pageSize?: number;
  userId?: string | null;
  action?: string | null;
  entityType?: string | null;
  fromUtc?: Date | string | null;
  toUtc?: Date | string | null;
}
