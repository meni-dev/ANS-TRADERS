export type RegisterCellType = 'Text' | 'Date' | 'Money' | 'Quantity' | 'Number'

export type RegisterColumn = {
  key: string
  label: string
  type: RegisterCellType
}

export type RegisterTotal = {
  columnKey: string
  value: number
}

export type RegisterSummary = {
  key: string
  title: string
  caption: string
  group: string
  /** Describes a position, not a period — stock has one current level, not a level per date. */
  isAsAt: boolean
}

export type Register = {
  key: string
  title: string
  caption: string
  fromDate: string
  toDate: string
  columns: RegisterColumn[]
  /** Positional, aligned with `columns`. Numbers arrive as invariant text so nothing is rounded. */
  rows: (string | null)[][]
  totals: RegisterTotal[]
  rowCount: number
  /** See {@link RegisterSummary.isAsAt}. When true, the dates above are simply today. */
  isAsAt: boolean
}
