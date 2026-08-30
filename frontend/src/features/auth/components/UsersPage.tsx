import { describeError } from '@/lib/api/errors'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import { PageHeader } from '@/components/layout/PageHeader'
import ContentCopyOutlinedIcon from '@mui/icons-material/ContentCopyOutlined'
import PersonAddAltOutlinedIcon from '@mui/icons-material/PersonAddAltOutlined'
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  IconButton,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import { useState } from 'react'
import { DialogHeader } from '@/components/feedback/DialogHeader'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { formatDate } from '@/lib/format'
import { useAuth } from '../AuthProvider'
import {
  useChangeUserRole,
  useCreateUser,
  useResetPassword,
  useRoles,
  useSetUserActive,
  useUsers,
} from '../hooks'

/** One sentence for the counter, whichever shape the failure came back in. */
const message = describeError

/**
 * Shown once, right after the server generates it. There is no second chance to read it, so it gets
 * its own panel rather than a toast that slides away while somebody is looking for a pen.
 */
function OneTimePassword({ password, onDone }: { password: string; onDone: () => void }) {
  const { notify } = useNotification()

  return (
    <Alert severity="info" sx={{ alignItems: 'center' }}>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
        <Typography variant="body2">Temporary password:</Typography>
        <Typography sx={{ fontFamily: 'monospace', fontWeight: 700, fontSize: 15 }}>{password}</Typography>
        <Tooltip title="Copy">
          <IconButton
            size="small"
            onClick={() => {
              void navigator.clipboard.writeText(password)
              notify('Copied')
            }}
          >
            <ContentCopyOutlinedIcon sx={{ fontSize: 16 }} />
          </IconButton>
        </Tooltip>
        <Button size="small" onClick={onDone}>
          Done
        </Button>
      </Stack>
      <Typography variant="caption" color="text.secondary">
        It is not stored anywhere readable. If it is lost, reset it again.
      </Typography>
    </Alert>
  )
}

function AddPersonDialog({
  open,
  onClose,
  onCreated,
}: {
  open: boolean
  onClose: () => void
  onCreated: (password: string) => void
}) {
  const createUser = useCreateUser()
  const { data: roles } = useRoles()

  const [name, setName] = useState('')
  const [username, setUsername] = useState('')
  const [roleId, setRoleId] = useState('')
  const [error, setError] = useState<string | null>(null)

  function close() {
    setName('')
    setUsername('')
    setRoleId('')
    setError(null)
    onClose()
  }

  async function submit() {
    setError(null)

    try {
      const created = await createUser.mutateAsync({
        name: name.trim(),
        username: username.trim(),
        roleId,
      })
      onCreated(created.temporaryPassword)
      close()
    } catch (caught) {
      setError(message(caught, 'Could not reach the server'))
    }
  }

  const chosen = roles?.find((role) => role.id === roleId)

  return (
    <Dialog open={open} onClose={close} maxWidth="xs" fullWidth>
      <DialogHeader title="Add a person" onClose={close} />
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField label="Name" value={name} onChange={(e) => setName(e.target.value)} autoFocus fullWidth />
          <TextField
            label="Username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            helperText="What they type to sign in"
            fullWidth
          />
          <TextField
            select
            label="Role"
            value={roleId}
            onChange={(e) => setRoleId(e.target.value)}
            helperText={chosen?.description ?? 'What they will be allowed to do'}
            fullWidth
          >
            {(roles ?? []).map((role) => (
              <MenuItem key={role.id} value={role.id}>
                {role.name}
              </MenuItem>
            ))}
          </TextField>
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={close}>Cancel</Button>
        <Button
          variant="contained"
          onClick={() => void submit()}
          disabled={createUser.isPending || !name.trim() || !username.trim() || !roleId}
        >
          {createUser.isPending ? 'Adding…' : 'Add'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}

export function UsersPage() {
  const { user: me, can } = useAuth()
  const manages = can('UserManage')

  const { data: users, isLoading } = useUsers()
  const { data: roles } = useRoles()
  const resetPassword = useResetPassword()
  const setActive = useSetUserActive()
  const changeRole = useChangeUserRole()
  const { notify } = useNotification()

  const [adding, setAdding] = useState(false)
  const [oneTime, setOneTime] = useState<string | null>(null)

  async function reset(id: string) {
    try {
      setOneTime((await resetPassword.mutateAsync(id)).temporaryPassword)
    } catch (caught) {
      notify(message(caught, 'Could not reset the password'), 'error')
    }
  }

  async function toggle(id: string, isActive: boolean) {
    try {
      await setActive.mutateAsync({ id, isActive })
      notify(isActive ? 'Account switched back on' : 'Account switched off')
    } catch (caught) {
      notify(message(caught, 'Could not change the account'), 'error')
    }
  }

  async function move(id: string, roleId: string) {
    try {
      await changeRole.mutateAsync({ id, roleId })
      notify('Role changed. They will be asked to sign in again.')
    } catch (caught) {
      notify(message(caught, 'Could not change the role'), 'error')
    }
  }

  return (
    <Stack spacing={2}>
      <PageHeader
        title="People"
        icon={<GroupsOutlinedIcon />}
        iconTone="blue"
        caption="Everyone who can sign in, and what each of them is allowed to do. Every document records who created it, so this list is what the audit trail names."
        actions={
          manages && (
            <Button
              variant="contained"
              startIcon={<PersonAddAltOutlinedIcon />}
              onClick={() => setAdding(true)}
            >
              Add person
            </Button>
          )
        }
        flush
      />

      {oneTime && <OneTimePassword password={oneTime} onDone={() => setOneTime(null)} />}

      <Paper sx={{ overflowX: 'auto' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Username</TableCell>
              <TableCell sx={{ minWidth: 180 }}>Role</TableCell>
              <TableCell>Last signed in</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {users?.map((row) => (
              <TableRow key={row.id} sx={{ opacity: row.isActive ? 1 : 0.55 }}>
                <TableCell>
                  <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {row.name}
                    </Typography>
                    {row.id === me?.id && <Chip size="small" label="You" />}
                    {!row.isActive && <Chip size="small" label="Switched off" />}
                    {row.mustChangePassword && row.isActive && (
                      <Chip size="small" color="warning" label="Temporary password" />
                    )}
                  </Stack>
                </TableCell>
                <TableCell>{row.username}</TableCell>
                <TableCell>
                  {manages && row.isActive ? (
                    <TextField
                      select
                      size="small"
                      value={row.roleId}
                      onChange={(event) => void move(row.id, event.target.value)}
                      disabled={changeRole.isPending}
                      fullWidth
                    >
                      {(roles ?? []).map((role) => (
                        <MenuItem key={role.id} value={role.id}>
                          {role.name}
                        </MenuItem>
                      ))}
                    </TextField>
                  ) : (
                    row.roleName
                  )}
                </TableCell>
                <TableCell>{row.lastSignedInAt ? formatDate(row.lastSignedInAt) : 'Never'}</TableCell>
                <TableCell align="right">
                  {manages && (
                    <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
                      <Button size="small" onClick={() => void reset(row.id)} disabled={resetPassword.isPending}>
                        Reset password
                      </Button>
                      {row.id !== me?.id && (
                        <Button
                          size="small"
                          color={row.isActive ? 'error' : 'primary'}
                          onClick={() => void toggle(row.id, !row.isActive)}
                          disabled={setActive.isPending}
                        >
                          {row.isActive ? 'Switch off' : 'Switch on'}
                        </Button>
                      )}
                    </Stack>
                  )}
                </TableCell>
              </TableRow>
            ))}
            {!isLoading && !users?.length && (
              <TableRow>
                <TableCell colSpan={5}>
                  <Box sx={{ py: 3, textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Nobody here yet.
                    </Typography>
                  </Box>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </Paper>

      <AddPersonDialog open={adding} onClose={() => setAdding(false)} onCreated={setOneTime} />
    </Stack>
  )
}
