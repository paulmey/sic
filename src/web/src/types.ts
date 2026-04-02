export interface User {
  id: string;
  identityProvider: string;
  identityId: string;
  displayName: string;
  appRoles: string[];
  createdAt: string;
}

export interface Category {
  id: string;
  name: string;
  icon: string;
  createdAt: string;
}

export interface Resource {
  id: string;
  categoryId: string;
  name: string;
  description: string;
  imageUrl: string;
  createdAt: string;
}

export interface Booking {
  id: string;
  resourceId: string;
  userId: string;
  title: string;
  description: string;
  startTime: string;
  endTime: string;
  createdAt: string;
}

export interface InviteLink {
  id: string;
  createdByUserId: string;
  expiresAt: string;
  usedByUserId: string | null;
  createdAt: string;
}

export interface ResourceRole {
  id: string;
  resourceId: string;
  userId: string;
  role: string;
}
