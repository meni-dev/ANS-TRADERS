import { describeError } from '@/lib/api/errors'
import ShoppingCartOutlinedIcon from '@mui/icons-material/ShoppingCartOutlined'
import { PageHeader } from '@/components/layout/PageHeader'
import { DocumentTotals } from '@/components/document/DocumentTotals'
import { LineItemsEditor } from '@/components/document/LineItemsEditor'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { FormErrorSummary } from '@/components/form/FormErrorSummary'
import { FormSection } from '@/components/form/FormSection'
import { RHFNumberField } from '@/components/form/RHFNumberField'
import { RHFSelectField } from '@/components/form/RHFSelectField'
import { RHFTextField } from '@/components/form/RHFTextField'
import { useShopSettings } from '@/features/settings/hooks'
import { SupplierPicker } from '@/features/suppliers/components/SupplierPicker'
import type { SupplierDto } from '@/features/suppliers/types'
import { computeDocument, computeLine, isInterState as computeIsInterState } from '@/lib/documents/gst'
import { emptyLine, PAYMENT_MODES, type DocumentLineValues } from '@/lib/documents/types'
import { todayIso } from '@/lib/format'
import { zodResolver } from '@hookform/resolvers/zod'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import { Alert, Box, Button, Chip, Grid, Paper, Stack, Tooltip, Typography } from '@mui/material'
import { useState } from 'react'
import { FormProvider, useForm, useWatch } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { useCreatePurchase } from '../hooks'
import { createPurchaseSchema, type CreatePurchaseFormValues } from '../types'

const defaultValues: CreatePurchaseFormValues = {
  supplierId: '',
  supplierInvoiceNumber: '',
  invoiceDate: todayIso(),
  // Suppliers bill on account far more often than they are paid at the door, so credit is the
  // default and the amount-paid box starts empty.
  paymentMode: 'Credit',
  amountPaid: 0,
  notes: '',
  items: [{ ...emptyLine }],
}

export function PurchaseFormPage() {
  const navigate = useNavigate()
  const { notify } = useNotification()
  const { data: shop } = useShopSettings()
  const [serverError, setServerError] = useState<string | null>(null)
  const [supplier, setSupplier] = useState<SupplierDto | null>(null)

  const form = useForm<CreatePurchaseFormValues>({
    resolver: zodResolver(createPurchaseSchema),
    defaultValues,
  })

  const createPurchase = useCreatePurchase()

  const items = (useWatch({ control: form.control, name: 'items' }) ?? []) as DocumentLineValues[]
  const paymentMode = useWatch({ control: form.control, name: 'paymentMode' })
  const amountPaid = useWatch({ control: form.control, name: 'amountPaid' })

  const isInterState = computeIsInterState(shop?.stateCode, supplier?.stateCode)

  // Computed inline rather than memoised: useWatch hands back a fresh array on every keystroke, so
  // a dependency list on it would never hit, and the arithmetic is a handful of multiplications.
  const amounts = computeDocument(
    items.map((line) =>
      computeLine(
        {
          quantity: line?.quantity ?? 0,
          rate: line?.rate ?? 0,
          discountPercent: line?.discountPercent ?? 0,
          gstRate: line?.gstRate ?? 0,
        },
        isInterState,
      ),
    ),
  )

  const handleSupplierChange = (next: SupplierDto | null) => {
    setSupplier(next)
    form.setValue('supplierId', next?.id ?? '', { shouldValidate: form.formState.submitCount > 0 })
  }

  const onSubmit = form.handleSubmit(async (values) => {
    setServerError(null)
    try {
      const created = await createPurchase.mutateAsync(values)
      notify(`Purchase ${created.purchaseNumber} recorded`)
      navigate(`/purchases/${created.id}`, { replace: true })
    } catch (error) {
      setServerError(describeError(error, 'Something went wrong. Please try again.'))
    }
  })

  return (
    <Box>
      <PageHeader
        title="Record Purchase"
        icon={<ShoppingCartOutlinedIcon />}
        iconTone="violet"
        caption="Enter a supplier's bill. The purchase number is assigned when you save."
        onBack={() => navigate('/purchases')}
      />

      <FormProvider {...form}>
        {/* noValidate hands validation to zod — see the note in CreateProductDialog. */}
        <form onSubmit={onSubmit} noValidate>
          {serverError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {serverError}
            </Alert>
          )}
          <FormErrorSummary />

          <FormSection title="Supplier & Bill" caption="Who supplied the goods, and against which bill.">
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, md: 5 }}>
                <SupplierPicker
                  value={supplier}
                  onChange={handleSupplierChange}
                  error={form.formState.errors.supplierId?.message}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6, md: 4 }}>
                <RHFTextField
                  name="supplierInvoiceNumber"
                  label="Supplier Bill No."
                  required
                  placeholder="As printed on their bill"
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                <RHFTextField
                  name="invoiceDate"
                  label="Bill Date"
                  type="date"
                  required
                />
              </Grid>
            </Grid>

            {supplier && (
              <Stack
                direction="row"
                spacing={1}
                sx={{ mt: 2, alignItems: 'center', flexWrap: 'wrap', rowGap: 1 }}
              >
                <Chip
                  size="small"
                  label={isInterState ? 'Inter-state · IGST' : 'Intra-state · CGST + SGST'}
                  sx={{ bgcolor: 'primary.light', color: 'primary.dark' }}
                />
                <Typography sx={{ fontSize: 12.5, color: 'text.secondary' }}>
                  {supplier.gstin ? `GSTIN ${supplier.gstin}` : 'No GSTIN on file'}
                  {supplier.state && ` · ${supplier.state}`}
                </Typography>
                <Tooltip
                  title={`Decided by comparing the supplier's state code with yours (${shop?.stateCode ?? '—'}).`}
                >
                  <InfoOutlinedIcon sx={{ fontSize: 15, color: 'text.disabled' }} />
                </Tooltip>
              </Stack>
            )}
          </FormSection>

          <FormSection title="Items" caption="Rates default to the item master's purchase rate — edit if the bill differs.">
            <LineItemsEditor rateSource="purchaseRate" isInterState={isInterState} />
          </FormSection>

          <Grid container spacing={2} sx={{ mb: 2 }}>
            <Grid size={{ xs: 12, md: 7 }}>
              <Paper variant="outlined" sx={{ p: 2.5, borderRadius: '8px', height: '100%' }}>
                <Typography variant="overline" sx={{ color: 'text.secondary', display: 'block', mb: 2 }}>
                  Payment
                </Typography>
                <Grid container spacing={2}>
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <RHFSelectField name="paymentMode" label="Payment Mode" options={[...PAYMENT_MODES]} />
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <RHFNumberField
                      name="amountPaid"
                      label="Amount Paid"
                      helperText={
                        paymentMode === 'Credit'
                          ? 'Leave at zero if nothing was paid yet'
                          : 'Enter what you actually settled'
                      }
                    />
                  </Grid>
                  <Grid size={12}>
                    <RHFTextField
                      name="notes"
                      label="Notes"
                      multiline
                      minRows={2}
                      placeholder="Optional — transport, damages, anything worth remembering"
                    />
                  </Grid>
                </Grid>
              </Paper>
            </Grid>

            <Grid size={{ xs: 12, md: 5 }}>
              <Paper variant="outlined" sx={{ p: 2.5, borderRadius: '8px', height: '100%' }}>
                <Typography variant="overline" sx={{ color: 'text.secondary', display: 'block', mb: 2 }}>
                  Totals
                </Typography>
                <DocumentTotals
                  amounts={amounts}
                  isInterState={isInterState}
                  amountPaid={Number(amountPaid) || 0}
                />
                <Typography sx={{ fontSize: 11.5, color: 'text.disabled', mt: 2 }}>
                  Figures are recalculated on the server when you save.
                </Typography>
              </Paper>
            </Grid>
          </Grid>

          <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
            <Button variant="outlined" onClick={() => navigate('/purchases')} disabled={createPurchase.isPending}>
              Cancel
            </Button>
            <Button type="submit" variant="contained" loading={createPurchase.isPending}>
              Save Purchase
            </Button>
          </Stack>
        </form>
      </FormProvider>
    </Box>
  )
}
