export interface ApiResponse<T> {
  success?: boolean;
  succeeded?: boolean;
  data: T;
  errors?: string[] | null;
  traceId?: string | null;
}

export interface PagedResult<T> {
  items: readonly T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
