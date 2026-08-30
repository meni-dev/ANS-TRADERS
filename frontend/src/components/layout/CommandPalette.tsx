import SearchIcon from '@mui/icons-material/Search'
import { Box, Dialog, InputBase, List, ListItemButton, Typography } from '@mui/material'
import { useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/features/auth/AuthProvider'
import { flattenNavPages, navItems, visibleNavItems } from './navConfig'
import { visibleQuickActions } from './quickActions'
import type { ReactNode } from 'react'

type Entry = {
  id: string
  label: string
  hint: string
  icon: ReactNode
  path: string
  section: 'Actions' | 'Go to'
}

/**
 * Ranks a candidate against what has been typed. Higher is better; 0 means it does not match.
 * <p>
 * Deliberately three plain rules rather than a fuzzy matcher: a shop has around thirty
 * destinations, and on a list that size fuzzy matching mostly produces surprising winners. Whole
 * word, then prefix, then anywhere — and a prefix of the label always beats a hit buried in a hint.
 * </p>
 */
function score(entry: Entry, query: string): number {
  const q = query.toLowerCase()
  const label = entry.label.toLowerCase()

  if (label === q) return 100
  if (label.startsWith(q)) return 80
  if (label.split(/\s+/).some((word) => word.startsWith(q))) return 60
  if (label.includes(q)) return 40
  if (entry.hint.toLowerCase().includes(q)) return 20
  return 0
}

export function CommandPalette({ open, onClose }: { open: boolean; onClose: () => void }) {
  const navigate = useNavigate()
  const { can } = useAuth()
  const [query, setQuery] = useState('')
  const [cursor, setCursor] = useState(0)
  const listRef = useRef<HTMLUListElement | null>(null)

  const entries = useMemo<Entry[]>(() => {
    const actions = visibleQuickActions(can).map<Entry>((action) => ({
      id: `action:${action.path}`,
      label: action.label,
      hint: action.hint,
      icon: action.icon,
      path: action.path,
      section: 'Actions',
    }))

    const actionPaths = new Set(actions.map((action) => action.path))

    const pages = flattenNavPages(visibleNavItems(navItems, can))
      // A destination already offered as an action does not need a second row saying the same thing.
      .filter((page) => !actionPaths.has(page.path))
      .map<Entry>((page) => ({
        id: `page:${page.path}`,
        label: page.label,
        hint: page.group,
        icon: page.icon,
        path: page.path,
        section: 'Go to',
      }))

    return [...actions, ...pages]
  }, [can])

  const results = useMemo(() => {
    if (!query.trim()) return entries

    return entries
      .map((entry) => ({ entry, rank: score(entry, query.trim()) }))
      .filter((row) => row.rank > 0)
      .sort((a, b) => b.rank - a.rank)
      .map((row) => row.entry)
  }, [entries, query])

  useEffect(() => {
    if (open) {
      setQuery('')
      setCursor(0)
    }
  }, [open])

  // The highlighted row has to stay on screen when it is being moved by the keyboard rather than
  // by the pointer.
  useEffect(() => {
    listRef.current?.children[cursor]?.scrollIntoView({ block: 'nearest' })
  }, [cursor])

  const run = (path: string) => {
    onClose()
    navigate(path)
  }

  const handleKeyDown = (event: React.KeyboardEvent) => {
    if (event.key === 'ArrowDown') {
      event.preventDefault()
      setCursor((prev) => (results.length ? (prev + 1) % results.length : 0))
    } else if (event.key === 'ArrowUp') {
      event.preventDefault()
      setCursor((prev) => (results.length ? (prev - 1 + results.length) % results.length : 0))
    } else if (event.key === 'Enter') {
      event.preventDefault()
      const chosen = results[cursor]
      if (chosen) run(chosen.path)
    }
  }

  return (
    <Dialog
      open={open}
      onClose={onClose}
      fullWidth
      maxWidth="sm"
      // Anchored near the top rather than centred: the list grows downwards as you type, and a
      // centred dialog would shuffle the first result under the pointer on every keystroke.
      slotProps={{ paper: { sx: { alignSelf: 'flex-start', mt: '12vh', overflow: 'hidden' } } }}
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 1.25,
          px: 2,
          py: 1.5,
          borderBottom: '1px solid',
          borderColor: 'divider',
        }}
      >
        <SearchIcon sx={{ fontSize: 19, color: 'text.disabled' }} />
        <InputBase
          autoFocus
          fullWidth
          value={query}
          onChange={(event) => {
            setQuery(event.target.value)
            setCursor(0)
          }}
          onKeyDown={handleKeyDown}
          placeholder="Search actions and screens…"
          sx={{ fontSize: 15 }}
        />
        <Typography sx={{ fontSize: 11, color: 'text.disabled', whiteSpace: 'nowrap' }}>
          esc to close
        </Typography>
      </Box>

      <Box sx={{ maxHeight: 380, overflowY: 'auto', py: 0.75 }}>
        {results.length === 0 ? (
          <Typography sx={{ px: 2, py: 3, fontSize: 13.5, color: 'text.disabled' }}>
            Nothing matches “{query}”.
          </Typography>
        ) : (
          <List disablePadding ref={listRef}>
            {results.map((entry, index) => {
              const startsSection = index === 0 || results[index - 1].section !== entry.section

              return (
                <ListItemButton
                  key={entry.id}
                  selected={index === cursor}
                  onMouseMove={() => setCursor(index)}
                  onClick={() => run(entry.path)}
                  sx={{
                    px: 2,
                    py: 0.875,
                    gap: 1.25,
                    borderRadius: 0,
                    mt: startsSection && index > 0 ? 1 : 0,
                    '&.Mui-selected, &.Mui-selected:hover': { bgcolor: 'grey.100' },
                  }}
                >
                  <Box sx={{ display: 'flex', color: 'text.disabled', '& svg': { fontSize: 18 } }}>
                    {entry.icon}
                  </Box>
                  <Box sx={{ minWidth: 0, flexGrow: 1 }}>
                    <Typography sx={{ fontSize: 13.5, fontWeight: 500, lineHeight: 1.4 }}>
                      {entry.label}
                    </Typography>
                    {entry.hint && (
                      <Typography
                        sx={{
                          fontSize: 12,
                          color: 'text.disabled',
                          lineHeight: 1.4,
                          overflow: 'hidden',
                          textOverflow: 'ellipsis',
                          whiteSpace: 'nowrap',
                        }}
                      >
                        {entry.hint}
                      </Typography>
                    )}
                  </Box>
                  <Typography
                    sx={{
                      fontSize: 10.5,
                      fontWeight: 700,
                      letterSpacing: '0.06em',
                      textTransform: 'uppercase',
                      color: 'text.disabled',
                      flexShrink: 0,
                    }}
                  >
                    {entry.section}
                  </Typography>
                </ListItemButton>
              )
            })}
          </List>
        )}
      </Box>
    </Dialog>
  )
}
