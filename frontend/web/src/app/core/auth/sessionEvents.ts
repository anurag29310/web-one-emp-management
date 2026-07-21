// Lets the Axios interceptor (outside React) notify AuthContext (inside React)
// that the session ended, without either module importing the other.
type SessionExpiredHandler = () => void

let handler: SessionExpiredHandler | null = null

export const sessionEvents = {
  onSessionExpired(next: SessionExpiredHandler): void {
    handler = next
  },
  emitSessionExpired(): void {
    handler?.()
  },
}
