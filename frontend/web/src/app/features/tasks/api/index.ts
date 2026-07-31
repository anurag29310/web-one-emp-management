import { selectRepository } from '@/app/core/config/selectRepository'
import { mockTaskRepository } from './mockTaskRepository'
import { apiTaskRepository } from './apiTaskRepository'
import type { TaskRepository } from './taskRepository'

export const taskRepository: TaskRepository = selectRepository({
  mock: mockTaskRepository,
  api: apiTaskRepository,
})

export type {
  CreateTaskInput,
  TaskAttachment,
  TaskAttachmentDownload,
  TaskComment,
  TaskItem,
  TaskListFilters,
  TaskPriority,
  TaskRepository,
  TaskStatus,
  UpdateTaskInput,
} from './taskRepository'
