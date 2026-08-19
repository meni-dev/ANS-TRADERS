import { useDebouncedValue } from '@/lib/hooks/useDebouncedValue'
import { Autocomplete, Box, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { useSuppliers } from '../hooks'
import type { SupplierDto } from '../types'

type SupplierPickerProps = {
  value: SupplierDto | null
  onChange: (supplier: SupplierDto | null) => void
  error?: string
  label?: string
}

/** Type-ahead over active suppliers, searchable by name or phone — the two things a shop remembers. */
export function SupplierPicker({ value, onChange, error, label = 'Supplier' }: SupplierPickerProps) {
  const [input, setInput] = useState('')
  const debouncedInput = useDebouncedValue(input)

  const { data, isFetching } = useSuppliers({
    search: debouncedInput || undefined,
    activeOnly: true,
    page: 1,
    pageSize: 20,
  })

  return (
    <Autocomplete
      value={value}
      onChange={(_, next) => onChange(next)}
      onInputChange={(_, next, reason) => {
        if (reason !== 'reset') setInput(next)
      }}
      options={data?.items ?? []}
      loading={isFetching}
      filterOptions={(x) => x}
      getOptionLabel={(option) => option.name}
      isOptionEqualToValue={(option, selected) => option.id === selected.id}
      noOptionsText={debouncedInput ? 'No matching suppliers' : 'Start typing a supplier name'}
      size="small"
      renderInput={(params) => (
        <TextField {...params} label={label} required error={!!error} helperText={error} />
      )}
      renderOption={(props, option) => {
        const { key, ...optionProps } = props as typeof props & { key: string }

        return (
          <Box component="li" key={key} {...optionProps} sx={{ display: 'block !important', py: 1 }}>
            <Typography sx={{ fontSize: 13.5, fontWeight: 600, lineHeight: 1.4 }}>{option.name}</Typography>
            <Typography sx={{ fontSize: 12, color: 'text.disabled', lineHeight: 1.4 }}>
              {option.phone}
              {option.gstin && ` · ${option.gstin}`}
            </Typography>
          </Box>
        )
      }}
    />
  )
}
