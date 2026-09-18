import { useDebouncedValue } from '@/lib/hooks/useDebouncedValue'
import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutlineOutlined'
import { Autocomplete, Box, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { useCustomers } from '../hooks'
import type { CustomerDto } from '../types'

// A sentinel row rather than a second control: the counter is already looking at this field when
// it realises the customer is not on file, so the way in is right there in the dropdown rather
// than a separate button competing for space.
const ADD_NEW_ID = '__add_new_customer__'
const addNewOption = { id: ADD_NEW_ID, name: 'Add new customer', phone: '' } as CustomerDto

type CustomerPickerProps = {
  value: CustomerDto | null
  onChange: (customer: CustomerDto | null) => void
  error?: string
  label?: string
  helperText?: string
  /** Offers "Add new customer" in the dropdown; opens a create dialog when picked. */
  onAddNew?: () => void
  autoFocus?: boolean
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
  onAddNew,
  autoFocus,
}: CustomerPickerProps) {
  const [input, setInput] = useState('')
  const debouncedInput = useDebouncedValue(input)

  const { data, isFetching } = useCustomers({
    search: debouncedInput || undefined,
    activeOnly: true,
    page: 1,
    pageSize: 20,
  })

  const options = [...(data?.items ?? []), ...(onAddNew ? [addNewOption] : [])]

  return (
    <Autocomplete
      value={value}
      onChange={(_, next) => {
        if (next?.id === ADD_NEW_ID) {
          onAddNew?.()
          return
        }
        onChange(next)
      }}
      onInputChange={(_, next, reason) => {
        if (reason !== 'reset') setInput(next)
      }}
      options={options}
      loading={isFetching}
      // The counter types a name and hits Enter without reaching for an arrow key — without this,
      // nothing is highlighted yet, Enter has nothing to confirm, and the keystroke falls through
      // to the form's own Enter-advances-the-field handling, leaving the customer unpicked.
      autoHighlight
      filterOptions={(x) => x}
      getOptionLabel={(option) => (option.id === ADD_NEW_ID ? 'Add new customer' : option.name)}
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
          autoFocus={autoFocus}
        />
      )}
      renderOption={(props, option) => {
        const { key, ...optionProps } = props as typeof props & { key: string }

        if (option.id === ADD_NEW_ID) {
          return (
            <Box
              component="li"
              key={key}
              {...optionProps}
              sx={{ display: 'flex !important', alignItems: 'center', gap: 1, py: 1, color: 'primary.main' }}
            >
              <AddCircleOutlineIcon sx={{ fontSize: 18 }} />
              <Typography sx={{ fontSize: 13.5, fontWeight: 600 }}>Add new customer</Typography>
            </Box>
          )
        }

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
