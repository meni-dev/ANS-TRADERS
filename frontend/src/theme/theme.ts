import { alpha, createTheme } from '@mui/material/styles'

/**
 * Design tokens. Every colour the UI uses is picked from one of these ramps rather than being
 * hand-written at the call site, so contrast and hue stay consistent across the app.
 */
const brand = {
  50: '#EEF3FF',
  100: '#DCE7FF',
  200: '#BACFFF',
  300: '#8FB0FF',
  400: '#6B95FF',
  500: '#4880FF',
  600: '#2F63E8',
  700: '#234FBF',
  800: '#1C3F97',
  900: '#173473',
}

const neutral = {
  50: '#F7F8FA',
  100: '#F1F3F7',
  200: '#E7EAF0',
  300: '#D6DBE4',
  400: '#AEB6C4',
  500: '#7A8394',
  600: '#5A6473',
  700: '#3F4855',
  800: '#2A313B',
  900: '#181D24',
}

/**
 * Error red, deliberately desaturated. A validation message is guidance, not an alarm — a
 * saturated signal red on a form the user is still filling in reads as a failure rather than a
 * correction. `main` stays strong enough for destructive buttons and field outlines; the
 * surface/text pair used by alerts is much quieter.
 */
const danger = {
  main: '#D2686C',
  dark: '#A8535A',
  surface: '#FBF5F5',
  border: '#EBD4D5',
  text: '#96565B',
  icon: '#C08287',
}

// Two soft, low-spread shadows do all the elevation work. MUI's default shadows are tuned for
// Material 2 and read as heavy grey smudges against a light grey canvas.
const shadow = {
  sm: '0 1px 2px 0 rgba(24, 29, 36, 0.04), 0 1px 3px 0 rgba(24, 29, 36, 0.06)',
  md: '0 2px 4px -1px rgba(24, 29, 36, 0.04), 0 8px 16px -4px rgba(24, 29, 36, 0.08)',
  lg: '0 8px 24px -6px rgba(24, 29, 36, 0.12), 0 16px 48px -12px rgba(24, 29, 36, 0.14)',
}

/**
 * Tint / solid pairs for the icon chips that head a stat tile or a dashboard module.
 * <p>
 * Five hues, all pulled towards the brand blue so a row of them reads as one family rather than as
 * a paint chart. They label a card; they never encode a value — a figure's own colour still comes
 * from the palette's semantic tones, so nothing on screen means "good" because it happens to be
 * teal.
 * </p>
 */
export const accent = {
  neutral: { tint: '#F1F3F7', solid: '#5A6473' },
  blue: { tint: '#EAF0FF', solid: '#2F63E8' },
  violet: { tint: '#F1EEFC', solid: '#5B45B8' },
  teal: { tint: '#E2F6F2', solid: '#00806D' },
  amber: { tint: '#FDF3E4', solid: '#B87A21' },
  rose: { tint: '#FCF0F1', solid: '#A8535A' },
} as const

export type AccentTone = keyof typeof accent

export const layout = {
  /** The sidebar with labels showing. */
  drawerWidth: 248,
  /** Collapsed: icons only, labels in a flyout. Wide enough for a 32px hit target plus air. */
  railWidth: 60,
  appBarHeight: 52,
  /** Both the drawer and the shifted main column animate on this, so they must agree. */
  sidebarTransition: 'width 180ms cubic-bezier(0.4, 0, 0.2, 1)',
}

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: brand[500],
      light: brand[300],
      dark: brand[700],
      contrastText: '#ffffff',
    },
    secondary: {
      main: neutral[600],
      light: neutral[400],
      dark: neutral[800],
      contrastText: '#ffffff',
    },
    success: { main: '#00A88F', light: '#E4F7F3', dark: '#00806D', contrastText: '#ffffff' },
    error: { main: danger.main, light: danger.surface, dark: danger.dark, contrastText: '#ffffff' },
    warning: { main: '#E9A23B', light: '#FDF3E4', dark: '#B87A21', contrastText: '#ffffff' },
    info: { main: brand[500], light: brand[50], dark: brand[700], contrastText: '#ffffff' },
    grey: neutral,
    background: {
      default: '#F6F7F9',
      paper: '#ffffff',
    },
    text: {
      primary: neutral[900],
      secondary: neutral[500],
      disabled: neutral[400],
    },
    divider: neutral[200],
    action: {
      hover: alpha(neutral[500], 0.06),
      selected: alpha(brand[500], 0.08),
      disabledBackground: neutral[100],
    },
  },

  typography: {
    fontFamily: 'Roboto, -apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif',
    // Page title. Deliberately far smaller than MUI's default h1 — in a dense admin tool an
    // oversized title pushes the actual data below the fold for no informational gain.
    h1: { fontSize: '1.375rem', fontWeight: 700, letterSpacing: '-0.015em', lineHeight: 1.3 },
    h2: { fontSize: '1.125rem', fontWeight: 700, letterSpacing: '-0.01em', lineHeight: 1.35 },
    h3: { fontSize: '1rem', fontWeight: 600, letterSpacing: '-0.005em', lineHeight: 1.4 },
    h4: { fontSize: '0.9375rem', fontWeight: 600, lineHeight: 1.4 },
    h5: { fontSize: '0.875rem', fontWeight: 600, lineHeight: 1.4 },
    h6: { fontSize: '0.8125rem', fontWeight: 600, lineHeight: 1.4 },
    subtitle1: { fontSize: '0.9375rem', fontWeight: 500, lineHeight: 1.5 },
    subtitle2: { fontSize: '0.8125rem', fontWeight: 600, lineHeight: 1.5 },
    body1: { fontSize: '0.875rem', lineHeight: 1.6 },
    body2: { fontSize: '0.8125rem', lineHeight: 1.6 },
    caption: { fontSize: '0.75rem', lineHeight: 1.5 },
    // Used for the small grey group headings inside forms and the sidebar.
    overline: {
      fontSize: '0.6875rem',
      fontWeight: 700,
      letterSpacing: '0.08em',
      textTransform: 'uppercase',
      lineHeight: 1.6,
    },
    button: { fontSize: '0.875rem', fontWeight: 600, letterSpacing: 0 },
  },

  // Base unit for inputs and small controls. Kept low deliberately: sx `borderRadius: n`
  // multiplies this value, so a generous base turns every card into a lozenge.
  shape: {
    borderRadius: 6,
  },

  components: {
    MuiCssBaseline: {
      styleOverrides: {
        html: {
          WebkitFontSmoothing: 'antialiased',
          MozOsxFontSmoothing: 'grayscale',
        },
        body: {
          // A thin, self-coloured scrollbar keeps long forms and tables from looking chunky.
          scrollbarColor: `${neutral[300]} transparent`,
          '&::-webkit-scrollbar, & *::-webkit-scrollbar': {
            width: 10,
            height: 10,
          },
          '&::-webkit-scrollbar-thumb, & *::-webkit-scrollbar-thumb': {
            borderRadius: 8,
            backgroundColor: neutral[300],
            border: '3px solid transparent',
            backgroundClip: 'content-box',
          },
          '&::-webkit-scrollbar-thumb:hover, & *::-webkit-scrollbar-thumb:hover': {
            backgroundColor: neutral[400],
          },
          '&::-webkit-scrollbar-track, & *::-webkit-scrollbar-track': {
            backgroundColor: 'transparent',
          },
        },
      },
    },

    MuiButton: {
      defaultProps: {
        disableElevation: true,
      },
      styleOverrides: {
        root: {
          textTransform: 'none',
          fontWeight: 600,
          borderRadius: 6,
          paddingInline: 16,
          whiteSpace: 'nowrap',
        },
        sizeSmall: { paddingBlock: 5, paddingInline: 12, fontSize: '0.8125rem' },
        sizeMedium: { paddingBlock: 8 },
        sizeLarge: { paddingBlock: 10, fontSize: '0.9375rem' },
        contained: {
          boxShadow: shadow.sm,
          '&:hover': { boxShadow: shadow.md },
        },
        outlined: {
          borderColor: neutral[300],
          color: neutral[700],
          '&:hover': { borderColor: neutral[400], backgroundColor: neutral[50] },
        },
        text: {
          color: neutral[600],
          '&:hover': { backgroundColor: neutral[100] },
        },
      },
    },

    MuiIconButton: {
      styleOverrides: {
        root: {
          borderRadius: 6,
          color: neutral[500],
          '&:hover': { backgroundColor: neutral[100], color: neutral[800] },
        },
      },
    },

    MuiPaper: {
      styleOverrides: {
        root: { backgroundImage: 'none' },
        outlined: { borderColor: neutral[200] },
        elevation1: { boxShadow: shadow.sm },
        elevation2: { boxShadow: shadow.md },
      },
    },

    MuiCard: {
      defaultProps: { variant: 'outlined' },
      styleOverrides: {
        root: {
          borderRadius: 8,
          borderColor: neutral[200],
        },
      },
    },

    MuiAppBar: {
      styleOverrides: {
        root: {
          boxShadow: 'none',
          borderBottom: `1px solid ${neutral[200]}`,
          backgroundColor: alpha('#ffffff', 0.8),
          backdropFilter: 'blur(8px)',
          color: neutral[900],
        },
      },
    },

    MuiDrawer: {
      styleOverrides: {
        paper: {
          borderRight: `1px solid ${neutral[200]}`,
          backgroundColor: '#ffffff',
        },
      },
    },

    // Inputs default to `small` everywhere: the product form has 13 fields, and at MUI's default
    // medium density it does not fit on a laptop screen without scrolling.
    MuiTextField: {
      defaultProps: { size: 'small' },
    },

    MuiOutlinedInput: {
      styleOverrides: {
        root: {
          borderRadius: 6,
          backgroundColor: '#ffffff',
          '& .MuiOutlinedInput-notchedOutline': {
            borderColor: neutral[300],
            transition: 'border-color 120ms ease',
          },
          '&:hover .MuiOutlinedInput-notchedOutline': {
            borderColor: neutral[400],
          },
          '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
            borderWidth: 1,
            borderColor: brand[500],
          },
          '&.Mui-focused': {
            boxShadow: `0 0 0 3px ${alpha(brand[500], 0.16)}`,
          },
          '&.Mui-error .MuiOutlinedInput-notchedOutline': {
            borderColor: danger.main,
          },
          '&.Mui-error.Mui-focused': {
            boxShadow: `0 0 0 3px ${alpha(danger.main, 0.14)}`,
          },
          '&.Mui-disabled': {
            backgroundColor: neutral[50],
          },
        },
        input: {
          '&::placeholder': { color: neutral[400], opacity: 1 },
        },
        // Labels sit above the box rather than floating into the border, so the outline is a plain
        // rectangle. MUI insets the fieldset by -5px to make room for a floating label; with the
        // label gone from there, that inset would lift the outline off the input.
        notchedOutline: {
          top: 0,
          '& legend': { display: 'none' },
        },
      },
    },

    /**
     * The label is a caption above the field, not a word floating in the border. MUI's outlined
     * label rides on the outline and its shrink offset is calibrated for a 16px label; at the 14px
     * this theme uses, the border line crossed the tops of the letters on every field in the app.
     *
     * Taking the label out of the border removes the whole class of problem: nothing has to line up
     * with anything, the label is readable while the field is being typed into, and the outline
     * stays an unbroken rectangle. `shrink` is forced on because there is no longer a resting
     * position inside the box for a label to float up from.
     */
    MuiInputLabel: {
      defaultProps: { shrink: true },
      styleOverrides: {
        root: {
          // Both selectors are needed: MUI re-applies the shrink transform from its own variant,
          // and only a two-class selector outranks it.
          '&.MuiInputLabel-outlined, &.MuiInputLabel-outlined.MuiInputLabel-shrink': {
            position: 'static',
            transform: 'none',
            transition: 'none',
            maxWidth: '100%',
            marginBottom: 4,
            fontSize: '0.75rem',
            fontWeight: 600,
            lineHeight: 1.4,
            color: neutral[600],
            // Focus is already announced by the border and the ring around the box. A caption that
            // changes colour as well is noise on a form with a dozen fields.
            '&.Mui-focused': { color: neutral[600] },
            '&.Mui-error': { color: danger.text },
            '&.Mui-disabled': { color: neutral[400] },
          },
        },
        asterisk: { color: danger.main },
      },
    },

    MuiFormHelperText: {
      styleOverrides: {
        root: {
          marginLeft: 2,
          marginTop: 4,
          fontSize: '0.75rem',
          '&.Mui-error': { color: danger.text },
        },
      },
    },

    MuiChip: {
      styleOverrides: {
        root: { fontWeight: 600, borderRadius: 5 },
        sizeSmall: { height: 22, fontSize: '0.6875rem' },
        outlined: { borderColor: neutral[300], color: neutral[600] },
      },
    },

    MuiTooltip: {
      defaultProps: { arrow: true },
      styleOverrides: {
        tooltip: {
          backgroundColor: neutral[800],
          fontSize: '0.75rem',
          fontWeight: 500,
          borderRadius: 5,
          paddingBlock: 6,
          paddingInline: 10,
        },
        arrow: { color: neutral[800] },
      },
    },

    MuiDialog: {
      styleOverrides: {
        paper: {
          borderRadius: 10,
          boxShadow: shadow.lg,
          // Without an explicit ceiling a tall form dialog grows past the viewport and its
          // action buttons end up unreachable below the fold.
          maxHeight: 'calc(100vh - 64px)',
        },
      },
    },

    MuiBackdrop: {
      styleOverrides: {
        root: { backgroundColor: alpha(neutral[900], 0.4) },
      },
    },

    MuiDialogTitle: {
      styleOverrides: {
        root: { fontSize: '1.0625rem', fontWeight: 700, letterSpacing: '-0.01em' },
      },
    },

    MuiDialogContent: {
      styleOverrides: {
        dividers: { borderColor: neutral[200] },
      },
    },

    MuiListItemButton: {
      styleOverrides: {
        root: {
          borderRadius: 6,
          '&.Mui-selected': {
            backgroundColor: brand[50],
            '&:hover': { backgroundColor: brand[100] },
          },
        },
      },
    },

    MuiListItemIcon: {
      styleOverrides: {
        root: { minWidth: 34, color: neutral[500] },
      },
    },

    MuiDivider: {
      styleOverrides: {
        root: { borderColor: neutral[200] },
      },
    },

    /**
     * Four states, one family.
     *
     * The selectors matter more than they look: MUI 9 emits `MuiAlert-standard` plus
     * `MuiAlert-colorError`, and there is no `MuiAlert-standardError` class at all. This block used
     * to target the old name, so every alert in the app rendered as plain white with grey text —
     * the whole palette below had never once been seen.
     */
    MuiAlert: {
      styleOverrides: {
        root: {
          borderRadius: 8,
          fontSize: '0.8125rem',
          // A message long enough to wrap should read as one block, so the icon sits with the
          // first line rather than floating in the middle of two.
          alignItems: 'flex-start',
          paddingBlock: 8,
          '& .MuiAlert-icon': { paddingBlock: 1, marginRight: 10 },
          '& .MuiAlert-message': { paddingBlock: 0, lineHeight: 1.55 },
          '& .MuiAlertTitle-root': { fontSize: '0.8125rem', marginBottom: 2 },

          // A hairline border carries the boundary so the fill can stay near-white. The old solid
          // pink block shouted louder than the message inside it.
          '&.MuiAlert-standard.MuiAlert-colorError': {
            backgroundColor: danger.surface,
            color: danger.text,
            border: `1px solid ${danger.border}`,
            '& .MuiAlert-icon': { color: danger.icon },
          },
          '&.MuiAlert-standard.MuiAlert-colorSuccess': {
            backgroundColor: '#F4FAF8',
            color: '#3F7168',
            border: '1px solid #D8EAE5',
            '& .MuiAlert-icon': { color: '#6BA396' },
          },
          '&.MuiAlert-standard.MuiAlert-colorWarning': {
            backgroundColor: '#FFFCF5',
            color: '#8A6220',
            border: '1px solid #F5E3C2',
            '& .MuiAlert-icon': { color: '#D09A3C' },
          },
          '&.MuiAlert-standard.MuiAlert-colorInfo': {
            backgroundColor: brand[50],
            color: brand[800],
            border: `1px solid ${brand[100]}`,
            '& .MuiAlert-icon': { color: brand[500] },
          },
        },
      },
    },

    MuiSnackbar: {
      styleOverrides: {
        root: { '& .MuiAlert-root': { boxShadow: shadow.lg } },
      },
    },

    MuiMenu: {
      styleOverrides: {
        paper: {
          borderRadius: 8,
          boxShadow: shadow.md,
          border: `1px solid ${neutral[200]}`,
        },
      },
    },

    MuiMenuItem: {
      styleOverrides: {
        root: { fontSize: '0.875rem', borderRadius: 5, marginInline: 4 },
      },
    },
  },
})

export { brand, neutral, shadow }
