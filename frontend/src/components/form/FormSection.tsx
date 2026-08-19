import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import { Box, Collapse, Paper, Stack, Typography } from '@mui/material'
import { useState, type ReactNode } from 'react'

type FormSectionProps = {
  title: string
  caption?: string
  children: ReactNode
  /** Renders the section as a click-to-expand block, collapsed by default. */
  collapsible?: boolean
}

/**
 * One titled white card per group of related fields. Chunking a long form into labelled cards
 * gives the eye a place to rest and makes the required/optional split obvious — a flat run of
 * inputs under one heading reads as a wall.
 */
export function FormSection({ title, caption, children, collapsible }: FormSectionProps) {
  const [open, setOpen] = useState(!collapsible)

  const header = (
    <Stack direction="row" spacing={1} sx={{ alignItems: 'center', width: '100%' }}>
      <Box sx={{ flexGrow: 1, minWidth: 0, textAlign: 'left' }}>
        <Typography variant="overline" sx={{ color: 'text.secondary', display: 'block' }}>
          {title}
        </Typography>
        {caption && (
          <Typography variant="caption" sx={{ color: 'text.disabled', display: 'block', mt: -0.25 }}>
            {caption}
          </Typography>
        )}
      </Box>
      {collapsible && (
        <ExpandMoreIcon
          sx={{
            fontSize: 20,
            color: 'text.disabled',
            transition: 'transform 180ms ease',
            transform: open ? 'rotate(180deg)' : 'none',
          }}
        />
      )}
    </Stack>
  )

  return (
    <Paper variant="outlined" sx={{ p: 2.5, mb: 2, borderRadius: '8px' }}>
      {collapsible ? (
        <Box
          component="button"
          type="button"
          onClick={() => setOpen((prev) => !prev)}
          aria-expanded={open}
          sx={{
            display: 'flex',
            width: '100%',
            border: 'none',
            background: 'none',
            p: 0,
            font: 'inherit',
            cursor: 'pointer',
            color: 'inherit',
          }}
        >
          {header}
        </Box>
      ) : (
        <Box sx={{ mb: 2 }}>{header}</Box>
      )}

      {collapsible ? (
        <Collapse in={open} timeout={180} unmountOnExit>
          <Box sx={{ pt: 2 }}>{children}</Box>
        </Collapse>
      ) : (
        children
      )}
    </Paper>
  )
}
