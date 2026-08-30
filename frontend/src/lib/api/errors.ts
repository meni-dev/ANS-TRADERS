import { ApiError } from './client'

/**
 * What the server actually objected to, one line per objection.
 * <p>
 * A validation response carries its useful text in <code>errors</code> and puts the developer's
 * word — "Validation failed" — in <code>message</code>. Reading <code>message</code> is the obvious
 * thing to do and it shows the counter a sentence that tells them nothing, while the sentence that
 * would have helped sits one field away.
 * </p>
 */
export function fieldErrors(error: unknown): string[] {
  if (!(error instanceof ApiError)) return []

  return Object.values(error.errors)
    .flat()
    .filter((line): line is string => typeof line === 'string' && line.trim().length > 0)
}

/**
 * Copy for the failures the counter cannot do anything about by re-reading a field. Each one says
 * what happened, whether anything was saved, and what to do — in that order, because "was it
 * recorded?" is the question somebody standing at a counter actually has.
 *
 * `CONCURRENT_UPDATE` is deliberately absent: the server knows which row lost the race and writes a
 * sentence naming it, and a generic line here would throw that away.
 */
const BY_CODE: Record<string, string> = {
  DUPLICATE_KEY:
    'Two people saved at the very same moment. Nothing was recorded — try once more.',
  STOCK_WOULD_GO_NEGATIVE:
    'The goods on this document have already moved on, so it can no longer be undone.',
  CASH_DAY_CLOSED:
    'That day has already been counted and closed, so cash cannot be added to it.',
  CASH_WOULD_GO_NEGATIVE: 'There is not that much in the till.',
  SESSION_EXPIRED: 'You have been signed out. Sign in again to carry on.',
}

const BY_STATUS: Record<number, string> = {
  0: 'The shop’s server cannot be reached. Check the internet connection and try again — nothing was saved.',
  401: 'You have been signed out. Sign in again to carry on.',
  403: 'Your role does not allow this. Ask the owner if you need it.',
  404: 'That is no longer there. It may have been removed while this screen was open.',
  408: 'That took too long and was given up on. Nothing was saved — try again.',
  500: 'Something went wrong at our end. Nothing was saved — please try again.',
  502: 'The shop’s server is not answering. Nothing was saved — try again in a moment.',
  503: 'The shop’s server is busy. Nothing was saved — try again in a moment.',
}

/**
 * One sentence a shop owner can act on, for anything that was thrown.
 * <p>
 * Order matters. A field objection is the most specific thing the server can say and is already
 * written in shop language by the service that raised it, so it wins. A known code comes next. Only
 * then the server's own message — and a stack of last resorts under that, because the one thing
 * this must never do is show somebody at a counter the word "undefined".
 * </p>
 */
export function describeError(error: unknown, fallback?: string): string {
  const fields = fieldErrors(error)
  if (fields.length > 0) return fields[0]

  if (error instanceof ApiError) {
    const known = BY_CODE[error.code] ?? BY_STATUS[error.status]
    if (known) return known

    // A message the server wrote for a person — the services phrase their own conflicts properly.
    // "Validation failed" is the one to step over, since it only ever appears with field errors and
    // those were handled above.
    if (error.message && error.message !== 'Validation failed' && error.message !== 'Request failed') {
      return error.message
    }
  }

  return fallback ?? 'Something went wrong. Nothing was saved — please try again.'
}

/**
 * Every objection, for a form that wants to list them rather than show the first.
 * Falls back to the single description so a caller can always render something.
 */
export function describeErrors(error: unknown, fallback?: string): string[] {
  const fields = fieldErrors(error)
  return fields.length > 0 ? fields : [describeError(error, fallback)]
}
