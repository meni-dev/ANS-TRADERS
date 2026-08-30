import { describeError } from '@/lib/api/errors'
import { DialogHeader } from '@/components/feedback/DialogHeader'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { FormErrorSummary } from '@/components/form/FormErrorSummary'
import { FormSection } from '@/components/form/FormSection'
import { RHFNumberField } from '@/components/form/RHFNumberField'
import { zodResolver } from '@hookform/resolvers/zod'
import LocalShippingOutlinedIcon from '@mui/icons-material/LocalShippingOutlined'
import { Alert, Box, Button, Dialog, DialogActions, DialogContent, Grid, InputAdornment } from '@mui/material'
import { useState } from 'react'
import { FormProvider, useForm } from 'react-hook-form'
import { useCreateSupplier } from '../hooks'
import { createSupplierSchema, type CreateSupplierFormValues } from '../types'
import { SupplierFormFields } from './SupplierFormFields'

const defaultValues: CreateSupplierFormValues = {
  name: '',
  phone: '',
  email: '',
  gstin: '',
  contactPerson: '',
  addressLine1: '',
  addressLine2: '',
  city: '',
  state: '',
  stateCode: '',
  pincode: '',
  paymentTerms: '',
  openingBalance: 0,
}

type CreateSupplierDialogProps = {
  open: boolean
  onClose: () => void
}

export function CreateSupplierDialog({ open, onClose }: CreateSupplierDialogProps) {
  const { notify } = useNotification()
  const [serverError, setServerError] = useState<string | null>(null)
  const form = useForm<CreateSupplierFormValues>({
    resolver: zodResolver(createSupplierSchema),
    defaultValues,
  })
  const createSupplier = useCreateSupplier()

  const handleClose = () => {
    if (createSupplier.isPending) return
    form.reset(defaultValues)
    setServerError(null)
    onClose()
  }

  const onSubmit = form.handleSubmit(async (values) => {
    setServerError(null)
    try {
      await createSupplier.mutateAsync(values)
      notify(`Supplier "${values.name}" created`)
      form.reset(defaultValues)
      onClose()
    } catch (error) {
      setServerError(describeError(error, 'Something went wrong. Please try again.'))
    }
  })

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
      <DialogHeader
        title="Add Supplier"
        subtitle="Create a new supplier in the party master."
        icon={<LocalShippingOutlinedIcon sx={{ fontSize: 20 }} />}
        onClose={handleClose}
        disabled={createSupplier.isPending}
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

            <SupplierFormFields />

            <FormSection title="Opening Balance" caption="Amount already payable when tracking starts.">
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
            <Button onClick={handleClose} disabled={createSupplier.isPending} variant="outlined">
              Cancel
            </Button>
            <Button type="submit" variant="contained" loading={createSupplier.isPending}>
              Save Supplier
            </Button>
          </DialogActions>
        </form>
      </FormProvider>
    </Dialog>
  )
}
