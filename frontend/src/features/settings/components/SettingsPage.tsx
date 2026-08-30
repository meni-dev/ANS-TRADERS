import { describeError } from '@/lib/api/errors'
import SettingsOutlinedIcon from '@mui/icons-material/SettingsOutlined'
import { PageHeader } from '@/components/layout/PageHeader'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { FormErrorSummary } from '@/components/form/FormErrorSummary'
import { FormSection } from '@/components/form/FormSection'
import { RHFSelectField } from '@/components/form/RHFSelectField'
import { RHFTextField } from '@/components/form/RHFTextField'
import { INDIAN_STATES } from '@/lib/indianStates'
import { zodResolver } from '@hookform/resolvers/zod'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import { Alert, Box, Button, CircularProgress, Grid, Stack, Tooltip, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import { FormProvider, useForm, useWatch } from 'react-hook-form'
import { useShopSettings, useUpdateShopSettings } from '../hooks'
import { shopSettingsSchema, type ShopSettingsDto, type ShopSettingsFormValues } from '../types'
import { BooksLockCard } from './BooksLockCard'
import { TemplatePicker } from './TemplatePicker'

function toFormValues(settings: ShopSettingsDto): ShopSettingsFormValues {
  return {
    name: settings.name,
    legalName: settings.legalName ?? '',
    gstin: settings.gstin ?? '',
    stateCode: settings.stateCode,
    state: settings.state,
    addressLine1: settings.addressLine1 ?? '',
    addressLine2: settings.addressLine2 ?? '',
    city: settings.city ?? '',
    pincode: settings.pincode ?? '',
    phone: settings.phone ?? '',
    email: settings.email ?? '',
    invoiceFooter: settings.invoiceFooter ?? '',
    bankDetails: settings.bankDetails ?? '',
    invoiceTerms: settings.invoiceTerms ?? '',
    invoiceTemplate: settings.invoiceTemplate,
    booksStartFrom: settings.booksStartFrom,
  }
}

export function SettingsPage() {
  const { notify } = useNotification()
  const { data: settings, isLoading, isError } = useShopSettings()
  const updateSettings = useUpdateShopSettings()
  const [serverError, setServerError] = useState<string | null>(null)

  const form = useForm<ShopSettingsFormValues>({
    resolver: zodResolver(shopSettingsSchema),
    defaultValues: settings ? toFormValues(settings) : undefined,
  })

  // The form is mounted before the fetch resolves, so it is filled in once the row arrives.
  useEffect(() => {
    if (settings) form.reset(toFormValues(settings))
  }, [settings, form])

  const values = useWatch({ control: form.control }) as Partial<ShopSettingsFormValues>

  if (isLoading) {
    return (
      <Box sx={{ display: 'grid', placeItems: 'center', minHeight: 320 }}>
        <CircularProgress size={28} />
      </Box>
    )
  }

  if (isError || !settings) {
    return <Alert severity="error">Settings could not be loaded. Check that the API is running.</Alert>
  }

  const onSubmit = form.handleSubmit(async (submitted) => {
    setServerError(null)
    try {
      await updateSettings.mutateAsync(submitted)
      notify('Settings saved')
    } catch (error) {
      setServerError(describeError(error, 'Something went wrong. Please try again.'))
    }
  })

  // Previews use what is typed rather than what is saved, so editing the address updates the
  // sample bill as the user goes.
  const previewShop: ShopSettingsDto = {
    ...settings,
    name: values.name || settings.name,
    legalName: values.legalName || null,
    gstin: values.gstin || null,
    stateCode: values.stateCode || settings.stateCode,
    state: values.state || settings.state,
    addressLine1: values.addressLine1 || null,
    addressLine2: values.addressLine2 || null,
    city: values.city || null,
    pincode: values.pincode || null,
    phone: values.phone || null,
    email: values.email || null,
    invoiceFooter: values.invoiceFooter || null,
    bankDetails: values.bankDetails || null,
    invoiceTerms: values.invoiceTerms || null,
  }

  const stateOptions = INDIAN_STATES.map((s) => ({ value: s.name, label: `${s.code} · ${s.name}` }))

  return (
    <Box>
      <PageHeader
        title="Settings"
        icon={<SettingsOutlinedIcon />}
        iconTone="neutral"
        caption="Your shop's details and how a printed bill looks."
      />

      <FormProvider {...form}>
        <form onSubmit={onSubmit} noValidate>
          {serverError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {serverError}
            </Alert>
          )}
          <FormErrorSummary />

          <FormSection title="Shop Details" caption="Printed as the seller header on every bill.">
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, md: 6 }}>
                <RHFTextField name="name" label="Shop Name" required />
              </Grid>
              <Grid size={{ xs: 12, md: 6 }}>
                <RHFTextField
                  name="legalName"
                  label="Legal Name"
                  helperText="Only if it differs from the trading name"
                />
              </Grid>

              <Grid size={{ xs: 12, sm: 6 }}>
                <RHFTextField name="gstin" label="GSTIN" placeholder="33AAECS1234F1Z8" />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <RHFTextField name="phone" label="Phone" />
              </Grid>

              <Grid size={12}>
                <RHFTextField name="addressLine1" label="Address Line 1" />
              </Grid>
              <Grid size={12}>
                <RHFTextField name="addressLine2" label="Address Line 2" />
              </Grid>

              <Grid size={{ xs: 12, sm: 4 }}>
                <RHFTextField name="city" label="City" />
              </Grid>
              <Grid size={{ xs: 6, sm: 4 }}>
                <RHFTextField name="pincode" label="Pincode" />
              </Grid>
              <Grid size={{ xs: 6, sm: 4 }}>
                <RHFTextField name="email" label="Email" />
              </Grid>
            </Grid>
          </FormSection>

          <FormSection
            title="Place of Business"
            caption="This decides whether a bill is taxed as IGST or as CGST + SGST."
          >
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 8 }}>
                <RHFSelectField
                  name="state"
                  label="State"
                  options={stateOptions}
                  required
                  onChange={(event) => {
                    // The code is what the tax rule compares, so it is kept in step with the name
                    // rather than being typed separately and allowed to disagree.
                    const picked = INDIAN_STATES.find((s) => s.name === event.target.value)
                    form.setValue('state', event.target.value as string)
                    if (picked) form.setValue('stateCode', picked.code, { shouldValidate: true })
                  }}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                <RHFTextField
                  name="stateCode"
                  label="State Code"
                  required
                  slotProps={{ input: { readOnly: true } }}
                  helperText="Set by the state above"
                />
              </Grid>
            </Grid>

            <Grid container spacing={2} sx={{ mt: 0 }}>
              <Grid size={{ xs: 12, md: 6 }}>
                <RHFTextField
                  name="booksStartFrom"
                  label="Books Begin From"
                  type="date"
                  helperText="Nothing can be dated before this. Leave empty and one mistyped year opens a closed financial year"
                />
              </Grid>
            </Grid>

            <Stack direction="row" spacing={1} sx={{ alignItems: 'flex-start', mt: 2 }}>
              <InfoOutlinedIcon sx={{ fontSize: 16, color: 'text.disabled', mt: 0.25 }} />
              <Typography sx={{ fontSize: 12.5, color: 'text.secondary' }}>
                Changing this only affects bills raised from now on. Every invoice and purchase
                already recorded keeps the tax split it was issued with.
              </Typography>
            </Stack>
          </FormSection>

          <FormSection
            title="Invoice Template"
            caption="Pick the layout printed bills use. Every one of these is a complete tax invoice."
          >
            <TemplatePicker
              value={values.invoiceTemplate ?? settings.invoiceTemplate}
              onChange={(id) => form.setValue('invoiceTemplate', id, { shouldDirty: true })}
              shop={previewShop}
            />
          </FormSection>

          <FormSection
            title="Printed on the Bill"
            caption="Optional blocks. Templates that have room for them will print them."
            collapsible
          >
            <Grid container spacing={2}>
              <Grid size={12}>
                <RHFTextField
                  name="invoiceFooter"
                  label="Footer Note"
                  multiline
                  minRows={2}
                  helperText="Printed by every template — a returns policy, a jurisdiction note"
                />
              </Grid>
              <Grid size={{ xs: 12, md: 6 }}>
                <RHFTextField
                  name="bankDetails"
                  label="Bank Details"
                  multiline
                  minRows={3}
                  helperText="Detailed and Traditional print this"
                />
              </Grid>
              <Grid size={{ xs: 12, md: 6 }}>
                <RHFTextField
                  name="invoiceTerms"
                  label="Terms & Conditions"
                  multiline
                  minRows={3}
                  helperText="Detailed and Traditional print this"
                />
              </Grid>
            </Grid>
          </FormSection>

          <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
            <Tooltip title="Undo everything not yet saved">
              <Box component="span">
                <Button
                  variant="outlined"
                  onClick={() => form.reset(toFormValues(settings))}
                  disabled={updateSettings.isPending || !form.formState.isDirty}
                >
                  Discard changes
                </Button>
              </Box>
            </Tooltip>
            <Button type="submit" variant="contained" loading={updateSettings.isPending}>
              Save Settings
            </Button>
          </Stack>
        </form>
      </FormProvider>

      <Box sx={{ mt: 3 }}>
        <BooksLockCard settings={settings} />
      </Box>
    </Box>
  )
}
