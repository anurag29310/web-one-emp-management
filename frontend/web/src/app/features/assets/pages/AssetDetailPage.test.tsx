import { describe, expect, it } from 'vitest'
import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from '@/test/renderWithProviders'
import { AssetDetailPage } from './AssetDetailPage'
import type { Role } from '@/app/shared/models/user'

const AVAILABLE_ASSET_ID = '00000000-0000-0000-0000-000000001602' // MacBook Pro 14", Available, never assigned
const ASSIGNED_ASSET_ID = '00000000-0000-0000-0000-000000001601' // Dell Latitude, Assigned to Ava Patel

async function renderDetail(assetId: string, role: Role = 'Admin') {
  renderWithProviders(<AssetDetailPage />, {
    role,
    path: '/assets/:id',
    route: `/assets/${assetId}`,
  })
  await screen.findByRole('heading', { level: 1 })
}

describe('AssetDetailPage', () => {
  it('renders asset details with an empty assignment history when it has never been assigned', async () => {
    await renderDetail(AVAILABLE_ASSET_ID)

    expect(screen.getByText(/MacBook Pro/)).toBeInTheDocument()
    expect(screen.getByText('AST-7C1D4E22')).toBeInTheDocument()
    expect(screen.getByText('This asset has never been assigned.')).toBeInTheDocument()
  })

  it('shows the current holder and an outstanding assignment for an Assigned asset', async () => {
    await renderDetail(ASSIGNED_ASSET_ID)

    // "Ava Patel" legitimately appears twice: once as the "Currently with"
    // field, once as the assignment history row.
    const avaPatelMentions = await screen.findAllByText('Ava Patel')
    expect(avaPatelMentions.length).toBe(2)
    expect(screen.getByText('Outstanding')).toBeInTheDocument()
  })

  it('hides all management actions for an Employee', async () => {
    await renderDetail(ASSIGNED_ASSET_ID, 'Employee')

    expect(screen.queryByRole('button', { name: 'Edit' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Record return' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Delete' })).not.toBeInTheDocument()
  })

  it('disables delete for an asset that is currently Assigned', async () => {
    await renderDetail(ASSIGNED_ASSET_ID)

    expect(screen.getByRole('button', { name: 'Delete' })).toBeDisabled()
  })

  it('does not offer Assign on an asset that is already Assigned', async () => {
    await renderDetail(ASSIGNED_ASSET_ID)

    // "Record return" only appears once the (separately-fetched) assignment
    // history has loaded and resolved an outstanding assignment.
    expect(await screen.findByRole('button', { name: 'Record return' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Assign to employee' })).not.toBeInTheDocument()
  })

  it('assigns an available asset, then allows recording its return', async () => {
    const user = userEvent.setup()
    await renderDetail(AVAILABLE_ASSET_ID)

    await user.click(screen.getByRole('button', { name: 'Assign to employee' }))
    const assignDialog = await screen.findByRole('dialog', { name: 'Assign asset' })
    await user.selectOptions(within(assignDialog).getByLabelText('Employee'), 'Ava Patel')
    await user.click(within(assignDialog).getByRole('button', { name: 'Assign' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(await screen.findByRole('button', { name: 'Record return' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Assign to employee' })).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Record return' }))
    const returnDialog = await screen.findByRole('dialog', { name: 'Record asset return' })
    await user.click(within(returnDialog).getByRole('button', { name: 'Record return' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(await screen.findByRole('button', { name: 'Assign to employee' })).toBeInTheDocument()
  })
})
