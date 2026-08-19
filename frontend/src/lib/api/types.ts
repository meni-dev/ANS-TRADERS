/** Envelope every paged list endpoint returns. Shared so features do not import from each other. */
export type PagedResult<T> = {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}
