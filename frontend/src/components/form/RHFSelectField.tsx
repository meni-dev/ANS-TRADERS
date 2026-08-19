import { MenuItem, TextField, type TextFieldProps } from '@mui/material'
import { Controller, useFormContext } from 'react-hook-form'

type Option = { value: string; label: string }

type RHFSelectFieldProps = Omit<TextFieldProps, 'name' | 'error' | 'select'> & {
  name: string
  options: Option[]
}

export function RHFSelectField({ name, options, ...textFieldProps }: RHFSelectFieldProps) {
  const { control } = useFormContext()

  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <TextField
          {...field}
          {...textFieldProps}
          value={field.value ?? ''}
          select
          error={!!fieldState.error}
          helperText={fieldState.error?.message ?? textFieldProps.helperText}
          fullWidth
        >
          {options.map((option) => (
            <MenuItem key={option.value} value={option.value}>
              {option.label}
            </MenuItem>
          ))}
        </TextField>
      )}
    />
  )
}
