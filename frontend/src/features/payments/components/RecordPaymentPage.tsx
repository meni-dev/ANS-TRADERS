import { describeError } from '@/lib/api/errors'
import PaymentsOutlinedIcon from '@mui/icons-material/PaymentsOutlined'
import { PageHeader } from '@/components/layout/PageHeader'
import { CustomerPicker } from '@/features/customers/components/CustomerPicker'
import type { CustomerDto } from '@/features/customers/types'
import { SupplierPicker } from '@/features/suppliers/components/SupplierPicker'
import type { SupplierDto } from '@/features/suppliers/types'
import { formatCurrency, formatDate, todayIso } from '@/lib/format'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  Alert,
  Box,
  Button,
  Divider,
  Grid,
  Paper,
  Stack,
  Typography,
} from '@mui/material'
import { useState } from 'react'
import { FormProvider, useForm, useWatch } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { RHFNumberField } from '@/components/form/RHFNumberField'
import { RHFSelectField } from '@/components/form/RHFSelectField'
import { RHFTextField } from '@/components/form/RHFTextField'
import { useOpenDocuments, useRecordPayment } from '../hooks'
import { recordPaymentSchema, type RecordPaymentFormValues } from '../types'

/**
 * Credit is missing on purpose. It appears on a bill to mean "nothing was tendered", which is the
 * one thing a receipt cannot record.
 */
const TENDER_MODES = [
  { value: 'Cash', label: 'Cash' },
  { value: 'Upi', label: 'UPI' },
  { value: 'Card', label: 'Card' },
  { value: 'BankTransfer', label: 'Bank Transfer' },
  { value: 'Cheque', label: 'Cheque' },
]

const defaultValues: RecordPaymentFormValues = {
  direction: 'Received',
  customerId: '',
  supplierId: '',
  paymentDate: todayIso(),
  amount: 0,
  mode: 'Cash',
  referenceNumber: '',
  notes: '',
  allocations: [],
  // The shop's own rule for money handed over "against my account".
  autoAllocateOldestFirst: true,
}

export function RecordPaymentPage() {
  const navigate = useNavigate()
  const [customer, setCustomer] = useState<CustomerDto | null>(null)
  const [supplier, setSupplier] = useState<SupplierDto | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)

  const form = useForm<RecordPaymentFormValues>({
    resolver: zodResolver(recordPaymentSchema),
    defaultValues,
  })

  const direction = useWatch({ control: form.control, name: 'direction' })
  const mode = useWatch({ control: form.control, name: 'mode' })
  const amount = useWatch({ control: form.control, name: 'amount' })
  const paymentDate = useWatch({ control: form.control, name: 'paymentDate' })
  const chequeDate = useWatch({ control: form.control, name: 'cheque.chequeDate' })

  const isReceipt = direction === 'Received'
  const party = isReceipt ? { customerId: customer?.id } : { supplierId: supplier?.id }
  const openDocuments = useOpenDocuments(party)
  const recordPayment = useRecordPayment()

  // A cheque the shop cannot bank yet settles nothing, and the form has to say so before the money
  // is entered — otherwise the balance not moving afterwards reads as a bug.
  const isPostDated = mode === 'Cheque' && Boolean(chequeDate) && chequeDate! > paymentDate

  const open = openDocuments.data ?? []
  const openTotal = open.reduce((sum, doc) => sum + doc.balanceDue, 0)

  function switchDirection(next: 'Received' | 'Paid') {
    setCustomer(null)
    setSupplier(null)
    form.setValue('direction', next)
    form.setValue('customerId', '')
    form.setValue('supplierId', '')
  }

  async function onSubmit(values: RecordPaymentFormValues) {
    setSubmitError(null)

    try {
      const payment = await recordPayment.mutateAsync({
        ...values,
        customerId: customer?.id,
        supplierId: supplier?.id,
        cheque: values.mode === 'Cheque' ? values.cheque : undefined,
      })

      navigate(`/accounts/payments?highlight=${payment.id}`)
    } catch (error) {
      setSubmitError(describeError(error, 'Could not record this payment'))
    }
  }

  return (
    <FormProvider {...form}>
      <Box component="form" onSubmit={form.handleSubmit(onSubmit)}>
        <Stack spacing={2.5}>
          <PageHeader
            title={isReceipt ? 'Record a receipt' : 'Record a payment'}
            icon={<PaymentsOutlinedIcon />}
            iconTone="teal"
            caption={isReceipt ? 'Money a customer has handed over' : 'Money paid to a supplier'}
            onBack={() => navigate('/accounts/payments')}
            flush
          />

          {submitError ? <Alert severity="error">{submitError}</Alert> : null}

          <Paper variant="outlined" sx={{ p: 2.5 }}>
            <Stack direction="row" spacing={1} sx={{ mb: 2.5 }}>
              <Button
                variant={isReceipt ? 'contained' : 'outlined'}
                onClick={() => switchDirection('Received')}
              >
                Received
              </Button>
              <Button
                variant={!isReceipt ? 'contained' : 'outlined'}
                onClick={() => switchDirection('Paid')}
              >
                Paid out
              </Button>
            </Stack>

            <Grid container spacing={2}>
              <Grid size={{ xs: 12, md: 5 }}>
                {isReceipt ? (
                  <CustomerPicker
                    value={customer}
                    onChange={(next) => {
                      setCustomer(next)
                      form.setValue('customerId', next?.id ?? '')
                    }}
                    error={form.formState.errors.customerId?.message}
                  />
                ) : (
                  <SupplierPicker
                    value={supplier}
                    onChange={(next) => {
                      setSupplier(next)
                      form.setValue('supplierId', next?.id ?? '')
                    }}
                    error={form.formState.errors.supplierId?.message}
                  />
                )}
              </Grid>
              <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                <RHFTextField
                  name="paymentDate"
                  label="Date"
                  type="date"
                  required
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6, md: 4 }}>
                <RHFNumberField name="amount" label="Amount" required />
              </Grid>

              <Grid size={{ xs: 12, sm: 6, md: 4 }}>
                <RHFSelectField name="mode" label="How it arrived" options={TENDER_MODES} required />
              </Grid>
              <Grid size={{ xs: 12, sm: 6, md: 4 }}>
                <RHFTextField
                  name="referenceNumber"
                  label="Reference"
                  placeholder="UPI ref, transfer no."
                />
              </Grid>
              <Grid size={{ xs: 12, md: 4 }}>
                <RHFTextField name="notes" label="Notes" />
              </Grid>

              {mode === 'Cheque' ? (
                <>
                  <Grid size={12}>
                    <Divider textAlign="left" sx={{ fontSize: 12.5, color: 'text.secondary' }}>
                      Cheque details
                    </Divider>
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                    <RHFTextField name="cheque.chequeNumber" label="Cheque number" required />
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                    <RHFTextField name="cheque.bankName" label="Drawn on (bank)" required />
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                    <RHFTextField
                      name="cheque.chequeDate"
                      label="Date on the cheque"
                      type="date"
                      required
                    />
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                    <RHFTextField
                      name="cheque.receivedOn"
                      label="Handed over on"
                      type="date"
                      required
                    />
                  </Grid>
                </>
              ) : null}
            </Grid>

            {isPostDated ? (
              <Alert severity="info" sx={{ mt: 2 }}>
                This cheque is dated {formatDate(chequeDate)}, so it cannot be banked yet. It will be
                recorded and shown in the register, but no bill is settled and the balance does not
                move until somebody banks it.
              </Alert>
            ) : null}
          </Paper>

          <Paper variant="outlined" sx={{ p: 2.5 }}>
            <Typography sx={{ fontWeight: 600, mb: 0.5 }}>What this settles</Typography>
            <Typography sx={{ fontSize: 13, color: 'text.secondary', mb: 1.5 }}>
              Open bills are settled oldest first. Anything left over stays on account and can be
              used against a later bill.
            </Typography>

            {open.length === 0 ? (
              <Typography sx={{ fontSize: 13.5, color: 'text.secondary' }}>
                {customer ?? supplier
                  ? 'Nothing open — the whole amount will stay on account.'
                  : 'Pick a party to see their open bills.'}
              </Typography>
            ) : (
              <Stack spacing={0.75}>
                {open.map((doc) => (
                  <Stack
                    key={doc.id}
                    direction="row"
                    spacing={2}
                   
                    sx={{ justifyContent: 'space-between', fontSize: 13.5 }}
                  >
                    <Typography sx={{ fontSize: 13.5, fontFamily: 'monospace' }}>
                      {doc.documentNumber}
                    </Typography>
                    <Typography sx={{ fontSize: 13, color: 'text.secondary', flex: 1 }}>
                      {formatDate(doc.dueDate ?? doc.documentDate)}
                      {doc.daysOld > 0 ? ` · ${doc.daysOld} days` : ''}
                    </Typography>
                    <Typography sx={{ fontSize: 13.5, fontVariantNumeric: 'tabular-nums' }}>
                      {formatCurrency(doc.balanceDue)}
                    </Typography>
                  </Stack>
                ))}
                <Divider sx={{ my: 0.75 }} />
                <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
                  <Typography sx={{ fontSize: 13.5, fontWeight: 600 }}>Open in total</Typography>
                  <Typography sx={{ fontSize: 13.5, fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
                    {formatCurrency(openTotal)}
                  </Typography>
                </Stack>
                {amount > openTotal ? (
                  <Alert severity="info" sx={{ mt: 1 }}>
                    {formatCurrency(amount - openTotal)} more than is owed — the remainder stays on
                    account as an advance.
                  </Alert>
                ) : null}
              </Stack>
            )}
          </Paper>

          <Stack direction="row" spacing={1.5} sx={{ justifyContent: 'flex-end' }}>
            <Button onClick={() => navigate('/accounts/payments')}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={recordPayment.isPending}>
              {recordPayment.isPending ? 'Recording…' : isReceipt ? 'Record receipt' : 'Record payment'}
            </Button>
          </Stack>
        </Stack>
      </Box>
    </FormProvider>
  )
}
