import type { User, Category, Resource, Booking, InviteLink } from './types';

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(url, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...options?.headers },
  });
  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error(body.error || `Request failed: ${res.status}`);
  }
  if (res.status === 204) return undefined as T;
  return res.json();
}

export const api = {
  // Auth
  getMe: () => request<User>('/api/me'),

  // Categories
  getCategories: () => request<Category[]>('/api/categories'),
  createCategory: (data: { name: string; icon?: string }) =>
    request<Category>('/api/categories', { method: 'POST', body: JSON.stringify(data) }),
  updateCategory: (id: string, data: { name: string; icon?: string }) =>
    request<Category>(`/api/categories/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  deleteCategory: (id: string) =>
    request<void>(`/api/categories/${id}`, { method: 'DELETE' }),

  // Resources
  getResources: (categoryId?: string) =>
    request<Resource[]>(`/api/resources${categoryId ? `?categoryId=${categoryId}` : ''}`),
  getResource: (id: string) => request<Resource>(`/api/resources/${id}`),
  createResource: (data: { name: string; categoryId?: string; description?: string }) =>
    request<Resource>('/api/resources', { method: 'POST', body: JSON.stringify(data) }),
  updateResource: (id: string, data: { name: string; categoryId?: string; description?: string }) =>
    request<Resource>(`/api/resources/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  deleteResource: (id: string) =>
    request<void>(`/api/resources/${id}`, { method: 'DELETE' }),

  // Bookings
  getBookings: (resourceId: string, from: string, to: string) =>
    request<Booking[]>(`/api/resources/${resourceId}/bookings?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`),
  createBooking: (resourceId: string, data: { title: string; description?: string; startTime: string; endTime: string }) =>
    request<Booking>(`/api/resources/${resourceId}/bookings`, { method: 'POST', body: JSON.stringify(data) }),
  deleteBooking: (resourceId: string, bookingId: string) =>
    request<void>(`/api/resources/${resourceId}/bookings/${bookingId}`, { method: 'DELETE' }),

  // Invites
  getInvites: () => request<InviteLink[]>('/api/invites'),
  createInvite: (validityDays?: number) =>
    request<InviteLink>('/api/invites', { method: 'POST', body: JSON.stringify({ validityDays }) }),
  deleteInvite: (id: string) =>
    request<void>(`/api/invites/${id}`, { method: 'DELETE' }),
  redeemInvite: (inviteId: string) =>
    request<User>('/api/invite/redeem', { method: 'POST', body: JSON.stringify({ inviteId }) }),
};
