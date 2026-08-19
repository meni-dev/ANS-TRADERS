import { z } from 'zod'

export type ProductDto = {
  id: string
  itemCode: string
  partNumber: string
  itemName: string
  description?: string | null
  vehicleBrand?: string | null
  vehicleModel?: string | null
  hsn: string
  gstRate: number
  cgstRate: number
  sgstRate: number
  uqc: string
  /** Null when this person may not see cost. Not zero — zero would read as a part the shop gets free. */
  purchaseRate: number | null
  sellingRate: number
  mrp: number
  openingStock: number
  stockOnHand: number
  reorderLevel: number
  isActive: boolean
  createdAt: string
  updatedAt: string
}

// Re-exported so existing product imports keep working now that the envelope lives in lib.
export type { PagedResult } from '@/lib/api/types'

const baseProductFields = {
  itemCode: z.string().min(1, 'Item code is required').max(100),
  partNumber: z.string().min(1, 'Part number is required').max(100),
  itemName: z.string().min(1, 'Item name is required').max(200),
  description: z.string().max(1000).optional().or(z.literal('')),
  vehicleBrand: z.string().max(100).optional().or(z.literal('')),
  vehicleModel: z.string().max(100).optional().or(z.literal('')),
  hsn: z.string().min(1, 'HSN code is required').max(20),
  gstRate: z.number('Enter a GST rate').min(0, 'Cannot be negative').max(100, 'Cannot exceed 100'),
  uqc: z.string().min(1, 'UQC is required').max(20),
  purchaseRate: z.number('Enter a purchase rate').min(0, 'Cannot be negative'),
  sellingRate: z.number('Enter a selling rate').min(0, 'Cannot be negative'),
  mrp: z.number('Enter an MRP').min(0, 'Cannot be negative'),
  reorderLevel: z.number('Enter a reorder level').min(0, 'Cannot be negative'),
}

export const createProductSchema = z.object({
  ...baseProductFields,
  openingStock: z.number('Enter an opening stock').min(0, 'Cannot be negative'),
})

export const editProductSchema = z.object({
  ...baseProductFields,
  isActive: z.boolean(),
})

export type CreateProductFormValues = z.infer<typeof createProductSchema>
export type EditProductFormValues = z.infer<typeof editProductSchema>

// ---------------------------------------------------------------------------------------------
// Catalogue import
// ---------------------------------------------------------------------------------------------

/**
 * One row exactly as it came out of the sheet. Every field is a string because a real file contains
 * whatever somebody typed — the server turns them into numbers and reports the ones that will not,
 * against their row number.
 */
export type ProductImportRow = {
  rowNumber: number
  itemCode?: string | null
  partNumber?: string | null
  itemName?: string | null
  description?: string | null
  vehicleBrand?: string | null
  vehicleModel?: string | null
  hsn?: string | null
  gstRate?: string | null
  uqc?: string | null
  purchaseRate?: string | null
  sellingRate?: string | null
  mrp?: string | null
  openingStock?: string | null
  reorderLevel?: string | null
}

export type ImportRowAction = 'Create' | 'Update' | 'Reject'

export type ProductImportRowResult = {
  rowNumber: number
  partNumber: string
  itemName: string
  action: ImportRowAction
  errors: string[]
}

export type ProductImportPreviewDto = {
  totalRows: number
  willCreate: number
  willUpdate: number
  rejected: number
  rows: ProductImportRowResult[]
}

export type ProductImportResultDto = {
  created: number
  updated: number
}

/** The column order of the downloadable template, and what the parser looks for. */
export const IMPORT_COLUMNS = [
  { key: 'itemCode', header: 'Item Code' },
  { key: 'partNumber', header: 'Part Number' },
  { key: 'itemName', header: 'Item Name' },
  { key: 'description', header: 'Description' },
  { key: 'vehicleBrand', header: 'Vehicle Brand' },
  { key: 'vehicleModel', header: 'Vehicle Model' },
  { key: 'hsn', header: 'HSN' },
  { key: 'gstRate', header: 'GST Rate' },
  { key: 'uqc', header: 'Unit' },
  { key: 'purchaseRate', header: 'Purchase Rate' },
  { key: 'sellingRate', header: 'Selling Rate' },
  { key: 'mrp', header: 'MRP' },
  { key: 'openingStock', header: 'Opening Stock' },
  { key: 'reorderLevel', header: 'Reorder Level' },
] as const

