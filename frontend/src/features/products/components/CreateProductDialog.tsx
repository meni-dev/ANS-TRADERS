import { describeError } from '@/lib/api/errors'
import { DialogHeader } from '@/components/feedback/DialogHeader'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { FormErrorSummary } from '@/components/form/FormErrorSummary'
import { RHFNumberField } from '@/components/form/RHFNumberField'
import { zodResolver } from '@hookform/resolvers/zod'
import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined'
import { Alert, Box, Button, Dialog, DialogActions, DialogContent, Grid } from '@mui/material'
import { useState } from 'react'
import { FormProvider, useForm } from 'react-hook-form'
import { useCreateProduct } from '../hooks'
import { createProductSchema, type CreateProductFormValues } from '../types'
import { FormSection } from '@/components/form/FormSection'
import { ProductFormFields } from './ProductFormFields'

const defaultValues: CreateProductFormValues = {
  itemCode: '',
  partNumber: '',
  itemName: '',
  description: '',
  vehicleBrand: '',
  vehicleModel: '',
  hsn: '',
  gstRate: 18,
  uqc: 'PCS',
  supplyType: 'Taxable' as const,
  purchaseRate: 0,
  sellingRate: 0,
  mrp: 0,
  openingStock: 0,
  reorderLevel: 0,
}

type CreateProductDialogProps = {
  open: boolean
  onClose: () => void
}

export function CreateProductDialog({ open, onClose }: CreateProductDialogProps) {
  const { notify } = useNotification()
  const [serverError, setServerError] = useState<string | null>(null)
  const form = useForm<CreateProductFormValues>({
    resolver: zodResolver(createProductSchema),
    defaultValues,
  })
  const createProduct = useCreateProduct()

  const handleClose = () => {
    if (createProduct.isPending) return
    form.reset(defaultValues)
    setServerError(null)
    onClose()
  }

  const onSubmit = form.handleSubmit(async (values) => {
    setServerError(null)
    try {
      await createProduct.mutateAsync(values)
      notify(`Product "${values.itemName}" created`)
      form.reset(defaultValues)
      onClose()
    } catch (error) {
      setServerError(describeError(error, 'Something went wrong. Please try again.'))
    }
  })

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
      <DialogHeader
        title="Add Product"
        subtitle="Create a new item in the spare parts master."
        icon={<Inventory2OutlinedIcon sx={{ fontSize: 20 }} />}
        onClose={handleClose}
        disabled={createProduct.isPending}
      />

      <FormProvider {...form}>
        {/* noValidate hands validation to zod: the browser would otherwise stop at the first
            empty required input, hiding every other error from the user. */}
        <form
          onSubmit={onSubmit}
          noValidate
          // Lets the content pane be the only scrolling region, so the header above and the
          // action bar below stay put however long the form gets.
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

            <FormSection title="Stock" caption="What is on the shelf now, and when to reorder.">
              <Grid container spacing={2}>
                <Grid size={{ xs: 6, sm: 3 }}>
                  <RHFNumberField
                    name="openingStock"
                    label="Opening Stock"
                    required
                    helperText="Set once, at creation"
                  />
                </Grid>
                <Grid size={{ xs: 6, sm: 3 }}>
                  <RHFNumberField
                    name="reorderLevel"
                    label="Reorder Level"
                    helperText="Flags the item as low"
                  />
                </Grid>
              </Grid>
            </FormSection>
          </DialogContent>

          <DialogActions sx={{ px: 3, py: 2, gap: 1, flexShrink: 0 }}>
            <Box sx={{ flexGrow: 1 }} />
            <Button onClick={handleClose} disabled={createProduct.isPending} variant="outlined">
              Cancel
            </Button>
            <Button type="submit" variant="contained" loading={createProduct.isPending}>
              Save Product
            </Button>
          </DialogActions>
        </form>
      </FormProvider>
    </Dialog>
  )
}
