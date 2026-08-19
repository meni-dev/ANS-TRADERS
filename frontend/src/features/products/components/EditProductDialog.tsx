import { DialogHeader } from '@/components/feedback/DialogHeader'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { FormErrorSummary } from '@/components/form/FormErrorSummary'
import { RHFNumberField } from '@/components/form/RHFNumberField'
import { RHFSwitch } from '@/components/form/RHFSwitch'
import { ApiError } from '@/lib/api/client'
import { zodResolver } from '@hookform/resolvers/zod'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import { Alert, Box, Button, Dialog, DialogActions, DialogContent, Grid, TextField, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import { FormProvider, useForm } from 'react-hook-form'
import { useUpdateProduct } from '../hooks'
import { editProductSchema, type EditProductFormValues, type ProductDto } from '../types'
import { FormSection } from '@/components/form/FormSection'
import { ProductFormFields } from './ProductFormFields'

function toFormValues(product: ProductDto): EditProductFormValues {
  return {
    itemCode: product.itemCode,
    partNumber: product.partNumber,
    itemName: product.itemName,
    description: product.description ?? '',
    vehicleBrand: product.vehicleBrand ?? '',
    vehicleModel: product.vehicleModel ?? '',
    hsn: product.hsn ?? '',
    gstRate: product.gstRate,
    uqc: product.uqc,
    // Editing the catalogue implies permission to see the rate, so it is present here. The
    // fallback is only so the form has a number to start from if it ever is not.
    purchaseRate: product.purchaseRate ?? 0,
    sellingRate: product.sellingRate,
    mrp: product.mrp,
    reorderLevel: product.reorderLevel,
    isActive: product.isActive,
  }
}

type EditProductDialogProps = {
  product: ProductDto
  onClose: () => void
}

export function EditProductDialog({ product, onClose }: EditProductDialogProps) {
  const { notify } = useNotification()
  const [serverError, setServerError] = useState<string | null>(null)
  const form = useForm<EditProductFormValues>({
    resolver: zodResolver(editProductSchema),
    defaultValues: toFormValues(product),
  })
  const updateProduct = useUpdateProduct(product.id)

  useEffect(() => {
    form.reset(toFormValues(product))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [product])

  const handleClose = () => {
    if (updateProduct.isPending) return
    setServerError(null)
    onClose()
  }

  const onSubmit = form.handleSubmit(async (values) => {
    setServerError(null)
    try {
      await updateProduct.mutateAsync(values)
      notify(`Product "${values.itemName}" updated`)
      onClose()
    } catch (error) {
      if (error instanceof ApiError) {
        setServerError(error.message)
      } else {
        setServerError('Something went wrong. Please try again.')
      }
    }
  })

  return (
    <Dialog open onClose={handleClose} maxWidth="md" fullWidth>
      <DialogHeader
        title="Edit Product"
        subtitle={product.itemName}
        icon={<EditOutlinedIcon sx={{ fontSize: 20 }} />}
        onClose={handleClose}
        disabled={updateProduct.isPending}
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

            <ProductFormFields />

            <FormSection title="Stock & Availability">
              {/* Opening stock is deliberately absent: it is a historical fact, and stock on hand
                  moves only through purchases, invoices and ledger adjustments. */}
              <Grid container spacing={2} sx={{ mb: 2 }}>
                <Grid size={{ xs: 6, sm: 3 }}>
                  <TextField
                    label="Stock on Hand"
                    value={product.stockOnHand}
                    disabled
                    fullWidth
                    helperText="Adjust from the Stock screen"
                  />
                </Grid>
                <Grid size={{ xs: 6, sm: 3 }}>
                  <RHFNumberField name="reorderLevel" label="Reorder Level" />
                </Grid>
              </Grid>

              <RHFSwitch name="isActive" label="Active" />
              <Typography variant="caption" color="text.disabled" sx={{ display: 'block', mt: 0.5 }}>
                Inactive items stay in the master but are hidden from billing search.
              </Typography>
            </FormSection>
          </DialogContent>

          <DialogActions sx={{ px: 3, py: 2, gap: 1, flexShrink: 0 }}>
            <Box sx={{ flexGrow: 1 }} />
            <Button onClick={handleClose} disabled={updateProduct.isPending} variant="outlined">
              Cancel
            </Button>
            <Button type="submit" variant="contained" loading={updateProduct.isPending}>
              Save Changes
            </Button>
          </DialogActions>
        </form>
      </FormProvider>
    </Dialog>
  )
}
