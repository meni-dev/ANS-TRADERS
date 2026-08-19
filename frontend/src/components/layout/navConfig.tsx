import TrendingUpOutlinedIcon from '@mui/icons-material/TrendingUpOutlined'
import AssignmentReturnOutlinedIcon from '@mui/icons-material/AssignmentReturnOutlined'
import AccountBalanceWalletOutlinedIcon from '@mui/icons-material/AccountBalanceWalletOutlined'
import PaymentsOutlinedIcon from '@mui/icons-material/PaymentsOutlined'
import AccountBalanceOutlinedIcon from '@mui/icons-material/AccountBalanceOutlined'
import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined'
import CategoryOutlinedIcon from '@mui/icons-material/CategoryOutlined'
import DashboardOutlinedIcon from '@mui/icons-material/DashboardOutlined'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import HistoryOutlinedIcon from '@mui/icons-material/HistoryOutlined'
import LocalShippingOutlinedIcon from '@mui/icons-material/LocalShippingOutlined'
import PersonOutlineOutlinedIcon from '@mui/icons-material/PersonOutlineOutlined'
import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined'
import PointOfSaleOutlinedIcon from '@mui/icons-material/PointOfSaleOutlined'
import ReceiptLongOutlinedIcon from '@mui/icons-material/ReceiptLongOutlined'
import SettingsOutlinedIcon from '@mui/icons-material/SettingsOutlined'
import ShoppingCartOutlinedIcon from '@mui/icons-material/ShoppingCartOutlined'
import SwapVertOutlinedIcon from '@mui/icons-material/SwapVertOutlined'
import TrendingDownOutlinedIcon from '@mui/icons-material/TrendingDownOutlined'
import WarehouseOutlinedIcon from '@mui/icons-material/WarehouseOutlined'
import QueryStatsOutlinedIcon from '@mui/icons-material/QueryStatsOutlined'
import BadgeOutlinedIcon from '@mui/icons-material/BadgeOutlined'
import type { ReactNode } from 'react'
import type { Permission } from '@/features/auth/types'

export type NavItem = {
  label: string
  icon: ReactNode
  path?: string
  children?: NavItem[]
  comingSoon?: boolean
  /**
   * Kept out of the drawer but still matched by {@link findNavTrail}, so a page reached from
   * elsewhere in the app is titled properly instead of falling back to whatever matches first.
   */
  hidden?: boolean
  /**
   * Hides the row from anyone without this permission.
   * <p>
   * Only about what is worth offering — the server refuses the request either way. A row left
   * visible is not a hole; a row hidden is not a lock.
   * </p>
   */
  permission?: Permission
}

/**
 * The drawer as this person sees it. A section whose children are all hidden goes with them, so
 * nobody is left with an empty group that expands onto nothing.
 */
export function visibleNavItems(
  items: NavItem[],
  can: (permission: Permission) => boolean,
): NavItem[] {
  return items
    .filter((item) => !item.permission || can(item.permission))
    .map((item) => (item.children ? { ...item, children: visibleNavItems(item.children, can) } : item))
    .filter((item) => !item.children || item.children.length > 0)
}

export const navItems: NavItem[] = [
  { label: 'Dashboard', icon: <DashboardOutlinedIcon />, path: '/' },
  {
    label: 'Billing',
    icon: <PointOfSaleOutlinedIcon />,
    children: [
      { label: 'Invoices', icon: <PointOfSaleOutlinedIcon />, path: '/billing' },
      { label: 'Sales Returns', icon: <AssignmentReturnOutlinedIcon />, path: '/billing/returns' },
    ],
  },
  {
    label: 'Purchase',
    icon: <ShoppingCartOutlinedIcon />,
    children: [
      { label: 'Purchase Bills', icon: <ShoppingCartOutlinedIcon />, path: '/purchases', permission: 'PurchaseView' },
      { label: 'Purchase Returns', icon: <AssignmentReturnOutlinedIcon />, path: '/purchases/returns', permission: 'PurchaseView' },
    ],
  },
  {
    label: 'Inventory',
    icon: <Inventory2OutlinedIcon />,
    children: [
      { label: 'Products', icon: <CategoryOutlinedIcon />, path: '/products' },
      { label: 'Stock', icon: <WarehouseOutlinedIcon />, path: '/inventory/stock', permission: 'StockView' },
      { label: 'Stock Ledger', icon: <SwapVertOutlinedIcon />, path: '/inventory/stock-ledger', permission: 'StockView' },
      { label: 'Low Stock', icon: <TrendingDownOutlinedIcon />, path: '/inventory/low-stock', permission: 'StockView' },
      { label: 'Shelf Insights', icon: <QueryStatsOutlinedIcon />, path: '/inventory/insights', permission: 'CostView' },
    ],
  },
  {
    label: 'Parties',
    icon: <GroupsOutlinedIcon />,
    children: [
      { label: 'Customers', icon: <PersonOutlineOutlinedIcon />, path: '/customers' },
      { label: 'Suppliers', icon: <LocalShippingOutlinedIcon />, path: '/suppliers' },
    ],
  },
  {
    label: 'Accounts',
    icon: <AccountBalanceOutlinedIcon />,
    children: [
      { label: 'Receipts & Payments', icon: <PaymentsOutlinedIcon />, path: '/accounts/payments' },
      { label: 'Cheque Register', icon: <AccountBalanceWalletOutlinedIcon />, path: '/accounts/cheques' },
      { label: 'Cash & Day Close', icon: <PointOfSaleOutlinedIcon />, path: '/accounts/cash' },
      { label: 'Profit & Loss', icon: <TrendingUpOutlinedIcon />, path: '/accounts/profit', permission: 'CostView' },
      {
        label: 'Statement',
        icon: <ReceiptLongOutlinedIcon />,
        path: '/accounts/statements',
        // Reached from a party row, never from the drawer — there is no statement without a party.
        hidden: true,
      },
    ],
  },
  { label: 'Reports & GST', icon: <AssessmentOutlinedIcon />, path: '/reports', permission: 'ReportView' },
  {
    label: 'Settings',
    icon: <SettingsOutlinedIcon />,
    children: [
      { label: 'Shop', icon: <SettingsOutlinedIcon />, path: '/settings', permission: 'SettingsEdit' },
      { label: 'People', icon: <GroupsOutlinedIcon />, path: '/settings/users', permission: 'UserManage' },
      { label: 'Roles', icon: <BadgeOutlinedIcon />, path: '/settings/roles', permission: 'UserManage' },
      { label: 'Audit Trail', icon: <HistoryOutlinedIcon />, path: '/settings/audit', permission: 'AuditView' },
    ],
  },
]

/**
 * True when a nav entry owns the current route. Sections like Billing have nested routes
 * (`/billing/new`, `/billing/:id`) that must still light up their parent row, so this matches on the
 * path segment rather than the whole string — with `/` exempted, since it prefixes everything.
 */
export function isNavPathActive(itemPath: string | undefined, pathname: string): boolean {
  if (!itemPath) return false
  if (itemPath === '/') return pathname === '/'
  return pathname === itemPath || pathname.startsWith(`${itemPath}/`)
}

/** Flattened lookup used by the app bar to title the current page. */
export function findNavTrail(pathname: string): NavItem[] {
  for (const item of navItems) {
    const child = bestMatch(item.children, pathname)
    if (child) return [item, child]
    if (isNavPathActive(item.path, pathname)) return [item]
  }
  return []
}

/**
 * The longest matching path wins, not the first one listed.
 * <p>
 * Sections have siblings whose paths nest — Sales Returns lives under <code>/billing/returns</code>
 * while Invoices owns <code>/billing</code>, which prefixes it. Taking the first match would light
 * up Invoices and title the page after it on every returns screen.
 * </p>
 */
function bestMatch(children: NavItem[] | undefined, pathname: string): NavItem | undefined {
  return children
    ?.filter((child) => isNavPathActive(child.path, pathname))
    .sort((a, b) => (b.path?.length ?? 0) - (a.path?.length ?? 0))[0]
}

/** True only for the single most specific entry, so exactly one row is ever highlighted. */
export function isNavRowActive(item: NavItem, siblings: NavItem[] | undefined, pathname: string) {
  if (!isNavPathActive(item.path, pathname)) return false
  const best = bestMatch(siblings, pathname)
  return !best || best.path === item.path
}
