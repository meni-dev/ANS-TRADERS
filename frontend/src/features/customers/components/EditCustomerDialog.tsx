import { describeError } from '@/lib/api/errors'
import { DialogHeader } from '@/components/feedback/DialogHeader'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { FormErrorSummary } from '@/components/form/FormErrorSummary'
import { FormSection } from '@/components/form/FormSection'
import { RHFSwitch } from '@/components/form/RHFSwitch'
import { zodResolver } from '@hookform/resolvers/zod'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import { Alert, Box, Button, Dialog, DialogActions, DialogContent, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import { FormProvider, useForm } from 'react-hook-form'
import { useUpdateCustomer } from '../hooks'
import { editCustomerSchema, type CustomerDto, type EditCustomerFormValues } from '../types'
import { CustomerFormFields } from './CustomerFormFields'

function toFormValues(customer: CustomerDto): EditCustomerFormValues {
  return {
    name: customer.name,
    phone: customer.phone,
    email: customer.email ?? '',
    gstin: customer.gstin ?? '',
    addressLine1: customer.addressLine1 ?? '',
    addressLine2: customer.addressLine2 ?? '',
    city: customer.city ?? '',
    state: customer.state ?? '',
    stateCode: customer.stateCode ?? '',
    pincode: customer.pincode ?? '',
    creditLimit: customer.creditLimit,
    creditDays: customer.creditDays,
    isActive: customer.isActive,
  }
}

type EditCustomerDialogProps = {
  customer: CustomerDto
  onClose: () => void
}

export function EditCustomerDialog({ customer, onClose }: EditCustomerDialogProps) {
  const { notify } = useNotification()
  const [serverError, setServerError] = useState<string | null>(null)
  const form = useForm<EditCustomerFormValues>({
    resolver: zodResolver(editCustomerSchema),
    defaultValues: toFormValues(customer),
  })
  const updateCustomer = useUpdateCustomer(customer.id)

  useEffect(() => {
    form.reset(toFormValues(customer))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [customer])

  const handleClose = () => {
    if (updateCustomer.isPending) return
    setServerError(null)
    onClose()
  }

  const onSubmit = form.handleSubmit(async (values) => {
    setServerError(null)
    try {
      await updateCustomer.mutateAsync(values)
      notify(`Customer "${values.name}" updated`)
      onClose()
    } catch (error) {
      setServerError(describeError(error, 'Something went wrong. Please try again.'))
    }
  })

  return (
    <Dialog open onClose={handleClose} maxWidth="md" fullWidth>
      <DialogHeader
        title="Edit Customer"
        subtitle={customer.name}
        icon={<EditOutlinedIcon sx={{ fontSize: 20 }} />}
        onClose={handleClose}
        disabled={updateCustomer.isPending}
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

            <FormSection title="Availability">
              <RHFSwitch name="isActive" label="Active" />
              <Typography variant="caption" color="text.disabled" sx={{ display: 'block', mt: 0.5 }}>
                Inactive customers stay in the master but are hidden from billing search.
              </Typography>
            </FormSection>
          </DialogContent>

          <DialogActions sx={{ px: 3, py: 2, gap: 1, flexShrink: 0 }}>
            <Box sx={{ flexGrow: 1 }} />
            <Button onClick={handleClose} disabled={updateCustomer.isPending} variant="outlined">
              Cancel
            </Button>
            <Button type="submit" variant="contained" loading={updateCustomer.isPending}>
              Save Changes
            </Button>
          </DialogActions>
        </form>
      </FormProvider>
    </Dialog>
  )
}
