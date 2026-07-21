import { appConfig } from './env'

/**
 * Single decision point for every feature's repository factory: given a mock
 * and an API implementation of the same interface, returns whichever one
 * `appConfig.dataSource` (VITE_DATA_SOURCE) selects. Callers always receive
 * the interface type, never the concrete implementation, so switching the
 * env var is the only change needed to move a feature between data sources.
 */
export function selectRepository<T>(implementations: { mock: T; api: T }): T {
  return appConfig.dataSource === 'api' ? implementations.api : implementations.mock
}
