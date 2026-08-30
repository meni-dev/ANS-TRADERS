import { visibleQuickActions } from '@/components/layout/quickActions'
import { useAuth } from '@/features/auth/AuthProvider'
import { accent } from '@/theme/theme'
import { Box, Button, Stack } from '@mui/material'
import { useNavigate } from 'react-router-dom'

/**
 * The day's work, one click from the first screen anybody opens.
 * <p>
 * The same actions live behind ⌘K and behind the app bar's New button. That repetition is
 * deliberate: the palette is for whoever already knows the shortcut, this strip is for whoever has
 * been shown the app once and is looking for the button.
 * </p>
 * <p>
 * Filtered by permission, so a counter hand is never offered Day Close. An action that would be
 * refused is worse than a missing one — it teaches people to expect refusals.
 * </p>
 */
export function QuickActionsBar() {
  const { can } = useAuth()
  const navigate = useNavigate()

  const actions = visibleQuickActions(can)
  if (actions.length === 0) return null

  return (
    // Scrolls sideways on a narrow screen rather than wrapping into a second row that pushes the
    // figures below the fold.
    <Box sx={{ overflowX: 'auto', pb: 0.5, mx: -0.5, px: 0.5 }}>
      <Stack direction="row" spacing={1} sx={{ width: 'max-content' }}>
        {actions.map((action, index) => (
          <Button
            key={action.path}
            size="small"
            variant={index === 0 ? 'contained' : 'outlined'}
            startIcon={
              <Box
                sx={{
                  display: 'flex',
                  // The lead action is filled, so its icon is already white; the rest carry their
                  // own hue, which is what makes five outlined buttons scannable.
                  color: index === 0 ? 'inherit' : accent[action.tone].solid,
                  '& svg': { fontSize: 17 },
                }}
              >
                {action.icon}
              </Box>
            }
            onClick={() => navigate(action.path)}
            sx={{
              whiteSpace: 'nowrap',
              ...(index > 0 && {
                bgcolor: '#fff',
                '&:hover': { bgcolor: accent[action.tone].tint, borderColor: accent[action.tone].solid },
              }),
            }}
          >
            {action.label}
          </Button>
        ))}
      </Stack>
    </Box>
  )
}
