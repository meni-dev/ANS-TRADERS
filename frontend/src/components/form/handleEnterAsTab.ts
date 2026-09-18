import type { KeyboardEvent } from 'react'

// The Tally-style flow shop counters expect: Enter walks straight through every field, no mouse
// needed. Chrome also treats a native date input's day/month/year as three separate tab stops and
// swallows Tab internally to move between them — Enter sidesteps that too, since it has no special
// meaning of its own on a date input.
//
// Wired on the form's CAPTURE phase, so this runs before a MUI Select or Autocomplete gets to act
// on the same Enter — otherwise a closed Select opens on the first Enter, confirms on the second,
// and reopens on the third rather than ever advancing, and there would be no single signal (short
// of each component's own internal state) to say "this key was already spent."
//
// The one thing this defers to: an ALREADY OPEN popup — a Select's menu or an Autocomplete's
// listbox — which sets aria-expanded="true" on the control and wants this Enter for itself, to
// confirm whatever option is highlighted (autoHighlight on the customer/product pickers keeps one
// highlighted as soon as there is a match, so a plain type-then-Enter picks it). Everywhere else —
// a closed field, a date input, a plain text/number box — Enter just advances.
//
// A field can also declare, via data-enter-click, that Enter should click a specific element
// instead of moving to whatever is next in DOM order — the last item row's Qty clicks "Add Line"
// so the flow keeps adding rows the way Tally does, and an empty trailing row's Item field clicks a
// hidden control that removes that row and jumps to Payment, so the counter can always get out of
// the item loop with one more Enter rather than being stuck minting blank rows forever.
//
// Buttons are otherwise left alone (Enter should activate the one actually focused, not skip past
// it — buttons that would rather be skipped entirely, like a row's delete icon, are pulled out of
// the tab order with tabIndex={-1} at the source instead of special-cased here). Notes is the one
// multi-line field: Shift+Enter inserts a line the way every other app treats it, and a bare Enter
// still advances rather than trapping the counter there for the rest of the form.
// Typed against a plain HTMLElement, not HTMLFormElement, so this also wires directly onto a
// MUI <Box component="form"> — several dialogs in this codebase build their form that way.
export function handleEnterAsTab(event: KeyboardEvent<HTMLElement>) {
  if (event.key !== 'Enter') return

  const target = event.target as HTMLElement
  if (target.tagName === 'TEXTAREA' && event.shiftKey) return
  if (target.tagName === 'BUTTON') return
  if (target.getAttribute('aria-expanded') === 'true') return

  const clickSelector = target.getAttribute('data-enter-click')
  if (clickSelector) {
    event.preventDefault()
    event.stopPropagation()
    event.currentTarget.querySelector<HTMLElement>(clickSelector)?.click()
    return
  }

  const focusable = Array.from(
    event.currentTarget.querySelectorAll<HTMLElement>(
      'a[href], button, input, select, textarea, [tabindex]',
    ),
  ).filter((el) => !el.hasAttribute('disabled') && el.tabIndex >= 0 && el.offsetParent !== null)

  const index = focusable.indexOf(target)
  if (index === -1) return

  event.preventDefault()
  event.stopPropagation()
  focusable[event.shiftKey ? index - 1 : index + 1]?.focus()
}
