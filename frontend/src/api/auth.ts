import { api } from './client'

export interface LoginResult {
  token: string
  displayName: string
  username: string
  expiresAt: string
}

export const authApi = {
  login: (username: string, password: string) =>
    api.post<LoginResult>('/auth/login', { username, password }),
}
