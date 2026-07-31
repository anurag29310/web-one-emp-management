import { describe, expect, it } from 'vitest'
import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from '@/test/renderWithProviders'
import { AssetListPage } from './AssetListPage'

async function renderPage(role: 'Admin' | 'HR' | 'Manager' | 'Employee' = 'Admin') {
  renderWithProviders(<AssetListPage />, { role })
  // Wait for the mock repository's simulated network delay to resolve.
  await waitFor(() => expect(screen.queryByText(/total$/)).toBeInTheDocument())
}

describe('AssetListPage', () => {
  it('lists the seeded assets once loaded', async () => {
    await renderPage()

    expect(screen.getByText('AST-3F2A9B10')).toBeInTheDocument()
    expect(screen.getByText('AST-7C1D4E22')).toBeInTheDocument()
  })

  it('filters assets by search term', async () => {
    const user = userEvent.setup()
    await renderPage()

    await user.type(screen.getByLabelText('Search assets'), 'dell')

    await waitFor(() => {
      expect(screen.getByText('AST-3F2A9B10')).toBeInTheDocument()
      expect(screen.queryByText('AST-7C1D4E22')).not.toBeInTheDocument()
    })
  })

  it('filters assets by status', async () => {
    const user = userEvent.setup()
    await renderPage()

    await user.selectOptions(screen.getByLabelText('Filter by status'), 'Retired')

    await waitFor(() => {
      expect(screen.getByText('AST-1B6E8A44')).toBeInTheDocument()
      expect(screen.queryByText('AST-3F2A9B10')).not.toBeInTheDocument()
    })
  })

  it('shows the register-asset action for Admin', async () => {
    await renderPage('Admin')
    expect(screen.getByRole('button', { name: 'Register asset' })).toBeInTheDocument()
  })

  it('hides the register-asset action for an Employee', async () => {
    await renderPage('Employee')
    expect(screen.queryByRole('button', { name: 'Register asset' })).not.toBeInTheDocument()
  })

  it('registers a new asset through the modal form', async () => {
    const user = userEvent.setup()
    await renderPage()

    await user.click(screen.getByRole('button', { name: 'Register asset' }))

    const dialog = await screen.findByRole('dialog', { name: 'Register asset' })
    await user.type(within(dialog).getByLabelText('Category'), 'Tablet')
    await user.type(within(dialog).getByLabelText('Brand'), 'Samsung')
    await user.type(within(dialog).getByLabelText('Model'), 'Galaxy Tab S9')
    await user.type(within(dialog).getByLabelText('Serial number'), 'SN-TEST-999')
    await user.type(within(dialog).getByLabelText('Purchase date'), '2026-07-01')
    await user.clear(within(dialog).getByLabelText('Purchase cost'))
    await user.type(within(dialog).getByLabelText('Purchase cost'), '650')

    await user.click(within(dialog).getByRole('button', { name: 'Register asset' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(await screen.findByText('SN-TEST-999')).toBeInTheDocument()
  })
})
