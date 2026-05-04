export interface NotificationDto {
  id: string;
  userId: string;
  titleAr: string;
  titleEn: string;
  messageAr: string;
  messageEn: string;
  isRead: boolean;
  readAtUtc?: string | null;
  relatedEntityType?: string | null;
  relatedEntityId?: string | null;
  relatedEntityParentId?: string | null;
  createdOnUtc: string;
}

export interface NotificationFilterRequest {
  page?: number;
  pageSize?: number;
  isRead?: boolean | null;
  search?: string | null;
}

export interface MarkNotificationReadRequest {
  isRead: boolean;
}
