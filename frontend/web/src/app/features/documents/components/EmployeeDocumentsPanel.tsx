import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useAuth } from '@/app/core/auth/useAuth'
import { AppError } from '@/app/shared/models/appError'
import { useEmployeeDocuments } from '../hooks/useEmployeeDocuments'
import { documentRepository, type EmployeeDocument } from '../api'
import {
  ALLOWED_DOCUMENT_EXTENSIONS_LABEL,
  DOCUMENT_TYPES,
  MAX_DOCUMENT_SIZE_LABEL,
  documentUploadSchema,
  type DocumentUploadFormValues,
} from '../types/documentSchema'

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function UploadForm({ employeeId, onUploaded }: { employeeId: string; onUploaded: () => void }) {
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<DocumentUploadFormValues>({
    resolver: zodResolver(documentUploadSchema),
    defaultValues: { documentType: DOCUMENT_TYPES[0], expiresAtUtc: '' },
  })

  async function submit(values: DocumentUploadFormValues) {
    setFormError(null)
    try {
      const file = values.file[0]
      await documentRepository.upload({
        employeeId,
        documentType: values.documentType,
        file,
        expiresAtUtc: values.expiresAtUtc ? new Date(values.expiresAtUtc).toISOString() : undefined,
      })
      reset({ documentType: values.documentType, expiresAtUtc: '' })
      onUploaded()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to upload document.')
    }
  }

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="space-y-3 rounded-lg border border-hairline bg-surface-1 p-4">
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="doc-type" className="mb-1 block text-sm font-medium text-ink-muted">
            Document type
          </label>
          <select
            id="doc-type"
            aria-invalid={Boolean(errors.documentType)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('documentType')}
          >
            {DOCUMENT_TYPES.map((type) => (
              <option key={type} value={type}>
                {type}
              </option>
            ))}
          </select>
          {errors.documentType && <p className="mt-1 text-xs text-danger">{errors.documentType.message}</p>}
        </div>
        <div>
          <label htmlFor="doc-expires" className="mb-1 block text-sm font-medium text-ink-muted">
            Expires on <span className="text-ink-tertiary">(optional)</span>
          </label>
          <input
            id="doc-expires"
            type="date"
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('expiresAtUtc')}
          />
        </div>
      </div>

      <div>
        <label htmlFor="doc-file" className="mb-1 block text-sm font-medium text-ink-muted">
          File
        </label>
        <input
          id="doc-file"
          type="file"
          aria-invalid={Boolean(errors.file)}
          accept="application/pdf,image/jpeg,image/png,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none file:mr-3 file:rounded-md file:border-0 file:bg-primary file:px-3 file:py-1.5 file:text-xs file:font-medium file:text-white focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('file')}
        />
        <p className="mt-1 text-xs text-ink-subtle">
          Allowed: {ALLOWED_DOCUMENT_EXTENSIONS_LABEL}. Max size {MAX_DOCUMENT_SIZE_LABEL}.
        </p>
        {errors.file && <p className="mt-1 text-xs text-danger">{errors.file.message as string}</p>}
      </div>

      {formError && (
        <p role="alert" className="text-sm text-danger">
          {formError}
        </p>
      )}

      <button
        type="submit"
        disabled={isSubmitting}
        className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
      >
        {isSubmitting ? 'Uploading…' : 'Upload document'}
      </button>
    </form>
  )
}

export function EmployeeDocumentsPanel({ employeeId }: { employeeId: string }) {
  const { user } = useAuth()
  const { documents, isLoading, error, refresh } = useEmployeeDocuments(employeeId)
  const [listError, setListError] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)

  const canManage = user?.role === 'Admin' || user?.role === 'HR'
  // Mirrors EmployeeDocumentsController.Upload's own authorization check, which compares the
  // caller's user id directly against the employeeId route parameter.
  const isSelf = user?.id === employeeId
  const canUpload = canManage || isSelf

  async function handleDownload(doc: EmployeeDocument) {
    setListError(null)
    setBusyId(doc.id)
    try {
      const { blob, fileName } = await documentRepository.download(employeeId, doc.id)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = fileName
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
    } catch (err) {
      setListError(err instanceof AppError ? err.message : 'Failed to download document.')
    } finally {
      setBusyId(null)
    }
  }

  async function handleDelete(doc: EmployeeDocument) {
    setListError(null)
    setBusyId(doc.id)
    try {
      await documentRepository.remove(employeeId, doc.id)
      refresh()
    } catch (err) {
      setListError(err instanceof AppError ? err.message : 'Failed to delete document.')
    } finally {
      setBusyId(null)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-[18px] font-medium leading-[1.3] tracking-[-0.2px] text-ink">Documents</h2>
        <p className="text-sm text-ink-subtle">{documents.length} files</p>
      </div>

      {canUpload && <UploadForm employeeId={employeeId} onUploaded={refresh} />}

      {(error || listError) && (
        <p role="alert" className="text-sm text-danger">
          {error ?? listError}
        </p>
      )}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">File</th>
              <th className="px-4 py-3">Type</th>
              <th className="px-4 py-3">Size</th>
              <th className="px-4 py-3">Uploaded</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 2 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={5}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading && documents.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={5}>
                  No documents uploaded yet.
                </td>
              </tr>
            )}

            {!isLoading &&
              documents.map((doc) => (
                <tr key={doc.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3 font-medium text-ink">{doc.originalFileName}</td>
                  <td className="px-4 py-3 text-ink-muted">{doc.documentType}</td>
                  <td className="px-4 py-3 text-ink-muted">{formatFileSize(doc.fileSizeBytes)}</td>
                  <td className="px-4 py-3 text-ink-muted">
                    {new Date(doc.uploadedAtUtc).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex justify-end gap-3">
                      <button
                        type="button"
                        disabled={busyId === doc.id}
                        onClick={() => void handleDownload(doc)}
                        className="text-xs font-medium text-primary-hover hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                      >
                        Download
                      </button>
                      {canManage && (
                        <button
                          type="button"
                          disabled={busyId === doc.id}
                          onClick={() => void handleDelete(doc)}
                          className="text-xs font-medium text-danger hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                        >
                          Delete
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
