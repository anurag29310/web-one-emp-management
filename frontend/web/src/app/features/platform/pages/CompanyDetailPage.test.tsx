import { describe, expect, it } from 'vitest'
import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from '@/test/renderWithProviders'
import { CompanyDetailPage } from './CompanyDetailPage'

const ACTIVE_COMPANY_ID = '00000000-0000-0000-0000-00000000c001' // Acme Corporation, status: Active
const SUSPENDED_COMPANY_ID = '00000000-0000-0000-0000-00000000c003' // Initech Solutions, status: Suspended
const PENDING_COMPANY_ID = '00000000-0000-0000-0000-00000000c004' // Umbrella Staffing, status: PendingApproval

async function renderDetail(companyId: string) {
  renderWithProviders(<CompanyDetailPage />, {
    role: 'SuperAdmin',
    path: '/platform/companies/:id',
    route: `/platform/companies/${companyId}`,
  })
  await screen.findByRole('heading', { level: 1 })
}

describe('CompanyDetailPage', () => {
  it('renders company details and admins for an Active company', async () => {
    await renderDetail(ACTIVE_COMPANY_ID)

    expect(screen.getByText('Acme Corporation')).toBeInTheDocument()
    expect(await screen.findByText('acme.admin')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Suspend' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Approve' })).not.toBeInTheDocument()
  })

  it('shows Activate for a Suspended company', async () => {
    await renderDetail(SUSPENDED_COMPANY_ID)

    expect(screen.getByRole('button', { name: 'Activate' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Suspend' })).not.toBeInTheDocument()
  })

  it('shows Approve/Reject for a PendingApproval company', async () => {
    await renderDetail(PENDING_COMPANY_ID)

    expect(screen.getByRole('button', { name: 'Approve' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Reject' })).toBeInTheDocument()
  })

  it('approves a pending company, flipping it to Trial', async () => {
    const user = userEvent.setup()
    await renderDetail(PENDING_COMPANY_ID)

    await user.click(screen.getByRole('button', { name: 'Approve' }))

    await waitFor(() => expect(screen.getByText('Trial')).toBeInTheDocument())
    expect(screen.queryByRole('button', { name: 'Approve' })).not.toBeInTheDocument()
  })

  it('suspends an active company with a reason via the prompt modal', async () => {
    const user = userEvent.setup()
    await renderDetail(ACTIVE_COMPANY_ID)

    await user.click(screen.getByRole('button', { name: 'Suspend' }))
    const dialog = await screen.findByRole('dialog', { name: 'Suspend company' })
    await user.type(within(dialog).getByLabelText(/Reason/), 'Overdue invoice')
    await user.click(within(dialog).getByRole('button', { name: 'Suspend' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    // Suspend replaces the Suspend action with Activate once the company reloads as Suspended —
    // avoids asserting on the status badge text directly, which collides with the "Suspended"
    // field label that's always rendered.
    expect(await screen.findByRole('button', { name: 'Activate' })).toBeInTheDocument()
  })

  it('issues a password reset token for an admin', async () => {
    const user = userEvent.setup()
    await renderDetail(ACTIVE_COMPANY_ID)

    await user.click(await screen.findByRole('button', { name: 'Send password reset' }))

    expect(await screen.findByText(/Password reset token issued/)).toBeInTheDocument()
  })
})
