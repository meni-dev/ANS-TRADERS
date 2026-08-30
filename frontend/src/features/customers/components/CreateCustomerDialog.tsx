import { describeError } from '@/lib/api/errors'
import { DialogHeader } from '@/components/feedback/DialogHeader'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { FormErrorSummary } from '@/components/form/FormErrorSummary'
import { FormSection } from '@/components/form/FormSection'
import { RHFNumberField } from '@/components/form/RHFNumberField'
import { zodResolver } from '@hookform/resolvers/zod'
import PersonAddAlt1OutlinedIcon from '@mui/icons-material/PersonAddAlt1Outlined'
import { Alert, Box, Button, Dialog, DialogActions, DialogContent, Grid, InputAdornment } from '@mui/material'
import { useState } from 'react'
import { FormProvider, useForm } from 'react-hook-form'
import { useCreateCustomer } from '../hooks'
import { createCustomerSchema, type CreateCustomerFormValues } from '../types'
import { CustomerFormFields } from './CustomerFormFields'

const defaultValues: CreateCustomerFormValues = {
  name: '',
  phone: '',
  email: '',
  gstin: '',
  addressLine1: '',
  addressLine2: '',
  city: '',
  state: '',
  stateCode: '',
  pincode: '',
  creditLimit: 0,
  creditDays: 0,
  openingBalance: 0,
}

type CreateCustomerDialogProps = {
  open: boolean
  onClose: () => void
}

export function CreateCustomerDialog({ open, onClose }: CreateCustomerDialogProps) {
  const { notify } = useNotification()
  const [serverError, setServerError] = useState<string | null>(null)
  const form = useForm<CreateCustomerFormValues>({
    resolver: zodResolver(createCustomerSchema),
    defaultValues,
  })
  const createCustomer = useCreateCustomer()

  const handleClose = () => {
    if (createCustomer.isPending) return
    form.reset(defaultValues)
    setServerError(null)
    onClose()
  }

  const onSubmit = form.handleSubmit(async (values) => {
    setServerError(null)
    try {
      await createCustomer.mutateAsync(values)
      notify(`Customer "${values.name}" created`)
      form.reset(defaultValues)
      onClose()
    } catch (error) {
      setServerError(describeError(error, 'Something went wrong. Please try again.'))
    }
  })

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
      <DialogHeader
        title="Add Customer"
        subtitle="Create a new customer in the party master."
        icon={<PersonAddAlt1OutlinedIcon sx={{ fontSize: 20 }} />}
        onClose={handleClose}
        disabled={createCustomer.isPending}
      />

      <FormProvider {...form}>
        {/* noValidate hands validation to zod: the browser would otherwise stop at the first
            empty required input, hiding every other error from the user. */}
        <form
          onSubmit={onSubmit}
          noValidate
          style={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0 }}
        >
          <DialogContent
            dividers
            sx={{ flex: 1, overflowY: 'auto', px: 3, py: 2.5, bgcolor: 'grey.50' }}
          >
            {serverError && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {serverError}
              </Alert>
            )}
            <FormErrorSummary />

            <CustomerFormFields />

            <FormSection title="Opening Balance" caption="Amount already outstanding when tracking starts.">
              <Grid container spacing={2}>
                <Grid size={{ xs: 12, sm: 4 }}>
                  <RHFNumberField
                    name="openingBalance"
                    label="Opening Balance"
                    required
                    slotProps={{ input: { startAdornment: <InputAdornment position="start">₹</InputAdornment> } }}
                  />
                </Grid>
              </Grid>
            </FormSection>
          </DialogContent>

          <DialogActions sx={{ px: 3, py: 2, gap: 1, flexShrink: 0 }}>
            <Box sx={{ flexGrow: 1 }} />
            <Button onClick={handleClose} disabled={createCustomer.isPending} variant="outlined">
              Cancel
            </Button>
            <Button type="submit" variant="contained" loading={createCustomer.isPending}>
              Save Customer
            </Button>
          </DialogActions>
        </form>
      </FormProvider>
    </Dialog>
  )
}
