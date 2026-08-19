import { TextField, type TextFieldProps } from '@mui/material'
import { Controller, useFormContext } from 'react-hook-form'

type RHFNumberFieldProps = Omit<TextFieldProps, 'name' | 'error' | 'type'> & {
  name: string
}

export function RHFNumberField({ name, slotProps, ...textFieldProps }: RHFNumberFieldProps) {
  const { control } = useFormContext()

  // Merged rather than replaced: callers pass `input` adornments (₹, %) and this component owns
  // `htmlInput`. Spreading one over the other would silently drop whichever came first.
  const mergedSlotProps = {
    ...slotProps,
    htmlInput: { step: 'any', ...(slotProps?.htmlInput as object) },
  }

  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <TextField
          {...textFieldProps}
          type="number"
          value={field.value ?? 0}
          onChange={(event) => {
            const raw = event.target.value
            field.onChange(raw === '' ? '' : Number(raw))
          }}
          onBlur={field.onBlur}
          name={field.name}
          inputRef={field.ref}
          error={!!fieldState.error}
          helperText={fieldState.error?.message ?? textFieldProps.helperText}
          fullWidth
          slotProps={mergedSlotProps}
        />
      )}
    />
  )
}
