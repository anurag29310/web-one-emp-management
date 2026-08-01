import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from '@/test/renderWithProviders'
import { CompanyRegisterPage } from './CompanyRegisterPage'
import { mockRegistrationSettings } from '../api/mockCompanyRegistrationRepository'

async function fillAndSubmit(overrides: Partial<Record<'companyName' | 'adminUserName' | 'adminEmail', string>> = {}) {
  const user = userEvent.setup()
  await user.type(screen.getByLabelText('Company name'), overrides.companyName ?? 'New Co')
  await user.type(screen.getByLabelText('Your username'), overrides.adminUserName ?? 'newco.admin')
  await user.type(screen.getByLabelText('Your email'), overrides.adminEmail ?? 'admin@newco.example.com')
  await user.type(screen.getByLabelText('Password'), 'Password@123')
  await user.type(screen.getByLabelText('Confirm password'), 'Password@123')
  await user.click(screen.getByRole('button', { name: 'Register company' }))
}

describe('CompanyRegisterPage', () => {
  it('shows a closed message when registration is disabled', async () => {
    mockRegistrationSettings.isPublicRegistrationEnabled = false
    renderWithProviders(<CompanyRegisterPage />, { role: null })

    expect(await screen.findByText('Registration is currently closed.')).toBeInTheDocument()
    mockRegistrationSettings.isPublicRegistrationEnabled = true
  })

  it('shows a pending-approval confirmation when approval is required', async () => {
    mockRegistrationSettings.isPublicRegistrationEnabled = true
    mockRegistrationSettings.requireApprovalForNewCompanies = true
    renderWithProviders(<CompanyRegisterPage />, { role: null })

    await screen.findByLabelText('Company name')
    await fillAndSubmit({ adminEmail: 'pending-admin@newco.example.com', adminUserName: 'pending.admin' })

    expect(await screen.findByText(/awaiting Super Admin approval/)).toBeInTheDocument()
  })

  it('establishes a session and navigates to /dashboard when approval is not required', async () => {
    mockRegistrationSettings.isPublicRegistrationEnabled = true
    mockRegistrationSettings.requireApprovalForNewCompanies = false
    renderWithProviders(<CompanyRegisterPage />, { role: null })

    await screen.findByLabelText('Company name')
    await fillAndSubmit({ adminEmail: 'trial-admin@newco.example.com', adminUserName: 'trial.admin' })

    await waitFor(() => expect(screen.queryByText(/awaiting Super Admin approval/)).not.toBeInTheDocument())
  })
})
