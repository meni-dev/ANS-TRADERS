import { CssBaseline, ThemeProvider } from '@mui/material'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { NotificationProvider } from '@/components/feedback/NotificationProvider'
import { AppLayout } from '@/components/layout/AppLayout'
import { AuthProvider } from '@/features/auth/AuthProvider'
import { LoginPage } from '@/features/auth/LoginPage'
import { AuditPage } from '@/features/auth/components/AuditPage'
import { RequireAuth } from '@/features/auth/components/RequireAuth'
import { RequirePermission } from '@/features/auth/components/RequirePermission'
import { RolesPage } from '@/features/auth/components/RolesPage'
import { UsersPage } from '@/features/auth/components/UsersPage'
import { InvoiceDetailPage } from '@/features/billing/components/InvoiceDetailPage'
import { InvoiceFormPage } from '@/features/billing/components/InvoiceFormPage'
import { InvoiceListPage } from '@/features/billing/components/InvoiceListPage'
import { CustomerListPage } from '@/features/customers/components/CustomerListPage'
import { DashboardPage } from '@/features/dashboard/DashboardPage'
import { ProductListPage } from '@/features/products/components/ProductListPage'
import { PurchaseDetailPage } from '@/features/purchase/components/PurchaseDetailPage'
import { PurchaseFormPage } from '@/features/purchase/components/PurchaseFormPage'
import { PurchaseListPage } from '@/features/purchase/components/PurchaseListPage'
import { SettingsPage } from '@/features/settings/components/SettingsPage'
import { CashPage } from '@/features/cash/components/CashPage'
import { RegistersPage } from '@/features/reports/components/RegistersPage'
import { ProfitAndLossPage } from '@/features/expenses/components/ProfitAndLossPage'
import { ProductImportPage } from '@/features/products/components/ProductImportPage'
import { ReturnFormPage } from '@/features/returns/components/ReturnFormPage'
import { ReturnListPage } from '@/features/returns/components/ReturnListPage'
import { ReturnNoteDetailPage } from '@/features/returns/components/ReturnNoteDetailPage'
import { ChequeRegisterPage } from '@/features/payments/components/ChequeRegisterPage'
import { PartyStatementPage } from '@/features/payments/components/PartyStatementPage'
import { PaymentListPage } from '@/features/payments/components/PaymentListPage'
import { RecordPaymentPage } from '@/features/payments/components/RecordPaymentPage'
import { StockLedgerPage } from '@/features/stock/components/StockLedgerPage'
import { ShelfInsightsPage } from '@/features/stock/components/ShelfInsightsPage'
import { StockListPage } from '@/features/stock/components/StockListPage'
import { SupplierListPage } from '@/features/suppliers/components/SupplierListPage'
import { theme } from '@/theme/theme'

const queryClient = new QueryClient()

function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <QueryClientProvider client={queryClient}>
        <NotificationProvider>
          <BrowserRouter>
            <AuthProvider>
              <Routes>
                <Route path="/login" element={<LoginPage />} />

                {/* Everything else sits behind the guard. The server refuses unsigned calls on its
                    own, so this is about showing a login form instead of a screen full of
                    failures. */}
                <Route element={<RequireAuth />}>
                  <Route element={<AppLayout />}>
                    <Route path="/" element={<DashboardPage />} />
                    <Route path="/products" element={<ProductListPage />} />
                    <Route path="/customers" element={<CustomerListPage />} />
                    <Route path="/suppliers" element={<SupplierListPage />} />

                    {/* "new" is declared before ":id" so it is never swallowed by the detail route. */}
                    <Route path="/billing" element={<InvoiceListPage />} />
                    <Route path="/billing/new" element={<InvoiceFormPage />} />
                    <Route path="/billing/returns" element={<ReturnListPage side="sales" />} />
                    <Route path="/billing/returns/:id" element={<ReturnNoteDetailPage side="sales" />} />
                    <Route path="/billing/:id/return" element={<ReturnFormPage side="sales" />} />
                    <Route path="/billing/:id" element={<InvoiceDetailPage />} />

                    {/* Reading a purchase bill is reading what the shop pays, so the whole
                        section sits behind one permission rather than each screen guessing. */}
                    <Route element={<RequirePermission permission="PurchaseView" />}>
                      <Route path="/purchases" element={<PurchaseListPage />} />
                      <Route path="/purchases/new" element={<PurchaseFormPage />} />
                      <Route path="/purchases/returns" element={<ReturnListPage side="purchase" />} />
                      <Route path="/purchases/returns/:id" element={<ReturnNoteDetailPage side="purchase" />} />
                      <Route path="/purchases/:id/return" element={<ReturnFormPage side="purchase" />} />
                      <Route path="/purchases/:id" element={<PurchaseDetailPage />} />
                    </Route>

                    {/* Low Stock is the same screen with its filter pre-set, so the sidebar can offer
                        the shortcut without a second implementation to keep in step. */}
                    <Route element={<RequirePermission permission="StockView" />}>
                      <Route path="/inventory/stock" element={<StockListPage />} />
                      <Route path="/inventory/low-stock" element={<StockListPage lowOnlyByDefault />} />
                      <Route path="/inventory/stock-ledger" element={<StockLedgerPage />} />
                    </Route>

                    <Route element={<RequirePermission permission="CostView" />}>
                      <Route path="/inventory/insights" element={<ShelfInsightsPage />} />
                      <Route path="/accounts/profit" element={<ProfitAndLossPage />} />
                    </Route>

                    <Route element={<RequirePermission permission="ProductManage" />}>
                      <Route path="/products/import" element={<ProductImportPage />} />
                    </Route>

                    <Route path="/accounts/payments" element={<PaymentListPage />} />
                    <Route path="/accounts/payments/new" element={<RecordPaymentPage />} />
                    <Route path="/accounts/cheques" element={<ChequeRegisterPage />} />
                    <Route path="/accounts/cash" element={<CashPage />} />
                    <Route path="/accounts/statements/:partyId" element={<PartyStatementPage />} />

                    <Route element={<RequirePermission permission="ReportView" />}>
                      <Route path="/reports" element={<RegistersPage />} />
                    </Route>

                    <Route element={<RequirePermission permission="SettingsEdit" />}>
                      <Route path="/settings" element={<SettingsPage />} />
                    </Route>

                    <Route element={<RequirePermission permission="UserManage" />}>
                      <Route path="/settings/users" element={<UsersPage />} />
                      <Route path="/settings/roles" element={<RolesPage />} />
                    </Route>

                    <Route element={<RequirePermission permission="AuditView" />}>
                      <Route path="/settings/audit" element={<AuditPage />} />
                    </Route>
                  </Route>
                </Route>

                <Route path="*" element={<Navigate to="/" replace />} />
              </Routes>
            </AuthProvider>
          </BrowserRouter>
        </NotificationProvider>
      </QueryClientProvider>
    </ThemeProvider>
  )
}

export default App
