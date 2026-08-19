import { TextField, type TextFieldProps } from '@mui/material'
import { Controller, useFormContext } from 'react-hook-form'

type RHFTextFieldProps = Omit<TextFieldProps, 'name' | 'error'> & {
  name: string
}

export function RHFTextField({ name, ...textFieldProps }: RHFTextFieldProps) {
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
          error={!!fieldState.error}
          helperText={fieldState.error?.message ?? textFieldProps.helperText}
          fullWidth
        />
      )}
    />
  )
}
