import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import {
  Box,
  Collapse,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Tooltip,
} from '@mui/material'
import { useEffect, useState } from 'react'
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
      sx={{
        position: 'relative',
        pl: depth === 0 ? 1.25 : 2.5,
        pr: 1.25,
        py: 0.75,
        mb: 0.25,
        color: isActive ? 'primary.dark' : 'text.secondary',
        '&:hover': { bgcolor: isActive ? 'primary.light' : 'grey.100', color: isActive ? 'primary.dark' : 'text.primary' },
        '&.Mui-disabled': { opacity: 1, color: 'text.disabled' },
        // Left accent bar on the active row. Reads faster than a colour change alone when the
        // user is scanning the rail from the far side of the screen.
        ...(isActive && {
          '&::before': {
            content: '""',
            position: 'absolute',
            left: -8,
            top: 6,
            bottom: 6,
            width: 3,
            borderRadius: '2px',
            bgcolor: 'primary.main',
          },
        }),
      }}
    >
      <ListItemIcon
        sx={{
          minWidth: 32,
          color: 'inherit',
          '& svg': { fontSize: depth === 0 ? 20 : 18 },
        }}
      >
        {item.icon}
      </ListItemIcon>
      <ListItemText
        primary={item.label}
        slotProps={{
          primary: {
            sx: {
              fontSize: depth === 0 ? 13.5 : 13,
              fontWeight: isActive ? 600 : 500,
              color: 'inherit',
            },
          },
        }}
      />
      {item.comingSoon && !hasChildren && <SoonBadge />}
      {hasChildren && (
        <ExpandMoreIcon
          sx={{
            fontSize: 18,
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
                <NavRow
                  key={child.label}
                  item={child}
                  depth={depth + 1}
                  siblings={item.children}
                />
              ))}
            </List>
          </Box>
        </Collapse>
      )}
    </>
  )
}

export function NavList({ items }: { items: NavItem[] }) {
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
