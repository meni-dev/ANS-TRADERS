import { Box } from '@mui/material'
import type { SxProps, Theme } from '@mui/material'

/**
 * The shop's mark.
 * <p>
 * One file, used everywhere — the sidebar, the sign-in card and every printed bill. A vector
 * redraw was sharper at sidebar size and was dropped anyway: a mark that differs between the screen
 * and the paper reads as two businesses, and the shop's own artwork is the one that matters.
 * </p>
 * <p>
 * Served from <code>public/</code> rather than imported, so the file can be swapped for a different
 * shop without a rebuild — and so a printed invoice fetches it by a plain URL, which is the one
 * thing every browser's print path handles the same way.
 * </p>
 */
export const SHOP_LOGO_URL = '/ans-logo.png'

type ShopLogoProps = {
  /** Rendered height in pixels. Width follows the mark's own proportions. */
  height: number
  sx?: SxProps<Theme>
}

export function ShopLogo({ height, sx }: ShopLogoProps) {
  return (
    <Box
      component="img"
      src={SHOP_LOGO_URL}
      // Named, not decorative: on a printed bill this is what identifies who issued it, and a
      // screen reader working through the document should say so.
      alt="ANS Traders"
      sx={{
        height,
        width: 'auto',
        display: 'block',
        flexShrink: 0,
        // Chrome and Safari skip background images when printing but always draw an <img>. Being
        // explicit costs nothing and stops a bill printing with an empty gap where the mark was.
        printColorAdjust: 'exact',
        WebkitPrintColorAdjust: 'exact',
        ...sx,
      }}
    />
  )
}
