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
import { useUpdateSupplier } from '../hooks'
import { editSupplierSchema, type EditSupplierFormValues, type SupplierDto } from '../types'
import { SupplierFormFields } from './SupplierFormFields'

function toFormValues(supplier: SupplierDto): EditSupplierFormValues {
  return {
    name: supplier.name,
    phone: supplier.phone,
    email: supplier.email ?? '',
    gstin: supplier.gstin ?? '',
    contactPerson: supplier.contactPerson ?? '',
    addressLine1: supplier.addressLine1 ?? '',
    addressLine2: supplier.addressLine2 ?? '',
    city: supplier.city ?? '',
    state: supplier.state ?? '',
    stateCode: supplier.stateCode ?? '',
    pincode: supplier.pincode ?? '',
    paymentTerms: supplier.paymentTerms ?? '',
    isActive: supplier.isActive,
  }
}

type EditSupplierDialogProps = {
  supplier: SupplierDto
  onClose: () => void
}

export function EditSupplierDialog({ supplier, onClose }: EditSupplierDialogProps) {
  const { notify } = useNotification()
  const [serverError, setServerError] = useState<string | null>(null)
  const form = useForm<EditSupplierFormValues>({
    resolver: zodResolver(editSupplierSchema),
    defaultValues: toFormValues(supplier),
  })
  const updateSupplier = useUpdateSupplier(supplier.id)

  useEffect(() => {
    form.reset(toFormValues(supplier))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [supplier])

  const handleClose = () => {
    if (updateSupplier.isPending) return
    setServerError(null)
    onClose()
  }

  const onSubmit = form.handleSubmit(async (values) => {
    setServerError(null)
    try {
      await updateSupplier.mutateAsync(values)
      notify(`Supplier "${values.name}" updated`)
      onClose()
    } catch (error) {
      setServerError(describeError(error, 'Something went wrong. Please try again.'))
    }
  })

  return (
    <Dialog open onClose={handleClose} maxWidth="md" fullWidth>
      <DialogHeader
        title="Edit Supplier"
        subtitle={supplier.name}
        icon={<EditOutlinedIcon sx={{ fontSize: 20 }} />}
        onClose={handleClose}
        disabled={updateSupplier.isPending}
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

            <FormSection title="Availability">
              <RHFSwitch name="isActive" label="Active" />
              <Typography variant="caption" color="text.disabled" sx={{ display: 'block', mt: 0.5 }}>
                Inactive suppliers stay in the master but are hidden from purchase search.
              </Typography>
            </FormSection>
          </DialogContent>

          <DialogActions sx={{ px: 3, py: 2, gap: 1, flexShrink: 0 }}>
            <Box sx={{ flexGrow: 1 }} />
            <Button onClick={handleClose} disabled={updateSupplier.isPending} variant="outlined">
              Cancel
            </Button>
            <Button type="submit" variant="contained" loading={updateSupplier.isPending}>
              Save Changes
            </Button>
          </DialogActions>
        </form>
      </FormProvider>
    </Dialog>
  )
}
