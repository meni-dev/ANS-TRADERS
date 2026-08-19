type PrintStylesProps = {
  /** What `@page size` the template is designed for. */
  page: 'A4' | 'A5'
  margin?: string
}

/**
 * The print rules every template shares. The app is a flex shell with a fixed drawer and a sticky
 * app bar; none of that belongs on paper, so printing strips the layout back to plain document flow
 * and shows only the sheet. Templates differ in page size and margin, never in what gets hidden.
 */
export function PrintStyles({ page, margin = '12mm' }: PrintStylesProps) {
  return (
    <style>{`
      @media print {
        @page { size: ${page}; margin: ${margin}; }
        body { background: #fff !important; }
        .no-print { display: none !important; }
        header, aside, nav, .MuiDrawer-root, .MuiAppBar-root { display: none !important; }
        main { padding: 0 !important; }
        .print-sheet {
          border: none !important;
          box-shadow: none !important;
          padding: 0 !important;
          margin: 0 !important;
          max-width: none !important;
        }
        /* A bill should not break mid-line or orphan its totals onto a second sheet. */
        tr, .print-keep { break-inside: avoid; }

        /* On screen the line table carries a min-width and scrolls sideways inside its container.
           Paper cannot scroll — an overflowing table is simply cut off at the margin, losing the
           right-hand columns, which on a tax invoice are the tax and the total. So for print the
           table is released to reflow into whatever width the page has, and the type and padding
           come down to make that fit comfortably. */
        .print-sheet table { min-width: 0 !important; width: 100% !important; }
        .print-sheet [class*="MuiTableCell-root"] {
          padding-left: 5px !important;
          padding-right: 5px !important;
          font-size: 10.5px !important;
          /* The per-column widths are tuned for a wide screen. Held on paper they starve the
             description column, which then wraps a part name over three lines while the number
             columns sit half empty. Releasing them lets the table give the text the slack. */
          width: auto !important;
        }
        /* Keeps the numbers on one line each so the description gets the leftover, not the other
           way round. */
        .print-sheet [class*="MuiTableCell-root"][class*="alignRight"],
        .print-sheet [class*="MuiTableCell-root"][class*="alignCenter"] { white-space: nowrap; }
        .print-sheet [class*="MuiTableCell-root"] p { font-size: 10px !important; }
        .print-sheet [class*="MuiBox-root"] { overflow: visible !important; }
      }
    `}</style>
  )
}
