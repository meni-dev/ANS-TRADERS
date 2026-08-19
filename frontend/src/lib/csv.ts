/**
 * A small CSV reader, written rather than installed.
 *
 * The format is well understood and the whole job is one state machine over quotes and separators;
 * a dependency for it would be more code to audit than the code it replaces. What it does handle,
 * because real spreadsheet exports contain all of it: quoted fields, commas and newlines inside
 * quotes, doubled quotes as an escape, CRLF endings, and a trailing newline.
 */
export function parseCsv(text: string): string[][] {
  const rows: string[][] = []
  let row: string[] = []
  let field = ''
  let quoted = false
  let i = 0

  // Excel writes a byte-order mark on UTF-8 CSVs, which would otherwise become part of the first
  // header name and make that column unmatchable.
  if (text.charCodeAt(0) === 0xfeff) i = 1

  const endField = () => {
    row.push(field)
    field = ''
  }

  const endRow = () => {
    endField()
    // A file ending in a newline would otherwise contribute a final row of one empty field.
    if (row.length > 1 || row[0] !== '') rows.push(row)
    row = []
  }

  while (i < text.length) {
    const char = text[i]

    if (quoted) {
      if (char === '"') {
        if (text[i + 1] === '"') {
          field += '"'
          i += 2
          continue
        }
        quoted = false
        i += 1
        continue
      }
      field += char
      i += 1
      continue
    }

    if (char === '"' && field === '') {
      quoted = true
      i += 1
      continue
    }

    if (char === ',') {
      endField()
      i += 1
      continue
    }

    if (char === '\r') {
      i += 1
      continue
    }

    if (char === '\n') {
      endRow()
      i += 1
      continue
    }

    field += char
    i += 1
  }

  if (field !== '' || row.length > 0) endRow()

  return rows
}

/**
 * Maps a parsed sheet onto named columns, matching headers case- and space-insensitively so
 * "Part Number", "part_number" and "PARTNUMBER" all find the same column. A shop's file will not
 * have been written to our spelling.
 */
export function indexHeaders(header: string[]): Record<string, number> {
  const index: Record<string, number> = {}

  header.forEach((name, position) => {
    const key = name.toLowerCase().replace(/[^a-z0-9]/g, '')
    if (key && !(key in index)) index[key] = position
  })

  return index
}

/** Quotes a value for output only when it needs it, so a plain file stays readable. */
export function toCsv(rows: (string | number)[][]): string {
  return rows
    .map((row) =>
      row
        .map((cell) => {
          const text = String(cell ?? '')
          return /[",\n\r]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text
        })
        .join(','),
    )
    .join('\n')
}

/**
 * Hands the browser a file it built itself — no round trip for something already in memory.
 * <p>
 * The leading BOM is what makes Excel read the file as UTF-8. Without it a customer named
 * "Muthu Kumar & Co." opens as mojibake, and the accountant has a file they cannot use.
 * </p>
 */
export function downloadCsv(name: string, body: string) {
  const url = URL.createObjectURL(new Blob(['\ufeff', body], { type: 'text/csv;charset=utf-8' }))
  const link = document.createElement('a')
  link.href = url
  link.download = name
  link.click()
  URL.revokeObjectURL(url)
}
