import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from '@/test/renderWithProviders'
import { PlatformSettingsPage } from './PlatformSettingsPage'

describe('PlatformSettingsPage', () => {
  it('loads and toggles the two registration switches, then saves', async () => {
    const user = userEvent.setup()
    renderWithProviders(<PlatformSettingsPage />, { role: 'SuperAdmin' })

    const publicToggle = await screen.findByRole('switch', { name: /Public registration enabled/ })
    expect(publicToggle).toBeChecked()

    await user.click(publicToggle)
    expect(publicToggle).not.toBeChecked()

    await user.click(screen.getByRole('button', { name: 'Save settings' }))

    await waitFor(() => expect(screen.getByText('Platform settings updated.')).toBeInTheDocument())
  })
})
