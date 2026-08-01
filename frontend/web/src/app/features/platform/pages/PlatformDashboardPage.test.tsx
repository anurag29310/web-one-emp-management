import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import { renderWithProviders } from '@/test/renderWithProviders'
import { PlatformDashboardPage } from './PlatformDashboardPage'

describe('PlatformDashboardPage', () => {
  it('renders cross-company stat tiles and recent registrations', async () => {
    renderWithProviders(<PlatformDashboardPage />, { role: 'SuperAdmin' })

    await waitFor(() => expect(screen.getByText('Total Companies')).toBeInTheDocument())

    // "Active"/"Suspended" also appear as company status badges in the recent-registrations
    // table below, so assert at least one match rather than a single unique one.
    expect(screen.getAllByText('Active').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Suspended').length).toBeGreaterThan(0)
    expect(screen.getByText('Total Employees')).toBeInTheDocument()
    expect(screen.getByText('Recently registered')).toBeInTheDocument()
    expect(await screen.findByText('Acme Corporation')).toBeInTheDocument()
  })
})
