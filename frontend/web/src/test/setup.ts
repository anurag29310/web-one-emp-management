import { afterEach } from 'vitest'
import { cleanup } from '@testing-library/react'
import '@testing-library/jest-dom/vitest'

// RTL's auto-cleanup-after-each-test only self-registers for Jest globals;
// under Vitest with `globals: false` (this repo's explicit-import style) it
// has to be wired up by hand, or DOM from one test leaks into the next.
afterEach(() => {
  cleanup()
})
