import AddOutlinedIcon from '@mui/icons-material/AddOutlined'
import LockOutlinedIcon from '@mui/icons-material/LockOutlined'
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  Divider,
  FormControlLabel,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { useEffect, useMemo, useState } from 'react'
import { ConfirmDialog } from '@/components/feedback/ConfirmDialog'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { ApiError } from '@/lib/api/client'
import { useAuth } from '../AuthProvider'
import {
  useCreateRole,
  useDeleteRole,
  usePermissionCatalogue,
  useRoles,
  useUpdateRole,
} from '../hooks'
import type { Permission, PermissionInfo, Role } from '../types'

function message(caught: unknown, fallback: string) {
  if (caught instanceof ApiError) {
    return Object.values(caught.errors)[0]?.[0] ?? caught.message
  }
  return fallback
}

/** A blank role to start from — nothing ticked, so every grant is a decision somebody made. */
const BLANK = { id: '', name: '', description: '', permissions: [] as Permission[] }

function PermissionGroup({
  group,
  entries,
  chosen,
  disabled,
  onToggle,
}: {
  group: string
  entries: PermissionInfo[]
  chosen: Set<Permission>
  disabled: boolean
  onToggle: (permission: Permission) => void
}) {
  return (
    <Box>
      <Typography
        sx={{ fontSize: 11.5, fontWeight: 700, letterSpacing: '0.06em', color: 'text.disabled', mb: 0.5 }}
      >
        {group.toUpperCase()}
      </Typography>
      <Stack>
        {entries.map((entry) => (
          <FormControlLabel
            key={entry.value}
            sx={{ alignItems: 'flex-start', ml: 0, mb: 0.75 }}
            disabled={disabled}
            control={
              <Checkbox
                size="small"
                sx={{ pt: 0.25 }}
                checked={chosen.has(entry.value)}
                onChange={() => onToggle(entry.value)}
              />
            }
            label={
              <Box>
                <Typography sx={{ fontSize: 13.5, fontWeight: 600 }}>{entry.label}</Typography>
                <Typography sx={{ fontSize: 12, color: 'text.secondary' }}>{entry.description}</Typography>
              </Box>
            }
          />
        ))}
      </Stack>
    </Box>
  )
}

/**
 * Who is allowed to do what. Roles are the part the shop builds; the permissions they are made of
 * are fixed, because each one is only real where a service refuses to run without it.
 */
export function RolesPage() {
  const { can } = useAuth()
  const manages = can('UserManage')

  const { data: roles } = useRoles()
  const { data: catalogue } = usePermissionCatalogue()
  const createRole = useCreateRole()
  const updateRole = useUpdateRole()
  const deleteRole = useDeleteRole()
  const { notify } = useNotification()

  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [draft, setDraft] = useState(BLANK)
  const [error, setError] = useState<string | null>(null)
  const [confirmDelete, setConfirmDelete] = useState<Role | null>(null)

  // Open on whatever role exists, so the panel is never an empty box on first load.
  useEffect(() => {
    if (selectedId === null && roles?.length) {
      setSelectedId(roles[0].id)
    }
  }, [roles, selectedId])

  const selected = roles?.find((role) => role.id === selectedId) ?? null
  const creating = selectedId === ''

  useEffect(() => {
    setError(null)
    setDraft(
      selected
        ? {
            id: selected.id,
            name: selected.name,
            description: selected.description ?? '',
            permissions: selected.permissions,
          }
        : BLANK,
    )
  }, [selected, creating])

  const groups = useMemo(() => {
    const byGroup = new Map<string, PermissionInfo[]>()
    for (const entry of catalogue ?? []) {
      byGroup.set(entry.group, [...(byGroup.get(entry.group) ?? []), entry])
    }
    return [...byGroup.entries()]
  }, [catalogue])

  const chosen = useMemo(() => new Set(draft.permissions), [draft.permissions])

  // The built-in role is shown, and shown as read-only. Hiding it would leave the shop wondering
  // where the owner's permissions live.
  const locked = !manages || (selected?.isSystem ?? false)

  const dirty =
    creating ||
    (selected !== null &&
      (draft.name !== selected.name ||
        (draft.description ?? '') !== (selected.description ?? '') ||
        draft.permissions.length !== selected.permissions.length ||
        draft.permissions.some((p) => !selected.permissions.includes(p))))

  function toggle(permission: Permission) {
    setDraft((current) => ({
      ...current,
      permissions: current.permissions.includes(permission)
        ? current.permissions.filter((p) => p !== permission)
        : [...current.permissions, permission],
    }))
  }

  async function save() {
    setError(null)

    const values = {
      name: draft.name.trim(),
      description: draft.description.trim() || null,
      permissions: draft.permissions,
    }

    try {
      if (creating) {
        const created = await createRole.mutateAsync(values)
        setSelectedId(created.id)
        notify(`'${created.name}' created`)
      } else if (selected) {
        await updateRole.mutateAsync({ id: selected.id, values })
        notify(`'${values.name}' saved`)
      }
    } catch (caught) {
      setError(message(caught, 'Could not reach the server'))
    }
  }

  async function remove(role: Role) {
    try {
      await deleteRole.mutateAsync(role.id)
      setSelectedId(null)
      notify(`'${role.name}' deleted`)
    } catch (caught) {
      notify(message(caught, 'Could not delete the role'), 'error')
    } finally {
      setConfirmDelete(null)
    }
  }

  return (
    <Stack spacing={2}>
      <Typography variant="body2" color="text.secondary">
        A role is a set of things somebody is allowed to do. The list of permissions is fixed — each
        one exists because the app refuses the action without it — but the roles you build out of
        them are yours.
      </Typography>

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ alignItems: 'flex-start' }}>
        <Paper sx={{ width: { xs: '100%', md: 260 }, flexShrink: 0, p: 1 }}>
          <Stack>
            {(roles ?? []).map((role) => (
              <Box
                key={role.id}
                component="button"
                type="button"
                onClick={() => setSelectedId(role.id)}
                sx={{
                  textAlign: 'left',
                  border: 0,
                  cursor: 'pointer',
                  borderRadius: 1,
                  px: 1.5,
                  py: 1,
                  bgcolor: role.id === selectedId ? 'action.selected' : 'transparent',
                  '&:hover': { bgcolor: 'action.hover' },
                }}
              >
                <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
                  <Typography sx={{ fontSize: 13.5, fontWeight: 600 }}>{role.name}</Typography>
                  {role.isSystem && <LockOutlinedIcon sx={{ fontSize: 14, color: 'text.disabled' }} />}
                </Stack>
                <Typography sx={{ fontSize: 11.5, color: 'text.disabled' }}>
                  {role.permissions.length} permissions · {role.userCount}{' '}
                  {role.userCount === 1 ? 'person' : 'people'}
                </Typography>
              </Box>
            ))}

            {manages && (
              <>
                <Divider sx={{ my: 1 }} />
                <Button size="small" startIcon={<AddOutlinedIcon />} onClick={() => setSelectedId('')}>
                  New role
                </Button>
              </>
            )}
          </Stack>
        </Paper>

        <Paper sx={{ flexGrow: 1, minWidth: 0, p: 3 }}>
          <Stack spacing={2.5}>
            {error && <Alert severity="error">{error}</Alert>}

            {selected?.isSystem && (
              <Alert severity="info" icon={<LockOutlinedIcon />}>
                {selected.name} is the built-in role. It holds everything and cannot be changed — one
                wrong tick here could leave a shop where nobody can add a user or unlock the books.
              </Alert>
            )}

            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
              <TextField
                size="small"
                label="Role name"
                value={draft.name}
                onChange={(event) => setDraft({ ...draft, name: event.target.value })}
                disabled={locked}
                sx={{ minWidth: 220 }}
              />
              <TextField
                size="small"
                label="What this role is for"
                value={draft.description}
                onChange={(event) => setDraft({ ...draft, description: event.target.value })}
                disabled={locked}
                fullWidth
              />
            </Stack>

            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <Chip size="small" label={`${draft.permissions.length} of ${catalogue?.length ?? 0}`} />
              {selected && !creating && (
                <Typography variant="caption" color="text.disabled">
                  {selected.userCount} {selected.userCount === 1 ? 'person holds' : 'people hold'} this
                </Typography>
              )}
            </Stack>

            <Box
              sx={{
                display: 'grid',
                gap: 3,
                gridTemplateColumns: { xs: '1fr', lg: '1fr 1fr' },
              }}
            >
              {groups.map(([group, entries]) => (
                <PermissionGroup
                  key={group}
                  group={group}
                  entries={entries}
                  chosen={chosen}
                  disabled={locked}
                  onToggle={toggle}
                />
              ))}
            </Box>

            {manages && !selected?.isSystem && (
              <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
                {selected && !creating && (
                  <Button color="error" onClick={() => setConfirmDelete(selected)}>
                    Delete role
                  </Button>
                )}
                <Button
                  variant="contained"
                  onClick={() => void save()}
                  disabled={
                    !dirty ||
                    createRole.isPending ||
                    updateRole.isPending ||
                    draft.name.trim().length < 2 ||
                    draft.permissions.length === 0
                  }
                >
                  {creating ? 'Create role' : 'Save changes'}
                </Button>
              </Stack>
            )}
          </Stack>
        </Paper>
      </Stack>

      <ConfirmDialog
        open={confirmDelete !== null}
        title={`Delete '${confirmDelete?.name}'?`}
        description={
          confirmDelete && confirmDelete.userCount > 0
            ? `${confirmDelete.userCount} people still hold this role. Move them to another role first.`
            : 'Nobody holds this role, so nothing else changes.'
        }
        confirmLabel="Delete"
        confirmColor="error"
        loading={deleteRole.isPending}
        onConfirm={() => confirmDelete && void remove(confirmDelete)}
        onCancel={() => setConfirmDelete(null)}
      />
    </Stack>
  )
}
