/**
 * MUI's Dialog runs its own focus-trap on open, which settles onto the dialog's own root after a
 * field inside it has already called its `autoFocus` — the dialog wins that race and the field's
 * autoFocus prop is silently a no-op. Wired as `slotProps={{ transition: { onEntered:
 * focusFirstField } } }`, this runs after the dialog's own focus handling has already finished, so
 * it reliably wins instead of racing it.
 */
export function focusFirstField(node: HTMLElement) {
  // Skips a disabled leading field (AdjustStockDialog shows a read-only "Recorded Stock" box
  // before the one actually being filled in) — a disabled input can't take focus anyway, but
  // querySelector would still hand it back as the first match.
  node.querySelector<HTMLElement>('input:not([disabled]), textarea:not([disabled])')?.focus()
}
