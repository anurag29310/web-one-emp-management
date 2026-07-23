export type DataSourceMode = 'mock' | 'api'

export interface AppConfig {
  dataSource: DataSourceMode
  apiBaseUrl: string
}

function readDataSource(): DataSourceMode {
  const raw = import.meta.env.VITE_DATA_SOURCE
  if (raw === 'api') return 'api'
  return 'mock'
}

export const appConfig: AppConfig = {
  dataSource: readDataSource(),
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL ?? '/api/v1',
}
