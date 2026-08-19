import { ApiError } from '@/lib/api/client'
import { downloadCsv, indexHeaders, parseCsv, toCsv } from '@/lib/csv'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import DownloadOutlinedIcon from '@mui/icons-material/DownloadOutlined'
import UploadFileOutlinedIcon from '@mui/icons-material/UploadFileOutlined'
import {
  Alert,
  Box,
  Button,
  Chip,
  FormControlLabel,
  IconButton,
  Paper,
  Stack,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useConfirmProductImport, usePreviewProductImport } from '../hooks'
import { IMPORT_COLUMNS, type ProductImportPreviewDto, type ProductImportRow } from '../types'

/**
 * Loads a catalogue from a spreadsheet. Upload → preview → fix → confirm, and the confirm is all or
 * nothing: a half-loaded catalogue leaves the shop unable to say which parts are on the master.
 */
export function ProductImportPage() {
  const navigate = useNavigate()
  const fileInput = useRef<HTMLInputElement>(null)

  const [fileName, setFileName] = useState<string | null>(null)
  const [rows, setRows] = useState<ProductImportRow[]>([])
  const [preview, setPreview] = useState<ProductImportPreviewDto | null>(null)
  const [updateExisting, setUpdateExisting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [done, setDone] = useState<string | null>(null)

  const previewImport = usePreviewProductImport()
  const confirmImport = useConfirmProductImport()

  function downloadTemplate() {
    const example = [
      'BP-001', 'BP001', 'Brake Pad - Front', 'Front disc pad', 'Honda', 'Activa',
      '87141090', '28', 'PCS', '220', '320', '380', '10', '3',
    ]

    downloadCsv(
      'product-import-template.csv',
      toCsv([IMPORT_COLUMNS.map((c) => c.header), example]),
    )
  }

  function downloadErrors() {
    if (!preview) return

    downloadCsv(
      'import-errors.csv',
      toCsv([
        ['Row', 'Part Number', 'Item Name', 'Problem'],
        ...preview.rows
          .filter((r) => r.errors.length > 0)
          .map((r) => [r.rowNumber, r.partNumber, r.itemName, r.errors.join('; ')]),
      ]),
    )
  }

  async function onFile(file: File) {
    setError(null)
    setDone(null)
    setPreview(null)
    setFileName(file.name)

    const sheet = parseCsv(await file.text())

    if (sheet.length < 2) {
      setError('That file has a header but no rows.')
      setRows([])
      return
    }

    const columns = indexHeaders(sheet[0])
    const missing = IMPORT_COLUMNS.filter(
      (c) => !(c.header.toLowerCase().replace(/[^a-z0-9]/g, '') in columns),
    )

    // Named up front rather than letting every row fail for the same reason.
    if (missing.length > 0) {
      setError(`The file is missing these columns: ${missing.map((c) => c.header).join(', ')}`)
      setRows([])
      return
    }

    const parsed = sheet.slice(1).map((line, index) => {
      const row: Record<string, string | number> = { rowNumber: index + 1 }

      for (const column of IMPORT_COLUMNS) {
        const at = columns[column.header.toLowerCase().replace(/[^a-z0-9]/g, '')]
        row[column.key] = (line[at] ?? '').trim()
      }

      return row as unknown as ProductImportRow
    })

    setRows(parsed)

    try {
      setPreview(await previewImport.mutateAsync({ rows: parsed, updateExisting }))
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Could not read that file')
    }
  }

  async function reprice(next: boolean) {
    setUpdateExisting(next)
    if (rows.length === 0) return

    // The whole verdict changes with this switch — a row that was "already on the master" becomes
    // an update — so it is re-checked rather than left showing the old answer.
    setPreview(await previewImport.mutateAsync({ rows, updateExisting: next }))
  }

  async function confirm() {
    setError(null)
    try {
      const result = await confirmImport.mutateAsync({ rows, updateExisting })
      setDone(`${result.created} added, ${result.updated} updated.`)
      setPreview(null)
      setRows([])
      setFileName(null)
    } catch (caught) {
      setError(
        caught instanceof ApiError
          ? (Object.values(caught.errors).flat()[0] ?? caught.message)
          : 'Could not import that file',
      )
    }
  }

  const blocked = (preview?.rejected ?? 0) > 0

  return (
    <Stack spacing={2.5}>
      <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
        <IconButton onClick={() => navigate('/products')} aria-label="Back">
          <ArrowBackIcon />
        </IconButton>
        <Box sx={{ flex: 1 }}>
          <Typography variant="h5" sx={{ fontWeight: 700 }}>
            Load the catalogue
          </Typography>
          <Typography sx={{ fontSize: 13.5, color: 'text.secondary' }}>
            A spreadsheet of parts. Nothing is saved until you confirm.
          </Typography>
        </Box>
        <Button startIcon={<DownloadOutlinedIcon />} onClick={downloadTemplate}>
          Template
        </Button>
      </Stack>

      {error ? <Alert severity="error">{error}</Alert> : null}
      {done ? <Alert severity="success">{done}</Alert> : null}

      <Paper variant="outlined" sx={{ p: 2.5 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ alignItems: 'center' }}>
          <Button
            variant="contained"
            startIcon={<UploadFileOutlinedIcon />}
            onClick={() => fileInput.current?.click()}
            disabled={previewImport.isPending}
          >
            {previewImport.isPending ? 'Reading…' : 'Choose a CSV file'}
          </Button>
          <input
            ref={fileInput}
            type="file"
            accept=".csv,text/csv"
            hidden
            onChange={(event) => {
              const file = event.target.files?.[0]
              if (file) void onFile(file)
              event.target.value = ''
            }}
          />
          {fileName ? (
            <Typography sx={{ fontSize: 13.5, color: 'text.secondary' }}>{fileName}</Typography>
          ) : null}
          <Box sx={{ flex: 1 }} />
          <FormControlLabel
            control={
              <Switch checked={updateExisting} onChange={(e) => void reprice(e.target.checked)} />
            }
            label="Update parts already on the master"
          />
        </Stack>
        <Typography sx={{ fontSize: 12.5, color: 'text.secondary', mt: 1 }}>
          {updateExisting
            ? 'A part number already on the master has its rates and details updated. Stock on the shelf is never touched by an import.'
            : 'A part number already on the master is reported as a problem. Turn this on to re-load a revised price list.'}
        </Typography>
      </Paper>

      {preview ? (
        <>
          <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
            <Chip label={`${preview.totalRows} rows`} variant="outlined" />
            <Chip label={`${preview.willCreate} to add`} color="success" variant="outlined" />
            {preview.willUpdate > 0 ? (
              <Chip label={`${preview.willUpdate} to update`} color="info" variant="outlined" />
            ) : null}
            {preview.rejected > 0 ? (
              <Chip label={`${preview.rejected} with problems`} color="error" variant="outlined" />
            ) : null}
            <Box sx={{ flex: 1 }} />
            {preview.rejected > 0 ? (
              <Button startIcon={<DownloadOutlinedIcon />} onClick={downloadErrors}>
                Download the problems
              </Button>
            ) : null}
          </Stack>

          {blocked ? (
            <Alert severity="warning">
              Nothing is imported while any row has a problem — a half-loaded catalogue is worse than
              none. Download the list, fix those rows, and upload the file again.
            </Alert>
          ) : null}

          <Paper variant="outlined">
            <Box sx={{ overflowX: 'auto', maxHeight: 480 }}>
              <Table size="small" stickyHeader sx={{ minWidth: 720 }}>
                <TableHead>
                  <TableRow>
                    <TableCell sx={{ fontWeight: 700, fontSize: 12.5, width: 70 }}>Row</TableCell>
                    <TableCell sx={{ fontWeight: 700, fontSize: 12.5 }}>Part number</TableCell>
                    <TableCell sx={{ fontWeight: 700, fontSize: 12.5 }}>Item</TableCell>
                    <TableCell sx={{ fontWeight: 700, fontSize: 12.5, width: 110 }}>Action</TableCell>
                    <TableCell sx={{ fontWeight: 700, fontSize: 12.5 }}>Problem</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {/* Problem rows first — that is the list the user is here to work through. */}
                  {[...preview.rows]
                    .sort((a, b) => b.errors.length - a.errors.length || a.rowNumber - b.rowNumber)
                    .map((row) => (
                      <TableRow key={row.rowNumber}>
                        <TableCell sx={{ fontSize: 12.5, fontFamily: 'monospace' }}>
                          {row.rowNumber}
                        </TableCell>
                        <TableCell sx={{ fontSize: 12.5, fontFamily: 'monospace' }}>
                          {row.partNumber || '—'}
                        </TableCell>
                        <TableCell sx={{ fontSize: 13 }}>{row.itemName || '—'}</TableCell>
                        <TableCell>
                          <Chip
                            size="small"
                            variant="outlined"
                            label={row.action}
                            color={
                              row.action === 'Reject'
                                ? 'error'
                                : row.action === 'Update'
                                  ? 'info'
                                  : 'success'
                            }
                          />
                        </TableCell>
                        <TableCell sx={{ fontSize: 12.5, color: 'error.main' }}>
                          {row.errors.join('; ')}
                        </TableCell>
                      </TableRow>
                    ))}
                </TableBody>
              </Table>
            </Box>
          </Paper>

          <Stack direction="row" spacing={1.5} sx={{ justifyContent: 'flex-end' }}>
            <Button onClick={() => navigate('/products')}>Cancel</Button>
            <Button
              variant="contained"
              disabled={blocked || confirmImport.isPending}
              onClick={confirm}
            >
              {confirmImport.isPending
                ? 'Importing…'
                : `Import ${preview.willCreate + preview.willUpdate} rows`}
            </Button>
          </Stack>
        </>
      ) : null}
    </Stack>
  )
}

