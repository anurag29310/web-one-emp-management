import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import { AssetStatusBadge } from './AssetStatusBadge'
import type { AssetStatus } from '../api'

describe('AssetStatusBadge', () => {
  it.each<AssetStatus>(['Available', 'Assigned', 'UnderRepair', 'Retired', 'Lost'])(
    'renders the %s status label',
    (status) => {
      render(<AssetStatusBadge status={status} />)
      expect(screen.getByText(status)).toBeInTheDocument()
    },
  )
})
