import { ProductPicker } from '@/components/document/ProductPicker'
import type { ProductDto } from '@/features/products/types'
import { computeLine } from '@/lib/documents/gst'
import { emptyLine, type DocumentLineValues } from '@/lib/documents/types'
import { formatCurrency, formatQuantity } from '@/lib/format'
import AddIcon from '@mui/icons-material/Add'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined'
import {
  Box,
  Button,
  IconButton,
  InputAdornment,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import { useFieldArray, useFormContext, useWatch } from 'react-hook-form'

type LineItemsEditorProps = {
  /** Which price on the product master seeds a new line: purchases buy at one, sales sell at the other. */
  rateSource: 'purchaseRate' | 'sellingRate'
  isInterState: boolean
  /**
   * Shows available stock per line and flags over-billing as the quantity is typed. Only meaningful
   * on the sales side — a purchase is what puts stock on the shelf in the first place.
   */
  showStock?: boolean
}

/** Narrow numeric cell input. The shared RHF field components are full-width and too tall here. */
function NumberCell({
  name,
  align = 'right',
  adornment,
  width,
}: {
  name: string
  align?: 'left' | 'right'
  adornment?: string
  width: number
}) {
  const { register, formState } = useFormContext()
  const path = name.split('.')
  // Field array errors nest as items[0].quantity, which is not reachable by a plain lookup.
  const error = path.reduce<unknown>(
    (node, key) => (node as Record<string, unknown> | undefined)?.[key],
    formState.errors,
  ) as { message?: string } | undefined

  return (
    <TextField
      {...register(name, { valueAsNumber: true })}
      type="number"
      size="small"
      error={!!error}
      sx={{ width }}
      slotProps={{
        htmlInput: { step: 'any', style: { textAlign: align, fontVariantNumeric: 'tabular-nums' } },
        input: adornment
          ? { startAdornment: <InputAdornment position="start">{adornment}</InputAdornment> }
          : undefined,
      }}
    />
  )
}

/**
 * The line grid shared by the purchase and invoice forms. Both documents bill the same way — the
 * only difference is which product rate seeds a new row — so they use one editor rather than two
 * that drift apart.
 */
export function LineItemsEditor({ rateSource, isInterState, showStock }: LineItemsEditorProps) {
  const { control, setValue, formState } = useFormContext()
  const { fields, append, remove } = useFieldArray({ control, name: 'items' })

  // Watching the array keeps the per-row amounts live as the user types.
  const items = (useWatch({ control, name: 'items' }) ?? []) as DocumentLineValues[]

  const chosenProductIds = items.map((line) => line?.productId).filter(Boolean)

  const handleProductChange = (index: number, product: ProductDto | null) => {
    if (!product) {
      setValue(`items.${index}.productId`, '', { shouldValidate: true })
      return
    }

    // The snapshot columns and the rate are filled from the master so the row is complete the
    // moment a part is picked — the common case is that neither needs editing.
    setValue(`items.${index}.productId`, product.id, { shouldValidate: true })
    setValue(`items.${index}.partNumber`, product.partNumber)
    setValue(`items.${index}.itemName`, product.itemName)
    setValue(`items.${index}.hsn`, product.hsn)
    setValue(`items.${index}.uqc`, product.uqc)
    setValue(`items.${index}.gstRate`, product.gstRate)
    setValue(`items.${index}.stockOnHand`, product.stockOnHand)
    setValue(`items.${index}.rate`, product[rateSource])
  }

  const itemsError = (formState.errors.items as { message?: string } | undefined)?.message

  const headerCell = {
    fontSize: 11.5,
    fontWeight: 700,
    letterSpacing: '0.02em',
    textTransform: 'uppercase' as const,
    color: 'text.secondary',
    py: 1,
    borderBottom: '1px solid',
    borderColor: 'divider',
    whiteSpace: 'nowrap' as const,
  }

  return (
    <Box>
      <Box sx={{ overflowX: 'auto' }}>
        <Table size="small" sx={{ minWidth: 900 }}>
          <TableHead>
            <TableRow>
              <TableCell sx={{ ...headerCell, width: 36, pl: 0 }}>#</TableCell>
              <TableCell sx={{ ...headerCell, minWidth: 260 }}>Item</TableCell>
              <TableCell sx={{ ...headerCell, width: 96 }}>HSN</TableCell>
              <TableCell align="right" sx={{ ...headerCell, width: 104 }}>Qty</TableCell>
              <TableCell align="right" sx={{ ...headerCell, width: 132 }}>Rate</TableCell>
              <TableCell align="right" sx={{ ...headerCell, width: 104 }}>Disc %</TableCell>
              <TableCell align="right" sx={{ ...headerCell, width: 72 }}>GST</TableCell>
              <TableCell align="right" sx={{ ...headerCell, width: 130 }}>Amount</TableCell>
              <TableCell sx={{ ...headerCell, width: 44, pr: 0 }} />
            </TableRow>
          </TableHead>

          <TableBody>
            {fields.map((field, index) => {
              const line = items[index] ?? emptyLine
              const amounts = computeLine(
                {
                  quantity: line.quantity,
                  rate: line.rate,
                  discountPercent: line.discountPercent,
                  gstRate: line.gstRate,
                },
                isInterState,
              )

              // RHF types `errors.items` as the union of a whole-array error and a per-row array,
              // so the row lookup has to go through unknown.
              const rowError = (
                formState.errors.items as unknown as
                  | Array<{ productId?: { message?: string } } | undefined>
                  | undefined
              )?.[index]?.productId?.message

              const selected: ProductDto | null = line.productId
                ? ({
                    id: line.productId,
                    partNumber: line.partNumber,
                    itemName: line.itemName,
                  } as ProductDto)
                : null

              return (
                <TableRow key={field.id} sx={{ '& td': { borderColor: 'grey.100', py: 1 } }}>
                  <TableCell sx={{ pl: 0, color: 'text.disabled', fontSize: 12.5 }}>{index + 1}</TableCell>

                  <TableCell>
                    <ProductPicker
                      value={selected}
                      onChange={(product) => handleProductChange(index, product)}
                      error={rowError}
                      excludeIds={chosenProductIds}
                      showStock={showStock}
                      autoFocus={index === fields.length - 1 && !line.productId}
                    />
                  </TableCell>

                  <TableCell>
                    <Typography sx={{ fontSize: 12.5, color: line.hsn ? 'text.secondary' : 'text.disabled' }}>
                      {line.hsn || '—'}
                    </Typography>
                  </TableCell>

                  <TableCell align="right">
                    <NumberCell name={`items.${index}.quantity`} width={88} />
                    {showStock && line.productId && (
                      // Stock sits under the box being typed into rather than in its own column:
                      // it is only interesting while a quantity is being decided.
                      <Typography
                        sx={{
                          fontSize: 11,
                          lineHeight: 1.4,
                          mt: 0.25,
                          fontVariantNumeric: 'tabular-nums',
                          color: line.quantity > line.stockOnHand ? 'error.dark' : 'text.disabled',
                        }}
                      >
                        {line.quantity > line.stockOnHand
                          ? `only ${formatQuantity(line.stockOnHand)} left`
                          : `${formatQuantity(line.stockOnHand)} in stock`}
                      </Typography>
                    )}
                  </TableCell>

                  <TableCell align="right">
                    <NumberCell name={`items.${index}.rate`} width={116} adornment="₹" />
                  </TableCell>

                  <TableCell align="right">
                    <NumberCell name={`items.${index}.discountPercent`} width={88} />
                  </TableCell>

                  <TableCell align="right">
                    <Typography sx={{ fontSize: 12.5, color: 'text.secondary', fontVariantNumeric: 'tabular-nums' }}>
                      {line.gstRate ? `${line.gstRate}%` : '—'}
                    </Typography>
                  </TableCell>

                  <TableCell align="right">
                    <Typography sx={{ fontSize: 13.5, fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
                      {formatCurrency(amounts.lineTotal)}
                    </Typography>
                    {amounts.discountAmount > 0 && (
                      <Typography sx={{ fontSize: 11.5, color: 'text.disabled', fontVariantNumeric: 'tabular-nums' }}>
                        −{formatCurrency(amounts.discountAmount)} off
                      </Typography>
                    )}
                  </TableCell>

                  <TableCell sx={{ pr: 0 }}>
                    <Tooltip title={fields.length === 1 ? 'A bill needs at least one line' : 'Remove line'}>
                      <Box component="span">
                        <IconButton
                          size="small"
                          onClick={() => remove(index)}
                          disabled={fields.length === 1}
                          aria-label={`Remove line ${index + 1}`}
                        >
                          <DeleteOutlineIcon sx={{ fontSize: 18 }} />
                        </IconButton>
                      </Box>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              )
            })}
          </TableBody>
        </Table>
      </Box>

      <Stack direction="row" spacing={2} sx={{ alignItems: 'center', mt: 1.5 }}>
        <Button size="small" variant="outlined" startIcon={<AddIcon />} onClick={() => append({ ...emptyLine })}>
          Add Line
        </Button>
        {itemsError && (
          <Typography sx={{ fontSize: 12.5, color: 'error.dark' }}>{itemsError}</Typography>
        )}
      </Stack>
    </Box>
  )
}
