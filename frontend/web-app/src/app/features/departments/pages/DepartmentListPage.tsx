import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useDepartments } from '../hooks/useDepartments'
import { departmentRepository } from '../api'
import { AppError } from '@/app/shared/models/appError'

export function DepartmentListPage() {
  const { departments, isLoading, error, refresh } = useDepartments()
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [name, setName] = useState('')
  const [code, setCode] = useState('')
  const [description, setDescription] = useState('')
  const [formError, setFormError] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)

  async function handleCreate(event: FormEvent) {
    event.preventDefault()
    if (!name.trim()) {
      setFormError('Name is required.')
      return
    }
    setIsSaving(true)
    setFormError(null)
    try {
      await departmentRepository.create({
        name: name.trim(),
        code: code.trim() || undefined,
        description: description.trim() || undefined,
      })
      setName('')
      setCode('')
      setDescription('')
      setIsFormOpen(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to create department.')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleDelete(id: string) {
    await departmentRepository.remove(id)
    refresh()
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Departments</h1>
          <p className="text-sm text-ink-subtle">{departments.length} departments</p>
        </div>
        <button
          type="button"
          onClick={() => setIsFormOpen((open) => !open)}
          className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
        >
          {isFormOpen ? 'Cancel' : 'New department'}
        </button>
      </div>

      {isFormOpen && (
        <form
          onSubmit={handleCreate}
          className="space-y-3 rounded-lg border border-hairline bg-surface-1 p-5"
        >
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="mb-1 block text-sm font-medium text-ink-muted">Name</label>
              <input
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-ink-muted">Code</label>
              <input
                value={code}
                onChange={(e) => setCode(e.target.value)}
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              />
            </div>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-ink-muted">Description</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={2}
              className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            />
          </div>
          {formError && <p className="text-sm text-danger">{formError}</p>}
          <button
            type="submit"
            disabled={isSaving}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:opacity-60"
          >
            {isSaving ? 'Saving…' : 'Create department'}
          </button>
        </form>
      )}

      {error && <p className="text-sm text-danger">{error}</p>}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Name</th>
              <th className="px-4 py-3">Code</th>
              <th className="px-4 py-3">Description</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 3 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={4}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading &&
              departments.map((department) => (
                <tr key={department.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3">
                    <Link
                      to={`/departments/${department.id}`}
                      className="font-medium text-ink hover:text-primary-hover"
                    >
                      {department.name}
                    </Link>
                  </td>
                  <td className="px-4 py-3 font-mono text-ink-subtle">{department.code ?? '—'}</td>
                  <td className="px-4 py-3 text-ink-muted">{department.description ?? '—'}</td>
                  <td className="px-4 py-3 text-right">
                    <button
                      type="button"
                      onClick={() => void handleDelete(department.id)}
                      className="text-xs font-medium text-danger hover:underline"
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
