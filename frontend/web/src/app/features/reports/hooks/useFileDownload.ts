import { useCallback, useState } from 'react'
import { AppError } from '@/app/shared/models/appError'
import { downloadBlob } from '@/app/shared/utils/downloadBlob'
import type { FileDownload } from '../api'

interface UseFileDownloadResult {
  isDownloading: boolean
  error: string | null
  /** Runs `fetcher`, then triggers a browser download for the blob it resolves with. */
  trigger: (fetcher: () => Promise<FileDownload>) => Promise<void>
}

/** Shared "call an export endpoint, then save the blob it returns" flow for Reports and Exports actions. */
export function useFileDownload(): UseFileDownloadResult {
  const [isDownloading, setIsDownloading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const trigger = useCallback(async (fetcher: () => Promise<FileDownload>) => {
    setIsDownloading(true)
    setError(null)
    try {
      const { blob, fileName } = await fetcher()
      downloadBlob(blob, fileName)
    } catch (err) {
      setError(err instanceof AppError ? err.message : 'Failed to download the export.')
    } finally {
      setIsDownloading(false)
    }
  }, [])

  return { isDownloading, error, trigger }
}
