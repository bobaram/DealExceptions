import { api } from './client'
import type { Comment } from '../types'

export const commentsApi = {
  add: (exceptionId: number, authorName: string, text: string) =>
    api.post<Comment>(`/exceptions/${exceptionId}/comments`, { authorName, text }),
}
