export interface NotificationDto {
  id: string;
  userId: string;
  title: string;
  message: string;
  isRead: boolean;
  readAtUtc?: string | null;
  relatedEntityType?: string | null;
  relatedEntityId?: string | null;
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
