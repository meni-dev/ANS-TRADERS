import { INVOICE_TEMPLATES, type InvoiceTemplateOption } from '@/features/billing/templates'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import OpenInFullIcon from '@mui/icons-material/OpenInFull'
import { Box, Chip, Dialog, DialogContent, Grid, IconButton, Stack, Tooltip, Typography } from '@mui/material'
import { useState } from 'react'
import type { ShopSettingsDto } from '../types'
import { SAMPLE_INVOICE } from './sampleInvoice'
import { DialogHeader } from '@/components/feedback/DialogHeader'

// The templates lay out at roughly a page width; the card shows them shrunk to fit.
const SHEET_WIDTH = 900
const CARD_WIDTH = 300
const CARD_HEIGHT = 330
const SCALE = CARD_WIDTH / SHEET_WIDTH

function Preview({
  option,
  shop,
  width,
  height,
  scale,
}: {
  option: InvoiceTemplateOption
  shop: ShopSettingsDto
  width: number
  height: number
  scale: number
}) {
  const Template = option.component

  return (
    <Box
      className="template-preview"
      sx={{
        width,
        height,
        overflow: 'hidden',
        position: 'relative',
        bgcolor: '#fff',
        // The preview is a picture of a bill, not a bill. Nothing inside it should be clickable,
        // and the card underneath owns the click.
        pointerEvents: 'none',
      }}
    >
      <Box sx={{ width: SHEET_WIDTH, transform: `scale(${scale})`, transformOrigin: 'top left' }}>
        <Template invoice={SAMPLE_INVOICE} shop={shop} />
      </Box>
    </Box>
  )
}

type TemplatePickerProps = {
  value: string
  onChange: (id: string) => void
  /** The live settings, so a preview shows the shop's own name and address rather than a placeholder. */
  shop: ShopSettingsDto
}

/**
 * Five layouts, each shown as a real render of a sample bill. A name and a sentence would make the
 * shopkeeper guess; a picture of the thing they are choosing does not.
 */
export function TemplatePicker({ value, onChange, shop }: TemplatePickerProps) {
  const [enlarged, setEnlarged] = useState<InvoiceTemplateOption | null>(null)

  // Previews render the shop's real details but always the sample bill, so the choice is not
  // affected by whichever invoice happened to be open.
  const previewShop = { ...shop, invoiceTemplate: value } as ShopSettingsDto

  return (
    <>
      <Grid container spacing={2}>
        {INVOICE_TEMPLATES.map((option) => {
          const selected = option.id === value

          return (
            <Grid key={option.id} size={{ xs: 12, sm: 6, lg: 4 }}>
              <Box
                component="button"
                type="button"
                onClick={() => onChange(option.id)}
                aria-pressed={selected}
                sx={{
                  width: '100%',
                  p: 0,
                  border: '2px solid',
                  borderColor: selected ? 'primary.main' : 'grey.300',
                  borderRadius: '10px',
                  overflow: 'hidden',
                  bgcolor: 'background.paper',
                  cursor: 'pointer',
                  font: 'inherit',
                  textAlign: 'left',
                  display: 'block',
                  transition: 'border-color 120ms ease, box-shadow 120ms ease',
                  '&:hover': { borderColor: selected ? 'primary.main' : 'grey.500' },
                }}
              >
                <Box sx={{ position: 'relative', bgcolor: 'grey.100', p: 1 }}>
                  <Box
                    sx={{
                      border: '1px solid',
                      borderColor: 'grey.300',
                      borderRadius: '4px',
                      overflow: 'hidden',
                      // A faint fade at the bottom says the sheet continues past the card rather
                      // than ending in a hard cut.
                      position: 'relative',
                      '&::after': {
                        content: '""',
                        position: 'absolute',
                        left: 0,
                        right: 0,
                        bottom: 0,
                        height: 48,
                        background: 'linear-gradient(to bottom, rgba(255,255,255,0), #fff)',
                      },
                    }}
                  >
                    <Preview
                      option={option}
                      shop={previewShop}
                      width={CARD_WIDTH}
                      height={CARD_HEIGHT}
                      scale={SCALE}
                    />
                  </Box>

                  <Tooltip title="See it full size">
                    <IconButton
                      size="small"
                      component="span"
                      onClick={(e) => {
                        e.stopPropagation()
                        setEnlarged(option)
                      }}
                      sx={{
                        position: 'absolute',
                        top: 14,
                        right: 14,
                        bgcolor: 'background.paper',
                        border: '1px solid',
                        borderColor: 'grey.300',
                        '&:hover': { bgcolor: 'grey.100' },
                      }}
                    >
                      <OpenInFullIcon sx={{ fontSize: 15 }} />
                    </IconButton>
                  </Tooltip>
                </Box>

                <Box sx={{ p: 1.75, borderTop: '1px solid', borderColor: 'grey.200' }}>
                  <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 0.5 }}>
                    <Typography sx={{ fontSize: 14, fontWeight: 700 }}>{option.name}</Typography>
                    <Chip
                      label={option.paper}
                      size="small"
                      sx={{ bgcolor: 'grey.100', color: 'text.secondary' }}
                    />
                    <Box sx={{ flexGrow: 1 }} />
                    {selected && (
                      <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', color: 'primary.main' }}>
                        <CheckCircleIcon sx={{ fontSize: 17 }} />
                        <Typography sx={{ fontSize: 12, fontWeight: 700 }}>In use</Typography>
                      </Stack>
                    )}
                  </Stack>
                  <Typography sx={{ fontSize: 12.5, color: 'text.secondary', lineHeight: 1.5 }}>
                    {option.description}
                  </Typography>
                </Box>
              </Box>
            </Grid>
          )
        })}
      </Grid>

      <Dialog open={!!enlarged} onClose={() => setEnlarged(null)} maxWidth="lg" fullWidth>
        {enlarged && (
          <>
            <DialogHeader
              title={`${enlarged.name} template`}
              subtitle={`${enlarged.description} · ${enlarged.paper}`}
              onClose={() => setEnlarged(null)}
            />
            <DialogContent dividers sx={{ bgcolor: 'grey.100', p: 3 }}>
              <Box sx={{ bgcolor: '#fff', mx: 'auto', width: SHEET_WIDTH, maxWidth: '100%', overflowX: 'auto' }}>
                <Box sx={{ width: SHEET_WIDTH, pointerEvents: 'none' }}>
                  <enlarged.component invoice={SAMPLE_INVOICE} shop={previewShop} />
                </Box>
              </Box>
            </DialogContent>
          </>
        )}
      </Dialog>
    </>
  )
}
