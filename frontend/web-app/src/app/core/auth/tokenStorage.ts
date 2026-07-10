const REFRESH_TOKEN_KEY = 'ems.refreshToken'

// The access token deliberately never touches localStorage/sessionStorage —
// keeping it in memory limits its exposure to XSS-driven storage scraping.
// The refresh token has to survive a full page reload (there's no httpOnly
// cookie option since the API returns tokens in the JSON body), so it's the
// one piece of secret state that goes to persistent storage.
let inMemoryAccessToken: string | null = null

export const tokenStorage = {
  getAccessToken(): string | null {
    return inMemoryAccessToken
  },
  setAccessToken(token: string | null): void {
    inMemoryAccessToken = token
  },
  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY)
  },
  setRefreshToken(token: string | null): void {
    if (token) {
      localStorage.setItem(REFRESH_TOKEN_KEY, token)
    } else {
      localStorage.removeItem(REFRESH_TOKEN_KEY)
    }
  },
  clear(): void {
    inMemoryAccessToken = null
    localStorage.removeItem(REFRESH_TOKEN_KEY)
  },
}
