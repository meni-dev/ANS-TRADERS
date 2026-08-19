import { DialogHeader } from '@/components/feedback/DialogHeader'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { RHFNumberField } from '@/components/form/RHFNumberField'
import { RHFSelectField } from '@/components/form/RHFSelectField'
import { RHFTextField } from '@/components/form/RHFTextField'
import { ApiError } from '@/lib/api/client'
import { todayIso } from '@/lib/format'
import { zodResolver } from '@hookform/resolvers/zod'
import PaymentsOutlinedIcon from '@mui/icons-material/PaymentsOutlined'
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  Grid,
  Stack,
} from '@mui/material'
import { useState } from 'react'
import { FormProvider, useForm } from 'react-hook-form'
import { useCreateExpense } from '../hooks'
import {
  createExpenseSchema,
  EXPENSE_CATEGORIES,
  EXPENSE_MODES,
  type CreateExpenseFormValues,
} from '../types'

/** Money spent on running the shop, as opposed to money paid to a supplier for goods. */
export function RecordExpenseDialog({ onClose }: { onClose: () => void }) {
  const { notify } = useNotification()
  const [serverError, setServerError] = useState<string | null>(null)
  const createExpense = useCreateExpense()

  const form = useForm<CreateExpenseFormValues>({
    resolver: zodResolver(createExpenseSchema),
    defaultValues: {
      expenseDate: todayIso(),
      category: 'ShopExpenses',
      amount: 0,
      mode: 'Cash',
      referenceNumber: '',
      paidTo: '',
      notes: '',
    },
  })

  async function onSubmit(values: CreateExpenseFormValues) {
    setServerError(null)
    try {
      const expense = await createExpense.mutateAsync(values)
      notify(`${expense.expenseNumber} recorded`, 'success')
      onClose()
    } catch (caught) {
      setServerError(
        caught instanceof ApiError
          ? (Object.values(caught.errors).flat()[0] ?? caught.message)
          : 'Could not record that',
      )
    }
  }

  return (
    <Dialog open fullWidth maxWidth="sm" onClose={createExpense.isPending ? undefined : onClose}>
      <DialogHeader
        title="Record spend"
        subtitle="Rent, salary, electricity — what it costs to keep the shop open"
        icon={<PaymentsOutlinedIcon fontSize="small" />}
        onClose={onClose}
        disabled={createExpense.isPending}
      />
      <FormProvider {...form}>
        <Box component="form" onSubmit={form.handleSubmit(onSubmit)}>
          <DialogContent dividers>
            <Stack spacing={2}>
              {serverError ? <Alert severity="error">{serverError}</Alert> : null}

              <Grid container spacing={2}>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <RHFTextField
                    name="expenseDate"
                    label="Date"
                    type="date"
                    required
                    slotProps={{ inputLabel: { shrink: true } }}
                  />
                </Grid>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <RHFNumberField name="amount" label="Amount" required />
                </Grid>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <RHFSelectField
                    name="category"
                    label="What for"
                    options={EXPENSE_CATEGORIES.map((c) => ({ value: c.value, label: c.label }))}
                    required
                  />
                </Grid>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <RHFSelectField
                    name="mode"
                    label="How paid"
                    options={EXPENSE_MODES.map((m) => ({ value: m.value, label: m.label }))}
                    required
                  />
                </Grid>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <RHFTextField name="paidTo" label="Paid to" placeholder="Landlord, TNEB, staff" />
                </Grid>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <RHFTextField name="referenceNumber" label="Reference" placeholder="UPI ref, cheque no." />
                </Grid>
                <Grid size={12}>
                  <RHFTextField name="notes" label="Notes" />
                </Grid>
              </Grid>
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, py: 2 }}>
            <Button onClick={onClose} disabled={createExpense.isPending}>
              Cancel
            </Button>
            <Button type="submit" variant="contained" disabled={createExpense.isPending}>
              {createExpense.isPending ? 'Recording…' : 'Record'}
            </Button>
          </DialogActions>
        </Box>
      </FormProvider>
    </Dialog>
  )
}
