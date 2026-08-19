import { Alert, AlertTitle } from '@mui/material'
import { useFormContext } from 'react-hook-form'

/**
 * Shown once the user has tried to submit and validation failed. React Hook Form validates every
 * field on submit, so each invalid input already carries its own red outline and message — this
 * just tells the user how many there are, since the offending field may be scrolled out of view.
 */
export function FormErrorSummary() {
  const {
    formState: { errors, submitCount },
  } = useFormContext()

  const count = Object.keys(errors).length
  if (submitCount === 0 || count === 0) return null

  return (
    <Alert severity="error" sx={{ mb: 2 }}>
      <AlertTitle sx={{ fontWeight: 600 }}>
        {count === 1 ? '1 field needs attention' : `${count} fields need attention`}
      </AlertTitle>
      Check the fields outlined in red below and try again.
    </Alert>
  )
}
