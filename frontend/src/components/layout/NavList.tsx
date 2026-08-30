import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import {
  Box,
  Collapse,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Paper,
  Popper,
  Tooltip,
  Typography,
} from '@mui/material'
import { useEffect, useRef, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { isNavPathActive, isNavRowActive, type NavItem } from './navConfig'

/** Small muted pill marking a section that isn't built yet. */
function SoonBadge() {
  return (
    <Box
      component="span"
      sx={{
        fontSize: 10,
        fontWeight: 700,
        letterSpacing: '0.04em',
        color: 'text.disabled',
        bgcolor: 'grey.100',
        borderRadius: '4px',
        px: 0.75,
        py: 0.125,
        lineHeight: 1.6,
      }}
    >
      SOON
    </Box>
  )
}

/** Shared by the expanded rail and the flyout, so an active row looks the same in both. */
const rowSx = (isActive: boolean, indented: boolean) => ({
  position: 'relative' as const,
  pl: indented ? 2.5 : 1.25,
  pr: 1.25,
  py: 0.375,
  minHeight: 30,
  mb: 0.125,
  color: isActive ? 'primary.dark' : 'text.secondary',
  '&:hover': {
    bgcolor: isActive ? 'primary.light' : 'grey.100',
    color: isActive ? 'primary.dark' : 'text.primary',
  },
  '&.Mui-disabled': { opacity: 1, color: 'text.disabled' },
  // Left accent bar on the active row. Reads faster than a colour change alone when the user is
  // scanning the rail from the far side of the screen.
  ...(isActive && {
    '&::before': {
      content: '""',
      position: 'absolute',
      left: -8,
      top: 5,
      bottom: 5,
      width: 3,
      borderRadius: '2px',
      bgcolor: 'primary.main',
    },
  }),
})

const labelSx = (isActive: boolean, small: boolean) => ({
  sx: {
    fontSize: small ? 12.5 : 13,
    fontWeight: isActive ? 600 : 500,
    color: 'inherit',
  },
})

function NavRow({
  item,
  depth,
  siblings,
}: {
  item: NavItem
  depth: number
  /** Its row group, so the most specific path among them can win. */
  siblings?: NavItem[]
}) {
  const navigate = useNavigate()
  const location = useLocation()

  const hasChildren = !!item.children?.length
  const isActive = isNavRowActive(item, siblings, location.pathname)
  const hasActiveChild = !!item.children?.some((c) => isNavPathActive(c.path, location.pathname))

  const [open, setOpen] = useState(hasActiveChild)

  // Navigating straight to a nested route (deep link, back button) must reveal the group that
  // contains it — otherwise the active row is hidden inside a collapsed parent.
  useEffect(() => {
    if (hasActiveChild) setOpen(true)
  }, [hasActiveChild])

  const handleClick = () => {
    if (hasChildren) {
      setOpen((prev) => !prev)
      return
    }
    if (item.path && !item.comingSoon) {
      navigate(item.path)
    }
  }

  const disabled = !!item.comingSoon && !hasChildren

  const row = (
    <ListItemButton
      onClick={handleClick}
      selected={isActive}
      disabled={disabled}
      disableRipple={disabled}
      sx={rowSx(isActive, depth > 0)}
    >
      <ListItemIcon
        sx={{ minWidth: 30, color: 'inherit', '& svg': { fontSize: depth === 0 ? 19 : 17 } }}
      >
        {item.icon}
      </ListItemIcon>
      <ListItemText primary={item.label} slotProps={{ primary: labelSx(isActive, depth > 0) }} />
      {item.comingSoon && !hasChildren && <SoonBadge />}
      {hasChildren && (
        <ExpandMoreIcon
          sx={{
            fontSize: 17,
            color: 'text.disabled',
            transition: 'transform 180ms ease',
            transform: open ? 'rotate(180deg)' : 'none',
          }}
        />
      )}
    </ListItemButton>
  )

  return (
    <>
      {disabled ? (
        <Tooltip title="Coming soon" placement="right">
          {/* A disabled button swallows pointer events, so the tooltip needs a live wrapper. */}
          <Box component="span" sx={{ display: 'block' }}>
            {row}
          </Box>
        </Tooltip>
      ) : (
        row
      )}

      {hasChildren && (
        <Collapse in={open} timeout={180} unmountOnExit>
          {/* Vertical guide line ties the children back to their parent. */}
          <Box
            sx={{
              position: 'relative',
              ml: 2.5,
              pl: 1,
              '&::before': {
                content: '""',
                position: 'absolute',
                left: 0,
                top: 4,
                bottom: 4,
                width: '1px',
                bgcolor: 'divider',
              },
            }}
          >
            <List disablePadding>
              {item.children!.filter((child) => !child.hidden).map((child) => (
                <NavRow key={child.label} item={child} depth={depth + 1} siblings={item.children} />
              ))}
            </List>
          </Box>
        </Collapse>
      )}
    </>
  )
}

/**
 * One row of the collapsed rail: the icon, and a flyout that carries everything the label would
 * have said.
 * <p>
 * The flyout is not decoration. Collapsed, a section's children have nowhere else to appear, so
 * without it half the app becomes unreachable the moment somebody narrows the sidebar.
 * </p>
 */
function RailRow({ item }: { item: NavItem }) {
  const navigate = useNavigate()
  const location = useLocation()
  const [anchor, setAnchor] = useState<HTMLElement | null>(null)
  const closeTimer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined)

  const children = item.children?.filter((child) => !child.hidden) ?? []
  const isActive =
    isNavPathActive(item.path, location.pathname) ||
    children.some((child) => isNavPathActive(child.path, location.pathname))

  // A short grace period, because the pointer has to cross a gap of page to reach the flyout and
  // closing on the first pixel of that gap makes the menu feel like it is running away.
  const openNow = (element: HTMLElement) => {
    clearTimeout(closeTimer.current)
    setAnchor(element)
  }
  const closeSoon = () => {
    closeTimer.current = setTimeout(() => setAnchor(null), 120)
  }

  useEffect(() => () => clearTimeout(closeTimer.current), [])

  const go = (path?: string) => {
    if (!path) return
    clearTimeout(closeTimer.current)
    setAnchor(null)
    navigate(path)
  }

  return (
    <Box onMouseEnter={(e) => openNow(e.currentTarget)} onMouseLeave={closeSoon}>
      <ListItemButton
        onClick={() => (children.length ? undefined : go(item.path))}
        onFocus={(e) => openNow(e.currentTarget)}
        selected={isActive}
        aria-label={item.label}
        sx={{
          justifyContent: 'center',
          px: 0,
          py: 0.75,
          mb: 0.25,
          minHeight: 34,
          color: isActive ? 'primary.dark' : 'text.secondary',
          bgcolor: isActive ? 'primary.light' : undefined,
          '&:hover': { bgcolor: isActive ? 'primary.light' : 'grey.100', color: 'text.primary' },
        }}
      >
        <ListItemIcon sx={{ minWidth: 0, color: 'inherit', '& svg': { fontSize: 20 } }}>
          {item.icon}
        </ListItemIcon>
      </ListItemButton>

      <Popper
        open={!!anchor}
        anchorEl={anchor}
        placement="right-start"
        // Sits above the drawer, which MUI puts at 1200.
        sx={{ zIndex: 1300 }}
        modifiers={[{ name: 'offset', options: { offset: [-4, 8] } }]}
      >
        <Paper
          elevation={2}
          onMouseEnter={() => clearTimeout(closeTimer.current)}
          onMouseLeave={closeSoon}
          sx={{ minWidth: 190, py: 0.75, px: 0.75, border: '1px solid', borderColor: 'divider' }}
        >
          {children.length === 0 ? (
            <Box
              onClick={() => go(item.path)}
              sx={{
                px: 1,
                py: 0.5,
                cursor: item.path ? 'pointer' : 'default',
                borderRadius: '5px',
                '&:hover': { bgcolor: 'grey.100' },
              }}
            >
              <Typography sx={{ fontSize: 13, fontWeight: 600 }}>{item.label}</Typography>
            </Box>
          ) : (
            <>
              <Typography
                sx={{
                  px: 1,
                  pb: 0.5,
                  fontSize: 11,
                  fontWeight: 700,
                  letterSpacing: '0.06em',
                  textTransform: 'uppercase',
                  color: 'text.disabled',
                }}
              >
                {item.label}
              </Typography>
              <List disablePadding>
                {children.map((child) => {
                  const childActive = isNavPathActive(child.path, location.pathname)
                  return (
                    <ListItemButton
                      key={child.label}
                      onClick={() => go(child.path)}
                      selected={childActive}
                      sx={{ ...rowSx(childActive, false), pl: 1, '&::before': undefined }}
                    >
                      <ListItemIcon
                        sx={{ minWidth: 28, color: 'inherit', '& svg': { fontSize: 17 } }}
                      >
                        {child.icon}
                      </ListItemIcon>
                      <ListItemText
                        primary={child.label}
                        slotProps={{ primary: labelSx(childActive, false) }}
                      />
                    </ListItemButton>
                  )
                })}
              </List>
            </>
          )}
        </Paper>
      </Popper>
    </Box>
  )
}

export function NavList({ items, collapsed }: { items: NavItem[]; collapsed?: boolean }) {
  if (collapsed) {
    return (
      <Box sx={{ px: 1, py: 1 }}>
        <List disablePadding>
          {items.map((item) => (
            <RailRow key={item.label} item={item} />
          ))}
        </List>
      </Box>
    )
  }

  return (
    <Box sx={{ px: 1.5, py: 1 }}>
      <List disablePadding>
        {items.map((item) => (
          <NavRow key={item.label} item={item} depth={0} />
        ))}
      </List>
    </Box>
  )
}
