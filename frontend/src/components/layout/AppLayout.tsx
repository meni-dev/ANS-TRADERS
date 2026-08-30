import { layout } from '@/theme/theme'
import AddIcon from '@mui/icons-material/Add'
import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import KeyboardArrowRightIcon from '@mui/icons-material/KeyboardArrowRight'
import NotificationsNoneOutlinedIcon from '@mui/icons-material/NotificationsNoneOutlined'
import SearchIcon from '@mui/icons-material/Search'
import KeyboardDoubleArrowLeftIcon from '@mui/icons-material/KeyboardDoubleArrowLeft'
import KeyboardDoubleArrowRightIcon from '@mui/icons-material/KeyboardDoubleArrowRight'
import { ShopLogo } from '@/components/brand/ShopLogo'
import {
  AppBar,
  Avatar,
  Box,
  Button,
  ButtonGroup,
  Divider,
  Drawer,
  IconButton,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Stack,
  Toolbar,
  Tooltip,
  Typography,
} from '@mui/material'
import { useCallback, useEffect, useState } from 'react'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '@/features/auth/AuthProvider'
import { CommandPalette } from './CommandPalette'
import { NavList } from './NavList'
import { findNavTrail, navItems, visibleNavItems } from './navConfig'
import { visibleQuickActions } from './quickActions'

const { drawerWidth, railWidth, appBarHeight, sidebarTransition } = layout

const SIDEBAR_KEY = 'ans.sidebar'

const isMac = typeof navigator !== 'undefined' && /Mac|iPhone|iPad/.test(navigator.userAgent)
const MOD = isMac ? '⌘' : 'Ctrl'

/** The two-letter key cap used in the app bar and the tooltips. */
function KeyCap({ children }: { children: React.ReactNode }) {
  return (
    <Box
      component="span"
      sx={{
        fontSize: 10.5,
        fontWeight: 600,
        lineHeight: 1.6,
        px: 0.625,
        borderRadius: '4px',
        border: '1px solid',
        borderColor: 'divider',
        bgcolor: 'grey.50',
        color: 'text.disabled',
        whiteSpace: 'nowrap',
      }}
    >
      {children}
    </Box>
  )
}

function SidebarBrand({ collapsed }: { collapsed: boolean }) {
  return (
    <Stack
      direction="row"
      spacing={1.25}
      sx={{
        alignItems: 'center',
        justifyContent: collapsed ? 'center' : 'flex-start',
        px: collapsed ? 0 : 2,
        height: appBarHeight,
        flexShrink: 0,
        overflow: 'hidden',
      }}
    >
      <ShopLogo height={30} />
      {!collapsed && (
        <Box sx={{ minWidth: 0 }}>
          <Typography
            sx={{ fontSize: 14, fontWeight: 700, letterSpacing: '-0.01em', lineHeight: 1.2 }}
          >
            ANS Traders
          </Typography>
          <Typography sx={{ fontSize: 10.5, color: 'text.disabled', lineHeight: 1.3 }}>
            Spare Parts ERP
          </Typography>
        </Box>
      )}
    </Stack>
  )
}

/** Two letters from the signed-in name, so the avatar identifies the person rather than the shop. */
function initials(name: string) {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return '?'
  return (parts.length === 1 ? parts[0].slice(0, 2) : parts[0][0] + parts[1][0]).toUpperCase()
}

function AccountMenu() {
  const { user, signOut } = useAuth()
  const navigate = useNavigate()
  const [anchor, setAnchor] = useState<HTMLElement | null>(null)

  if (!user) return null

  return (
    <>
      <Tooltip title={`${user.name} · ${user.roleName}`}>
        <IconButton size="small" onClick={(event) => setAnchor(event.currentTarget)}>
          <Avatar
            sx={{
              width: 28,
              height: 28,
              bgcolor: 'grey.200',
              color: 'grey.700',
              fontSize: 12,
              fontWeight: 700,
            }}
          >
            {initials(user.name)}
          </Avatar>
        </IconButton>
      </Tooltip>
      <Menu anchorEl={anchor} open={!!anchor} onClose={() => setAnchor(null)}>
        <Box sx={{ px: 2, py: 1 }}>
          <Typography sx={{ fontSize: 13, fontWeight: 700 }}>{user.name}</Typography>
          <Typography sx={{ fontSize: 11.5, color: 'text.disabled' }}>
            {user.username} · {user.roleName}
          </Typography>
        </Box>
        <Divider />
        <MenuItem
          onClick={() => {
            setAnchor(null)
            navigate('/settings/users')
          }}
        >
          <ListItemText primary="People" />
        </MenuItem>
        <MenuItem
          onClick={() => {
            setAnchor(null)
            navigate('/settings/audit')
          }}
        >
          <ListItemText primary="Audit trail" />
        </MenuItem>
        <Divider />
        <MenuItem
          onClick={() => {
            setAnchor(null)
            void signOut()
          }}
        >
          <ListItemText primary="Sign out" />
        </MenuItem>
        <Divider />
        {/* Where a person actually looks for it, and legible rather than a grey whisper in a
            corner. It is the first thing anybody is asked for when something goes wrong. */}
        <Box sx={{ px: 2, pt: 1, pb: 0.5 }}>
          <Typography sx={{ fontSize: 12, fontWeight: 600, color: 'text.secondary' }}>
            ANS Traders
          </Typography>
          <Typography sx={{ fontSize: 11.5, color: 'text.disabled' }}>
            Version {__APP_VERSION__} · MVP
          </Typography>
        </Box>
      </Menu>
    </>
  )
}

/**
 * The first quick action this person may use, as a button, with the rest on a caret.
 * <p>
 * Which action leads depends on the role: a biller opens on New Invoice, someone who only handles
 * purchases opens on Record Purchase. Hard-coding the invoice would leave half the shop with a
 * primary button they are not allowed to press.
 * </p>
 */
function NewButton() {
  const { can } = useAuth()
  const navigate = useNavigate()
  const [anchor, setAnchor] = useState<HTMLElement | null>(null)

  const actions = visibleQuickActions(can)
  if (actions.length === 0) return null

  const [primary, ...rest] = actions

  return (
    <>
      <ButtonGroup
        variant="contained"
        size="small"
        sx={{
          display: { xs: 'none', sm: 'inline-flex' },
          // MUI's own divider is primary.light and all but vanishes on a filled button.
          '& .MuiButtonGroup-firstButton': { borderRightColor: 'rgba(255,255,255,0.32)' },
        }}
      >
        <Button startIcon={<AddIcon sx={{ fontSize: 17 }} />} onClick={() => navigate(primary.path)}>
          {primary.label.replace(/^New /, '')}
        </Button>
        {rest.length > 0 && (
          <Button
            onClick={(event) => setAnchor(event.currentTarget)}
            aria-label="More actions"
            sx={{ px: 0.5, minWidth: 0 }}
          >
            <ExpandMoreIcon sx={{ fontSize: 18 }} />
          </Button>
        )}
      </ButtonGroup>

      <Menu anchorEl={anchor} open={!!anchor} onClose={() => setAnchor(null)}>
        {rest.map((action) => (
          <MenuItem
            key={action.path}
            onClick={() => {
              setAnchor(null)
              navigate(action.path)
            }}
          >
            <ListItemIcon sx={{ '& svg': { fontSize: 18 } }}>{action.icon}</ListItemIcon>
            <ListItemText primary={action.label} />
          </MenuItem>
        ))}
      </Menu>
    </>
  )
}

export function AppLayout() {
  const { can } = useAuth()
  const location = useLocation()
  const visible = visibleNavItems(navItems, can)
  const trail = findNavTrail(location.pathname)
  // The app bar previously repeated the product name on every screen. Showing where the user
  // actually is costs the same space and is the thing they need when navigating.
  const heading = trail.at(-1)?.label ?? 'Dashboard'
  const parent = trail.length > 1 ? trail[0].label : null

  const [collapsed, setCollapsed] = useState(
    () => localStorage.getItem(SIDEBAR_KEY) === 'collapsed',
  )
  const [paletteOpen, setPaletteOpen] = useState(false)

  const toggleSidebar = useCallback(() => {
    setCollapsed((prev) => {
      localStorage.setItem(SIDEBAR_KEY, prev ? 'expanded' : 'collapsed')
      return !prev
    })
  }, [])

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (!(event.metaKey || event.ctrlKey)) return
      if (event.key.toLowerCase() === 'k') {
        event.preventDefault()
        setPaletteOpen((prev) => !prev)
      } else if (event.key === '\\') {
        event.preventDefault()
        toggleSidebar()
      }
    }

    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [toggleSidebar])

  const sidebarWidth = collapsed ? railWidth : drawerWidth

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <AppBar
        position="fixed"
        color="inherit"
        sx={{
          width: `calc(100% - ${sidebarWidth}px)`,
          ml: `${sidebarWidth}px`,
          transition: sidebarTransition + ', margin-left 180ms cubic-bezier(0.4, 0, 0.2, 1)',
        }}
      >
        <Toolbar sx={{ minHeight: `${appBarHeight}px !important`, px: { xs: 2, md: 2.5 }, gap: 1 }}>
          <Stack
            direction="row"
            spacing={0.25}
            sx={{ alignItems: 'center', flexGrow: 1, minWidth: 0 }}
          >
            {parent && (
              <>
                <Typography
                  sx={{ fontSize: 13, color: 'text.disabled', fontWeight: 500, flexShrink: 0 }}
                >
                  {parent}
                </Typography>
                <KeyboardArrowRightIcon sx={{ fontSize: 15, color: 'text.disabled' }} />
              </>
            )}
            <Typography
              component="h1"
              sx={{
                fontSize: 13.5,
                fontWeight: 600,
                lineHeight: 1.3,
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
            >
              {heading}
            </Typography>
          </Stack>

          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <Box
              component="button"
              type="button"
              onClick={() => setPaletteOpen(true)}
              sx={{
                display: { xs: 'none', md: 'flex' },
                alignItems: 'center',
                gap: 1,
                height: 30,
                pl: 1,
                pr: 0.75,
                minWidth: 190,
                font: 'inherit',
                cursor: 'pointer',
                borderRadius: '6px',
                border: '1px solid',
                borderColor: 'divider',
                bgcolor: 'grey.50',
                color: 'text.disabled',
                '&:hover': { borderColor: 'grey.400', bgcolor: '#fff' },
              }}
            >
              <SearchIcon sx={{ fontSize: 17 }} />
              <Box component="span" sx={{ fontSize: 12.5, flexGrow: 1, textAlign: 'left' }}>
                Search or jump to…
              </Box>
              <KeyCap>{MOD} K</KeyCap>
            </Box>

            <Tooltip title={`Search or jump to  ${MOD} K`}>
              <IconButton
                size="small"
                onClick={() => setPaletteOpen(true)}
                sx={{ display: { md: 'none' } }}
                aria-label="Search"
              >
                <SearchIcon sx={{ fontSize: 19 }} />
              </IconButton>
            </Tooltip>

            <NewButton />

            <Tooltip title="Notifications">
              <IconButton size="small">
                <NotificationsNoneOutlinedIcon sx={{ fontSize: 19 }} />
              </IconButton>
            </Tooltip>
            <AccountMenu />
          </Stack>
        </Toolbar>
      </AppBar>

      <Drawer
        variant="permanent"
        sx={{
          width: sidebarWidth,
          flexShrink: 0,
          transition: sidebarTransition,
          [`& .MuiDrawer-paper`]: {
            width: sidebarWidth,
            boxSizing: 'border-box',
            display: 'flex',
            flexDirection: 'column',
            overflowX: 'hidden',
            transition: sidebarTransition,
          },
        }}
      >
        <SidebarBrand collapsed={collapsed} />
        <Box sx={{ borderTop: '1px solid', borderColor: 'divider' }} />
        <Box sx={{ flexGrow: 1, overflowY: 'auto', overflowX: 'hidden' }}>
          <NavList items={visible} collapsed={collapsed} />
        </Box>
        <Stack
          direction="row"
          sx={{
            alignItems: 'center',
            justifyContent: 'center',
            py: 0.75,
            borderTop: '1px solid',
            borderColor: 'divider',
            flexShrink: 0,
          }}
        >
          <Tooltip title={`${collapsed ? 'Expand' : 'Collapse'} sidebar  ${MOD} \\`} placement="right">
            <IconButton size="small" onClick={toggleSidebar} aria-label="Toggle sidebar">
              {collapsed ? (
                <KeyboardDoubleArrowRightIcon sx={{ fontSize: 19 }} />
              ) : (
                <KeyboardDoubleArrowLeftIcon sx={{ fontSize: 19 }} />
              )}
            </IconButton>
          </Tooltip>
        </Stack>
      </Drawer>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          minWidth: 0,
          bgcolor: 'background.default',
          minHeight: '100vh',
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        <Box sx={{ height: `${appBarHeight}px`, flexShrink: 0 }} />
        <Box sx={{ p: { xs: 2, md: 2.5 }, flexGrow: 1 }}>
          <Outlet />
        </Box>
      </Box>

      <CommandPalette open={paletteOpen} onClose={() => setPaletteOpen(false)} />
    </Box>
  )
}
