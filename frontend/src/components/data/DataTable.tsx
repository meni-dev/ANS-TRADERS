import { Box, Paper, Typography } from '@mui/material'
import { DataGrid, type GridColDef, type GridPaginationModel } from '@mui/x-data-grid'
import type { ReactNode } from 'react'

type DataTableProps<T extends { id: string }> = {
  rows: T[]
  columns: GridColDef<T>[]
  loading?: boolean
  rowCount: number
  paginationModel: GridPaginationModel
  onPaginationModelChange: (model: GridPaginationModel) => void
  onRowClick?: (row: T) => void
  /** Shown in place of the grid body when there are no rows and nothing is loading. */
  emptyTitle?: string
  emptyDescription?: string
  emptyAction?: ReactNode
}

function EmptyState({
  title,
  description,
  action,
}: {
  title: string
  description?: string
  action?: ReactNode
}) {
  return (
    <Box
      sx={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 1,
        px: 3,
        textAlign: 'center',
      }}
    >
      <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
        {title}
      </Typography>
      {description && (
        <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 380 }}>
          {description}
        </Typography>
      )}
      {action && <Box sx={{ mt: 1 }}>{action}</Box>}
    </Box>
  )
}

export function DataTable<T extends { id: string }>({
  rows,
  columns,
  loading,
  rowCount,
  paginationModel,
  onPaginationModelChange,
  onRowClick,
  emptyTitle = 'Nothing here yet',
  emptyDescription,
  emptyAction,
}: DataTableProps<T>) {
  return (
    <Paper variant="outlined" sx={{ width: '100%', borderRadius: '8px', overflow: 'hidden' }}>
      <DataGrid
        rows={rows}
        columns={columns}
        loading={loading}
        rowCount={rowCount}
        paginationMode="server"
        paginationModel={paginationModel}
        onPaginationModelChange={onPaginationModelChange}
        pageSizeOptions={[10, 20, 50]}
        disableRowSelectionOnClick
        disableColumnMenu
        rowHeight={52}
        columnHeaderHeight={44}
        onRowClick={onRowClick ? (params) => onRowClick(params.row) : undefined}
        slots={{
          noRowsOverlay: () => (
            <EmptyState title={emptyTitle} description={emptyDescription} action={emptyAction} />
          ),
        }}
        sx={{
          border: 'none',
          '--DataGrid-overlayHeight': '260px',
          // Header reads as a label strip rather than another data row.
          '& .MuiDataGrid-columnHeaders': {
            borderBottom: '1px solid',
            borderColor: 'divider',
          },
          '& .MuiDataGrid-columnHeader': {
            backgroundColor: 'grey.50',
          },
          '& .MuiDataGrid-columnHeaderTitle': {
            fontSize: 12,
            fontWeight: 700,
            letterSpacing: '0.02em',
            textTransform: 'uppercase',
            color: 'text.secondary',
          },
          '& .MuiDataGrid-columnSeparator': { display: 'none' },
          // Cells are centred explicitly: a custom renderCell drops the grid's own alignment, so
          // single-line and two-line cells in the same row would otherwise sit at different heights.
          '& .MuiDataGrid-cell': {
            borderColor: 'grey.100',
            fontSize: 13.5,
            display: 'flex',
            alignItems: 'center',
            '&:focus, &:focus-within': { outline: 'none' },
          },
          '& .MuiDataGrid-cell--textRight': { justifyContent: 'flex-end' },
          '& .MuiDataGrid-cell--textCenter': { justifyContent: 'center' },
          '& .MuiDataGrid-columnHeader:focus, & .MuiDataGrid-columnHeader:focus-within': {
            outline: 'none',
          },
          '& .MuiDataGrid-row': {
            ...(onRowClick ? { cursor: 'pointer' } : {}),
            '&:hover': { backgroundColor: 'grey.50' },
            '&:last-of-type .MuiDataGrid-cell': { borderBottom: 'none' },
          },
          '& .MuiDataGrid-footerContainer': {
            borderTop: '1px solid',
            borderColor: 'divider',
            minHeight: 48,
          },
          '& .MuiTablePagination-root': { fontSize: 13 },
        }}
      />
    </Paper>
  )
}
