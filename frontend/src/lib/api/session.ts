const STORAGE_KEY = 'ans.session'

let token: string | null = localStorage.getItem(STORAGE_KEY)
let onExpired: (() => void) | null = null

/**
 * The session token, held in a module rather than in React state.
 * <p>
 * {@link apiRequest} needs it on every call and is not a component, so reading it from a hook would
 * mean threading it through every api function. Keeping it here also survives a reload, so a
 * refresh mid-bill does not sign the counter out.
 * </p>
 */
export function getToken() {
  return token
}

export function setToken(value: string | null) {
  token = value

  if (value) {
    localStorage.setItem(STORAGE_KEY, value)
  } else {
    localStorage.removeItem(STORAGE_KEY)
  }
}

/**
 * Called when the server rejects the token — expired, or the account was switched off while the tab
 * was open. Lets the provider clear its user without the client importing React.
 */
export function onSessionExpired(handler: (() => void) | null) {
  onExpired = handler
}

export function notifySessionExpired() {
  setToken(null)
  onExpired?.()
}
