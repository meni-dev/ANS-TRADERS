import { describeError } from '@/lib/api/errors'
import PointOfSaleOutlinedIcon from '@mui/icons-material/PointOfSaleOutlined'
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
import { CustomerPicker } from '@/features/customers/components/CustomerPicker'
import { CustomerCreditStrip } from '@/features/payments/components/CustomerCreditStrip'
import type { CustomerDto } from '@/features/customers/types'
import { applyBillDiscount, computeDocument, isInterState as computeIsInterState } from '@/lib/documents/gst'
import { emptyLine, PAYMENT_MODES, type DocumentLineValues } from '@/lib/documents/types'
import { todayIso } from '@/lib/format'
import { zodResolver } from '@hookform/resolvers/zod'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import { Alert, Box, Button, Chip, Grid, Paper, Stack, Tooltip, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import { FormProvider, useForm, useWatch } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { useCreateInvoice } from '../hooks'
import { createInvoiceSchema, type CreateInvoiceFormValues } from '../types'

const defaultValues: CreateInvoiceFormValues = {
  customerId: '',
  walkInName: '',
  invoiceDate: todayIso(),
  // Counter sales are settled on the spot far more often than not.
  paymentMode: 'Cash',
  amountPaid: 0,
  billDiscountAmount: 0,
  notes: '',
  items: [{ ...emptyLine }],
}

export function InvoiceFormPage() {
  const navigate = useNavigate()
  const { notify } = useNotification()
  const { data: shop } = useShopSettings()
  const [serverError, setServerError] = useState<string | null>(null)
  const [customer, setCustomer] = useState<CustomerDto | null>(null)

  const form = useForm<CreateInvoiceFormValues>({
    resolver: zodResolver(createInvoiceSchema),
    defaultValues,
  })

  const createInvoice = useCreateInvoice()

  const items = (useWatch({ control: form.control, name: 'items' }) ?? []) as DocumentLineValues[]
  const billDiscountAmount = useWatch({ control: form.control, name: 'billDiscountAmount' })
  const paymentMode = useWatch({ control: form.control, name: 'paymentMode' })
  const amountPaid = useWatch({ control: form.control, name: 'amountPaid' })

  const isInterState = computeIsInterState(shop?.stateCode, customer?.stateCode)

  // Computed inline rather than memoised: useWatch hands back a fresh array on every keystroke, so
  // a dependency list on it would never hit, and the arithmetic is a handful of multiplications.
  const amounts = computeDocument(
    applyBillDiscount(
      items.map((line) => ({
        quantity: line?.quantity ?? 0,
        rate: line?.rate ?? 0,
        discountPercent: line?.discountPercent ?? 0,
        gstRate: line?.gstRate ?? 0,
      })),
      Number(billDiscountAmount) || 0,
      isInterState,
    ),
  )

  const isCredit = paymentMode === 'Credit'

  // Anything but credit is settled in full at the counter, and the server enforces that. Keeping
  // the box in step means the totals panel never shows a balance the saved invoice will not have.
  useEffect(() => {
    if (!isCredit) {
      form.setValue('amountPaid', amounts.grandTotal)
    }
  }, [isCredit, amounts.grandTotal, form])

  const handleCustomerChange = (next: CustomerDto | null) => {
    setCustomer(next)
    form.setValue('customerId', next?.id ?? '')
    // Two names on one bill is ambiguous — an account customer supersedes whatever was typed.
    if (next) form.setValue('walkInName', '')
  }

  const onSubmit = form.handleSubmit(async (values) => {
    setServerError(null)
    try {
      const created = await createInvoice.mutateAsync(values)
      notify(`Invoice ${created.invoiceNumber} issued`)
      navigate(`/billing/${created.id}`, { replace: true })
    } catch (error) {
      setServerError(describeError(error, 'Something went wrong. Please try again.'))
    }
  })

  return (
    <Box>
      <PageHeader
        title="New Invoice"
        icon={<PointOfSaleOutlinedIcon />}
        iconTone="blue"
        caption="The invoice number is assigned when you save, and cannot be changed afterwards."
        onBack={() => navigate('/billing')}
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

          <FormSection
            title="Bill To"
            caption="Pick a saved customer, or just type a name for a walk-in."
          >
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, md: 5 }}>
                <CustomerPicker
                  value={customer}
                  onChange={handleCustomerChange}
                  helperText="Leave empty for a walk-in"
                />
                {/* Warns, never blocks: who gets credit is the owner's call, and a screen that
                    refuses him is a screen he learns to work around. */}
                <CustomerCreditStrip
                  customerId={customer?.id}
                  currentBillTotal={amounts.grandTotal}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6, md: 4 }}>
                <RHFTextField
                  name="walkInName"
                  label="Walk-in Name"
                  disabled={!!customer}
                  placeholder="Name on the bill"
                  helperText={customer ? 'Using the saved customer above' : undefined}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                <RHFTextField
                  name="invoiceDate"
                  label="Invoice Date"
                  type="date"
                  required
                />
              </Grid>
            </Grid>

            <Stack direction="row" spacing={1} sx={{ mt: 2, alignItems: 'center', flexWrap: 'wrap', rowGap: 1 }}>
              <Chip
                size="small"
                label={isInterState ? 'Inter-state · IGST' : 'Intra-state · CGST + SGST'}
                sx={{ bgcolor: 'primary.light', color: 'primary.dark' }}
              />
              <Typography sx={{ fontSize: 12.5, color: 'text.secondary' }}>
                {customer?.gstin
                  ? `GSTIN ${customer.gstin}`
                  : 'Unregistered — B2C supply, no GSTIN on the bill'}
                {customer?.state && ` · ${customer.state}`}
              </Typography>
              <Tooltip
                title={`Decided by comparing the customer's state code with yours (${shop?.stateCode ?? '—'}). A walk-in with no state on file is billed as local.`}
              >
                <InfoOutlinedIcon sx={{ fontSize: 15, color: 'text.disabled' }} />
              </Tooltip>
            </Stack>
          </FormSection>

          <FormSection title="Items" caption="Rates default to the item master's selling rate. You can only bill what is in stock.">
            <LineItemsEditor rateSource="sellingRate" isInterState={isInterState} showStock />
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
                    {/* Spread across the lines before tax, so GST is charged on what was actually
                        taken — see applyBillDiscount. */}
                    <RHFNumberField
                      name="billDiscountAmount"
                      label="Discount on the bill"
                      helperText="A flat amount off the whole bill"
                    />
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <RHFNumberField
                      name="amountPaid"
                      label="Amount Received"
                      disabled={!isCredit}
                      helperText={
                        isCredit
                          ? 'Part payment is fine — the rest goes on account'
                          : 'Settled in full at the counter'
                      }
                    />
                  </Grid>
                  <Grid size={12}>
                    <RHFTextField
                      name="notes"
                      label="Notes"
                      multiline
                      minRows={2}
                      placeholder="Optional — printed on the bill"
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
            <Button variant="outlined" onClick={() => navigate('/billing')} disabled={createInvoice.isPending}>
              Cancel
            </Button>
            <Button type="submit" variant="contained" loading={createInvoice.isPending}>
              Issue Invoice
            </Button>
          </Stack>
        </form>
      </FormProvider>
    </Box>
  )
}
