import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined'
import PaymentsOutlinedIcon from '@mui/icons-material/PaymentsOutlined'
import PointOfSaleOutlinedIcon from '@mui/icons-material/PointOfSaleOutlined'
import ShoppingCartOutlinedIcon from '@mui/icons-material/ShoppingCartOutlined'
import SavingsOutlinedIcon from '@mui/icons-material/SavingsOutlined'
import type { ReactNode } from 'react'
import type { Permission } from '@/features/auth/types'
import type { AccentTone } from '@/theme/theme'

export type QuickAction = {
  label: string
  /** What it is for, one line. Shown in the command palette, not on the dashboard buttons. */
  hint: string
  icon: ReactNode
  path: string
  permission?: Permission
  /**
   * Colours the icon on the dashboard strip so the five are told apart at a glance rather than
   * read one by one. The app bar menu and the palette ignore it — a menu is already a list.
   */
  tone: AccentTone
}

/**
 * The five things a counter does every day, in the order a day runs: bill, buy, collect, close,
 * hand over. One list feeds the dashboard row, the command palette and the app bar's New button —
 * three places that would otherwise drift apart on which actions exist and who may see them.
 *
 * Routes only, deliberately. An action that has to open a dialog on some other screen would need
 * that screen to grow a query parameter for it, and a quick action that lands you somewhere and
 * leaves you to find the button is worse than no quick action.
 */
export const quickActions: QuickAction[] = [
  {
    label: 'New Invoice',
    hint: 'Bill a customer at the counter',
    icon: <PointOfSaleOutlinedIcon />,
    path: '/billing/new',
    tone: 'blue',
    permission: 'BillCreate',
  },
  {
    label: 'Record Purchase',
    hint: "Enter a supplier's bill and put stock on the shelf",
    icon: <ShoppingCartOutlinedIcon />,
    path: '/purchases/new',
    tone: 'violet',
    permission: 'PurchaseCreate',
  },
  {
    label: 'Receive Payment',
    hint: 'Take money against an outstanding bill',
    icon: <PaymentsOutlinedIcon />,
    path: '/accounts/payments/new',
    tone: 'teal',
    permission: 'PaymentRecord',
  },
  {
    label: 'Day Close',
    hint: 'Count the drawer and close the day',
    icon: <SavingsOutlinedIcon />,
    path: '/accounts/cash',
    tone: 'amber',
    permission: 'CashDayClose',
  },
  {
    label: 'Registers',
    hint: 'The seventeen registers, ready for the accountant',
    icon: <AssessmentOutlinedIcon />,
    path: '/reports',
    tone: 'rose',
    permission: 'ReportView',
  },
]

export function visibleQuickActions(can: (permission: Permission) => boolean): QuickAction[] {
  return quickActions.filter((action) => !action.permission || can(action.permission))
}
