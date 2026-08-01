import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from '@/test/renderWithProviders'
import { PlatformAuditLogsPage } from './PlatformAuditLogsPage'

async function renderPage() {
  renderWithProviders(<PlatformAuditLogsPage />, { role: 'SuperAdmin' })
  // The page header and the Pagination footer both render "N total" text, so wait on the seeded
  // content itself rather than an ambiguous /total$/ match.
  await screen.findAllByText('Update')
}

describe('PlatformAuditLogsPage', () => {
  it('lists audit log entries across companies', async () => {
    await renderPage()

    expect(screen.getAllByText('Update').length).toBeGreaterThan(0)
  })

  it('filters by company id', async () => {
    const user = userEvent.setup()
    await renderPage()

    await user.type(screen.getByPlaceholderText('Company ID…'), '00000000-0000-0000-0000-00000000c001')

    await waitFor(() => expect(screen.getAllByText(/4 total/).length).toBeGreaterThan(0))
  })

  it('opens the detail modal for a log entry', async () => {
    const user = userEvent.setup()
    await renderPage()

    await user.click(screen.getAllByRole('button', { name: 'View' })[0])

    expect(await screen.findByRole('dialog', { name: 'Audit log entry' })).toBeInTheDocument()
  })
})
