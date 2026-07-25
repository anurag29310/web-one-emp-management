import type { OfficeLocation } from './officeLocationRepository'

export const mockOfficeLocations: OfficeLocation[] = [
  {
    id: '00000000-0000-0000-0000-000000000501',
    name: 'Seattle HQ',
    code: 'SEA',
    addressLine1: '100 Main Street',
    addressLine2: null,
    city: 'Seattle',
    state: 'WA',
    country: 'USA',
    timeZoneId: 'America/Los_Angeles',
    isDeleted: false,
    createdAtUtc: '2021-01-01T00:00:00Z',
    updatedAtUtc: null,
  },
  {
    id: '00000000-0000-0000-0000-000000000502',
    name: 'Austin Office',
    code: 'AUS',
    addressLine1: '220 Pine Street',
    addressLine2: null,
    city: 'Austin',
    state: 'TX',
    country: 'USA',
    timeZoneId: 'America/Chicago',
    isDeleted: false,
    createdAtUtc: '2021-02-01T00:00:00Z',
    updatedAtUtc: null,
  },
]
