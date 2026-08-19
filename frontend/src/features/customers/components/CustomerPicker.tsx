import { useDebouncedValue } from '@/lib/hooks/useDebouncedValue'
import { Autocomplete, Box, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { useCustomers } from '../hooks'
import type { CustomerDto } from '../types'

type CustomerPickerProps = {
  value: CustomerDto | null
  onChange: (customer: CustomerDto | null) => void
  error?: string
  label?: string
  helperText?: string
}

/**
 * Type-ahead over active customers. Phone is searchable and shown on every row because that is how
 * a counter identifies a returning customer — two "Kumar"s are told apart by number, not by name.
 */
export function CustomerPicker({
  value,
  onChange,
  error,
  label = 'Customer',
  helperText,
}: CustomerPickerProps) {
  const [input, setInput] = useState('')
  const debouncedInput = useDebouncedValue(input)

  const { data, isFetching } = useCustomers({
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
      noOptionsText={debouncedInput ? 'No matching customers' : 'Start typing a name or phone number'}
      size="small"
      renderInput={(params) => (
        <TextField
          {...params}
          label={label}
          error={!!error}
          helperText={error ?? helperText}
          placeholder="Search by name or phone…"
        />
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
