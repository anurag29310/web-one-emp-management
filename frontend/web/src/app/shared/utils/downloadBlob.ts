/**
 * Triggers a browser download for a blob returned by a file-export endpoint
 * (ReportsController's CSV exports, ExportsController's Excel/PDF exports).
 * Creates a temporary object URL and an anchor click, then revokes the URL —
 * the standard pattern for "download this in-memory blob" without a real
 * server-hosted file URL.
 */
export function downloadBlob(blob: Blob, fileName: string): void {
  const url = window.URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  link.remove()
  window.URL.revokeObjectURL(url)
}

/**
 * Pulls the file name out of a `Content-Disposition: attachment; filename="…"`
 * header (RFC 6266). Falls back to `fallback` if the header is missing or the
 * axios/browser environment stripped it (e.g. CORS without an explicit
 * Access-Control-Expose-Headers).
 */
export function extractFileName(contentDisposition: string | undefined, fallback: string): string {
  if (!contentDisposition) return fallback

  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition)
  if (utf8Match?.[1]) {
    try {
      return decodeURIComponent(utf8Match[1])
    } catch {
      // fall through to the plain filename match below
    }
  }

  const plainMatch = /filename="?([^";]+)"?/i.exec(contentDisposition)
  return plainMatch?.[1] ?? fallback
}
