export interface User {
  id: string;
  email: string;
  name: string;
}

export interface Organization {
  id: string;
  name: string;
  slug: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: User;
  organization: Organization;
  permissions: string[];
}

export interface Member {
  id: string;
  name: string;
  email: string;
  role: string;
  joinedAt: string;
}

export interface Role {
  id: string;
  name: string;
  description: string;
  permissions: string[];
}

export interface ProjectSummary {
  id: string;
  name: string;
  environment: string;
  status: string;
  currency: string;
  isPrimary: boolean;
  createdAt: string;
}

export interface ProjectListResponse {
  items: ProjectSummary[];
  totalCount: number;
}

export interface ProjectResponse {
  id: string;
  organizationId: string;
  name: string;
  slug: string;
  description: string | null;
  environment: string;
  currency: string;
  timeZone: string;
  locale: string;
  country: string;
  status: string;
  isPrimary: boolean;
  isInUse: boolean;
  createdAt: string;
  updatedAt: string | null;
  archivedAt: string | null;
  disabledAt: string | null;
}

export interface CreateProjectRequest {
  name: string;
  description?: string;
  environment: string;
  currency: string;
  timeZone: string;
  locale: string;
  country: string;
}

export interface UpdateProjectRequest {
  name?: string;
  description?: string;
  currency?: string;
  timeZone?: string;
  locale?: string;
  country?: string;
  environment?: string;
}

export const ENVIRONMENTS = ['Sandbox', 'Development', 'Staging', 'Production'] as const;
export const CURRENCIES = ['BRL', 'USD', 'EUR', 'GBP', 'ARS', 'CLP', 'COP', 'MXN'] as const;
export const TIMEZONES = [
  'America/Sao_Paulo', 'America/New_York', 'America/Chicago', 'America/Denver',
  'America/Los_Angeles', 'America/Mexico_City', 'America/Argentina/Buenos_Aires',
  'America/Santiago', 'America/Bogota', 'America/Lima',
  'Europe/London', 'Europe/Lisbon', 'Europe/Madrid', 'Europe/Paris', 'Europe/Berlin',
  'UTC',
] as const;
export const LOCALES = ['pt-BR', 'en-US', 'es-AR', 'es-ES', 'fr-FR', 'de-DE'] as const;
export const COUNTRIES = ['BR', 'US', 'AR', 'CL', 'CO', 'MX', 'PE', 'GB', 'PT', 'ES', 'FR', 'DE'] as const;

// R001 — Audit log types
export interface AuditLogEntry {
  id: string;
  organizationId: string;
  projectId?: string;
  action: string;
  resourceType: string;
  resourceId: string;
  actorUserId?: string;
  metadata?: Record<string, unknown>;
  createdAt: string;
}

export interface AuditLogListResponse {
  items: AuditLogEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export function getStatusBadge(status: string): { label: string; color: string } {
  switch (status) {
    case 'Active': return { label: 'Ativo', color: 'bg-green-500/20 text-green-400 border-green-800/50' };
    case 'Disabled': return { label: 'Desativado', color: 'bg-red-500/20 text-red-400 border-red-800/50' };
    case 'Archived': return { label: 'Arquivado', color: 'bg-slate-500/20 text-slate-400 border-slate-800/50' };
    default: return { label: status, color: 'bg-slate-500/20 text-slate-400 border-slate-800/50' };
  }
}

export function getEnvBadge(env: string): { label: string; color: string } {
  switch (env) {
    case 'Production': return { label: 'Produção', color: 'bg-amber-500/20 text-amber-400 border-amber-800/50' };
    case 'Staging': return { label: 'Homologação', color: 'bg-blue-500/20 text-blue-400 border-blue-800/50' };
    case 'Development': return { label: 'Desenvolvimento', color: 'bg-indigo-500/20 text-indigo-400 border-indigo-800/50' };
    case 'Sandbox': return { label: 'Sandbox', color: 'bg-emerald-500/20 text-emerald-400 border-emerald-800/50' };
    default: return { label: env, color: 'bg-slate-500/20 text-slate-400 border-slate-800/50' };
  }
}

// R002 — Brand types
export interface BrandAddress {
  street: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
}

export interface BrandResponse {
  id: string;
  projectId: string;
  name: string;
  description: string | null;
  websiteUrl: string | null;
  termsUrl: string | null;
  privacyUrl: string | null;
  supportEmail: string | null;
  logoUrl: string | null;
  primaryColor: string | null;
  secondaryColor: string | null;
  address: BrandAddress | null;
  createdAt: string;
  updatedAt: string | null;
}

// R002 — Store types
export interface StoreSummaryResponse {
  id: string;
  name: string;
  code: string;
  status: string;
  city?: string;
  country?: string;
}

export interface StoreResponse extends StoreSummaryResponse {
  projectId: string;
  description: string | null;
  storeType: string;
  addressLine1: string | null;
  addressLine2: string | null;
  state: string | null;
  postalCode: string | null;
  contactEmail: string | null;
  contactPhone: string | null;
  geoLocationId: string | null;
  createdAt: string;
  updatedAt: string | null;
}

// R002 — Area types
export interface AreaResponse {
  id: string;
  name: string;
  description: string | null;
  parentAreaId: string | null;
  depth: number;
  storeCount?: number;
  children?: AreaResponse[];
}

// R002 — GeoLocation types
export interface GeoLocationResponse {
  id: string;
  name: string;
  description: string | null;
  type: string;
  coordinates: string;
  latitude: number | null;
  longitude: number | null;
  radius: number | null;
  unit: string | null;
  createdAt: string;
}
