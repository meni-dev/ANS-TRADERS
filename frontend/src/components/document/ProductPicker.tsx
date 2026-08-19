import { useProducts } from '@/features/products/hooks'
import type { ProductDto } from '@/features/products/types'
import { formatQuantity } from '@/lib/format'
import { useDebouncedValue } from '@/lib/hooks/useDebouncedValue'
import { Autocomplete, Box, TextField, Typography } from '@mui/material'
import { useState } from 'react'

type ProductPickerProps = {
  value: ProductDto | null
  onChange: (product: ProductDto | null) => void
  error?: string
  /** Products already on the document. Picking one twice is rejected server-side, so it is hidden here. */
  excludeIds?: string[]
  autoFocus?: boolean
  /** Annotates each option with its stock. Off for purchases, where stock is not a constraint. */
  showStock?: boolean
}

/**
 * Type-ahead over the item master, keyed off part number as well as name — at a counter the part
 * number is what is read off the box, and it is shorter to type than the description.
 */
export function ProductPicker({
  value,
  onChange,
  error,
  excludeIds = [],
  autoFocus,
  showStock,
}: ProductPickerProps) {
  const [input, setInput] = useState('')
  const debouncedInput = useDebouncedValue(input)

  const { data, isFetching } = useProducts({
    search: debouncedInput || undefined,
    activeOnly: true,
    page: 1,
    pageSize: 20,
  })

  const options = (data?.items ?? []).filter(
    (product) => product.id === value?.id || !excludeIds.includes(product.id),
  )

  return (
    <Autocomplete
      value={value}
      onChange={(_, next) => onChange(next)}
      // The input is left uncontrolled — MUI shows the chosen product's label on its own. This
      // only mirrors what the user types so the search term can be debounced off it.
      onInputChange={(_, next, reason) => {
        if (reason !== 'reset') setInput(next)
      }}
      options={options}
      loading={isFetching}
      // The list is already the server's ranked search result; re-filtering it locally would
      // discard matches on fields the client never sees.
      filterOptions={(x) => x}
      getOptionLabel={(option) => `${option.partNumber} · ${option.itemName}`}
      isOptionEqualToValue={(option, selected) => option.id === selected.id}
      noOptionsText={debouncedInput ? 'No matching parts' : 'Start typing a part number or name'}
      size="small"
      renderInput={(params) => (
        <TextField
          {...params}
          placeholder="Part number or item name…"
          error={!!error}
          helperText={error}
          autoFocus={autoFocus}
        />
      )}
      renderOption={(props, option) => {
        const { key, ...optionProps } = props as typeof props & { key: string }
        const vehicle = [option.vehicleBrand, option.vehicleModel].filter(Boolean).join(' · ')

        return (
          <Box component="li" key={key} {...optionProps} sx={{ display: 'block !important', py: 1 }}>
            <Typography sx={{ fontSize: 13.5, fontWeight: 600, lineHeight: 1.4 }}>
              {option.itemName}
            </Typography>
            <Typography sx={{ fontSize: 12, color: 'text.disabled', lineHeight: 1.4 }}>
              {option.partNumber}
              {vehicle && ` · ${vehicle}`} · GST {option.gstRate}%
              {showStock && (
                <Typography
                  component="span"
                  sx={{
                    fontSize: 12,
                    fontWeight: 600,
                    ml: 0.75,
                    color: option.stockOnHand <= 0 ? 'error.dark' : 'success.dark',
                  }}
                >
                  {option.stockOnHand <= 0
                    ? 'out of stock'
                    : `${formatQuantity(option.stockOnHand)} ${option.uqc}`}
                </Typography>
              )}
            </Typography>
          </Box>
        )
      }}
    />
  )
}
