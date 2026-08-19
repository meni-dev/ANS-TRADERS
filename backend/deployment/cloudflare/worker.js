/**
 * Puts api.<your-domain> in front of the Lambda Function URL.
 *
 * A plain proxied CNAME does not work. A Function URL is served by a shared AWS endpoint that
 * decides which function you meant from the Host header, so a request arriving with
 * Host: api.your-domain.com is not recognised and comes back 403. This Worker rebuilds the request
 * against the real function hostname, which sets Host correctly, and passes everything else
 * through untouched — method, path, query, body, and the Authorization header the app signs in with.
 *
 * Set LAMBDA_FUNCTION_URL as a Worker variable, and route api.<your-domain>/* to this Worker.
 */
export default {
  async fetch(request, env) {
    if (!env.LAMBDA_FUNCTION_URL) {
      return new Response('LAMBDA_FUNCTION_URL is not set on this Worker', { status: 500 })
    }

    const incoming = new URL(request.url)
    const target = new URL(env.LAMBDA_FUNCTION_URL)

    target.pathname = incoming.pathname
    target.search = incoming.search

    // Passing `request` as the second argument copies the method, headers and body, while the URL
    // decides the Host. Redirects are left alone rather than followed here, so the browser sees
    // whatever the API actually said.
    return fetch(new Request(target, request), { redirect: 'manual' })
  },
}
