// The backend has no leave-type listing endpoint (leaveTypeId is a bare guid
// FK on LeaveRequestDto) — these are display-only labels for known mock IDs,
// used by the UI for a friendlier hint in the apply form. In API mode the raw
// leaveTypeId is entered directly; this list does not change that behavior.
export const KNOWN_LEAVE_TYPES = [
  { id: '30000000-0000-0000-0000-000000000001', name: 'Casual Leave' },
  { id: '30000000-0000-0000-0000-000000000002', name: 'Sick Leave' },
  { id: '30000000-0000-0000-0000-000000000003', name: 'Earned Leave' },
  { id: '30000000-0000-0000-0000-000000000004', name: 'Unpaid Leave' },
  { id: '30000000-0000-0000-0000-000000000005', name: 'Work From Home' },
] as const

export function leaveTypeName(id: string): string {
  return KNOWN_LEAVE_TYPES.find((t) => t.id === id)?.name ?? id
}
