import { layout } from '@/theme/theme'
import NotificationsNoneOutlinedIcon from '@mui/icons-material/NotificationsNoneOutlined'
import { ShopLogo } from '@/components/brand/ShopLogo'
import {
  AppBar,
  Avatar,
  Box,
  Divider,
  Drawer,
  IconButton,
  ListItemText,
  Menu,
  MenuItem,
  Stack,
  Toolbar,
  Tooltip,
  Typography,
} from '@mui/material'
import { useState } from 'react'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '@/features/auth/AuthProvider'
import { NavList } from './NavList'
import { findNavTrail, navItems, visibleNavItems } from './navConfig'

const { drawerWidth, appBarHeight } = layout

function BrandMark() {
  return (
    <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center', px: 2, height: appBarHeight, flexShrink: 0 }}>
      <ShopLogo height={30} />
      <Box sx={{ minWidth: 0 }}>
        <Typography sx={{ fontSize: 15, fontWeight: 700, letterSpacing: '-0.01em', lineHeight: 1.2 }}>
          ANS Traders
        </Typography>
        <Typography sx={{ fontSize: 11, color: 'text.disabled', lineHeight: 1.3 }}>
          Spare Parts ERP
        </Typography>
      </Box>
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
          <Avatar sx={{ width: 32, height: 32, bgcolor: 'grey.200', color: 'grey.700', fontSize: 13, fontWeight: 700 }}>
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

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <AppBar
        position="fixed"
        color="inherit"
        sx={{ width: `calc(100% - ${drawerWidth}px)`, ml: `${drawerWidth}px` }}
      >
        <Toolbar sx={{ minHeight: `${appBarHeight}px !important`, px: { xs: 2, md: 3 } }}>
          <Box sx={{ flexGrow: 1, minWidth: 0 }}>
            {parent && (
              <Typography sx={{ fontSize: 11.5, color: 'text.disabled', fontWeight: 600, lineHeight: 1.3 }}>
                {parent}
              </Typography>
            )}
            <Typography component="h1" sx={{ fontSize: 15, fontWeight: 600, lineHeight: 1.3 }}>
              {heading}
            </Typography>
          </Box>

          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <Tooltip title="Notifications">
              <IconButton size="small">
                <NotificationsNoneOutlinedIcon sx={{ fontSize: 20 }} />
              </IconButton>
            </Tooltip>
            <AccountMenu />
          </Stack>
        </Toolbar>
      </AppBar>

      <Drawer
        variant="permanent"
        sx={{
          width: drawerWidth,
          flexShrink: 0,
          [`& .MuiDrawer-paper`]: {
            width: drawerWidth,
            boxSizing: 'border-box',
            display: 'flex',
            flexDirection: 'column',
          },
        }}
      >
        <BrandMark />
        <Box sx={{ borderTop: '1px solid', borderColor: 'divider' }} />
        <Box sx={{ flexGrow: 1, overflowY: 'auto' }}>
          <NavList items={visible} />
        </Box>
        <Box sx={{ px: 2.5, py: 1.5, borderTop: '1px solid', borderColor: 'divider' }}>
          <Typography sx={{ fontSize: 11, color: 'text.disabled' }}>Version 0.1.0 · MVP</Typography>
        </Box>
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
        <Box sx={{ p: { xs: 2, md: 3 }, flexGrow: 1 }}>
          <Outlet />
        </Box>
      </Box>
    </Box>
  )
}
