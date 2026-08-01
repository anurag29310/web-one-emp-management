import { describe, expect, it } from 'vitest'
import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from '@/test/renderWithProviders'
import { CompanyListPage } from './CompanyListPage'

async function renderPage() {
  renderWithProviders(<CompanyListPage />, { role: 'SuperAdmin' })
  // Both the page header and the Pagination footer render "N total" text, so wait on the
  // seeded content itself rather than an ambiguous /total$/ match.
  await screen.findByText('Acme Corporation')
}

describe('CompanyListPage', () => {
  it('lists the seeded companies once loaded', async () => {
    await renderPage()

    expect(screen.getByText('Acme Corporation')).toBeInTheDocument()
    expect(screen.getByText('Globex Industries')).toBeInTheDocument()
  })

  it('filters companies by search term', async () => {
    const user = userEvent.setup()
    await renderPage()

    await user.type(screen.getByLabelText('Search companies'), 'acme')

    await waitFor(() => {
      expect(screen.getByText('Acme Corporation')).toBeInTheDocument()
      expect(screen.queryByText('Globex Industries')).not.toBeInTheDocument()
    })
  })

  it('filters companies by status', async () => {
    const user = userEvent.setup()
    await renderPage()

    await user.selectOptions(screen.getByLabelText('Filter by status'), 'Suspended')

    await waitFor(() => {
      expect(screen.getByText('Initech Solutions')).toBeInTheDocument()
      expect(screen.queryByText('Acme Corporation')).not.toBeInTheDocument()
    })
  })

  it('creates a new company through the modal form', async () => {
    const user = userEvent.setup()
    await renderPage()

    await user.click(screen.getByRole('button', { name: 'New company' }))
    const dialog = await screen.findByRole('dialog', { name: 'New company' })
    await user.type(within(dialog).getByLabelText('Company name'), 'Stark Industries')
    await user.click(within(dialog).getByRole('button', { name: 'Create company' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(await screen.findByText('Stark Industries')).toBeInTheDocument()
  })
})
