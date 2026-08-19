import { FormControlLabel, Switch } from '@mui/material'
import { Controller, useFormContext } from 'react-hook-form'

type RHFSwitchProps = {
  name: string
  label: string
}

export function RHFSwitch({ name, label }: RHFSwitchProps) {
  const { control } = useFormContext()

  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <FormControlLabel
          control={<Switch checked={!!field.value} onChange={(e) => field.onChange(e.target.checked)} />}
          label={label}
        />
      )}
    />
  )
}
