import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import type { Client, ClientInput, ClientListFilters, ClientRepository } from './clientRepository'
import { mockClients } from './mockData'

let clients = [...mockClients]

function nextId(): string {
  return `00000000-0000-0000-0000-${Date.now().toString().padStart(12, '0')}`
}

function findClientOrThrow(id: string): Client {
  const client = clients.find((c) => c.id === id)
  if (!client) {
    throw new AppError(`Client ${id} was not found.`, 404, 'NOT_FOUND')
  }
  return client
}

export const mockClientRepository: ClientRepository = {
  async list(filters: ClientListFilters = {}): Promise<PagedResult<Client>> {
    await delay(300)
    const { page = 1, pageSize = 20, search, isActive } = filters

    let filtered = clients.filter((c) => !c.isDeleted)
    if (typeof isActive === 'boolean') filtered = filtered.filter((c) => c.isActive === isActive)
    if (search) {
      const term = search.toLowerCase()
      filtered = filtered.filter(
        (c) =>
          c.clientName.toLowerCase().includes(term) ||
          c.companyName.toLowerCase().includes(term) ||
          c.contactPerson.toLowerCase().includes(term) ||
          c.email.toLowerCase().includes(term),
      )
    }

    filtered = [...filtered].sort((a, b) => a.clientName.localeCompare(b.clientName))

    const start = (page - 1) * pageSize
    const pageItems = filtered.slice(start, start + pageSize)

    return {
      data: pageItems,
      page,
      pageSize,
      totalCount: filtered.length,
      totalPages: Math.max(1, Math.ceil(filtered.length / pageSize)),
      correlationId: 'mock-correlation-id',
    }
  },

  async getById(id: string): Promise<Client> {
    await delay(200)
    return findClientOrThrow(id)
  },

  async create(input: ClientInput): Promise<Client> {
    await delay(300)
    if (clients.some((c) => !c.isDeleted && c.clientName.toLowerCase() === input.clientName.toLowerCase())) {
      throw new AppError(`A client named "${input.clientName}" already exists.`, 409, 'VALIDATION_ERROR')
    }
    const client: Client = {
      id: nextId(),
      clientName: input.clientName,
      companyName: input.companyName,
      contactPerson: input.contactPerson,
      mobileNumber: input.mobileNumber,
      alternateMobile: input.alternateMobile ?? null,
      email: input.email,
      gstNumber: input.gstNumber ?? null,
      addressLine1: input.addressLine1,
      addressLine2: input.addressLine2 ?? null,
      city: input.city,
      state: input.state ?? null,
      country: input.country,
      postalCode: input.postalCode,
      latitude: input.latitude ?? null,
      longitude: input.longitude ?? null,
      notes: input.notes ?? null,
      isActive: true,
      isArchived: false,
      isDeleted: false,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: null,
    }
    clients = [client, ...clients]
    return client
  },

  async update(id: string, input: ClientInput): Promise<Client> {
    await delay(300)
    const existing = findClientOrThrow(id)
    const updated: Client = {
      ...existing,
      clientName: input.clientName,
      companyName: input.companyName,
      contactPerson: input.contactPerson,
      mobileNumber: input.mobileNumber,
      alternateMobile: input.alternateMobile ?? null,
      email: input.email,
      gstNumber: input.gstNumber ?? null,
      addressLine1: input.addressLine1,
      addressLine2: input.addressLine2 ?? null,
      city: input.city,
      state: input.state ?? null,
      country: input.country,
      postalCode: input.postalCode,
      latitude: input.latitude ?? null,
      longitude: input.longitude ?? null,
      notes: input.notes ?? null,
      updatedAtUtc: new Date().toISOString(),
    }
    clients = clients.map((c) => (c.id === id ? updated : c))
    return updated
  },

  async remove(id: string): Promise<void> {
    await delay(200)
    findClientOrThrow(id)
    clients = clients.map((c) => (c.id === id ? { ...c, isDeleted: true } : c))
  },

  async activate(id: string): Promise<void> {
    await delay(200)
    findClientOrThrow(id)
    clients = clients.map((c) => (c.id === id ? { ...c, isActive: true } : c))
  },

  async deactivate(id: string): Promise<void> {
    await delay(200)
    findClientOrThrow(id)
    clients = clients.map((c) => (c.id === id ? { ...c, isActive: false } : c))
  },

  async archive(id: string): Promise<void> {
    await delay(200)
    findClientOrThrow(id)
    clients = clients.map((c) => (c.id === id ? { ...c, isArchived: true, isActive: false } : c))
  },

  async restore(id: string): Promise<void> {
    await delay(200)
    findClientOrThrow(id)
    clients = clients.map((c) => (c.id === id ? { ...c, isDeleted: false, isArchived: false } : c))
  },
}
