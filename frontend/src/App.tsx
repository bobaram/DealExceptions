import { useState, useEffect } from 'react'
import ExceptionsPage from './pages/ExceptionsPage'
import LoginPage from './pages/LoginPage'
import { setToken } from './api/client'
import type { LoginResult } from './api/auth'

interface AuthState {
  token: string
  username: string
  displayName: string
}

function getStoredAuth(): AuthState | null {
  try {
    const s = sessionStorage.getItem('auth')
    return s ? (JSON.parse(s) as AuthState) : null
  } catch {
    return null
  }
}

export default function App() {
  const [auth, setAuth] = useState<AuthState | null>(() => {
    const stored = getStoredAuth()
    if (stored) setToken(stored.token)
    return stored
  })

  useEffect(() => {
    const handler = () => {
      sessionStorage.removeItem('auth')
      setAuth(null)
    }
    window.addEventListener('auth:expired', handler)
    return () => window.removeEventListener('auth:expired', handler)
  }, [])

  function handleLogin(result: LoginResult) {
    const state: AuthState = {
      token:       result.token,
      username:    result.username,
      displayName: result.displayName,
    }
    sessionStorage.setItem('auth', JSON.stringify(state))
    setToken(result.token)
    setAuth(state)
  }

  function handleLogout() {
    sessionStorage.removeItem('auth')
    setToken(null)
    setAuth(null)
  }

  if (!auth) return <LoginPage onLogin={handleLogin} />
  return <ExceptionsPage username={auth.displayName} onLogout={handleLogout} />
}
