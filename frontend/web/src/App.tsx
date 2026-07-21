import { AuthProvider } from '@/app/core/auth/AuthContext'
import { AppRouter } from '@/app/core/routes/AppRouter'

export function App() {
  return (
    <AuthProvider>
      <AppRouter />
    </AuthProvider>
  )
}
