const PERMISSION_CLAIM = 'permission';

function collectStringish(v: unknown, acc: Set<string>): void {
  if (v === null || v === undefined) return;
  if (typeof v === 'string' && v.trim()) {
    acc.add(v.trim());
    return;
  }
  if (typeof v === 'number' && Number.isFinite(v)) {
    acc.add(String(v));
    return;
  }
  if (Array.isArray(v)) {
    for (const x of v) collectStringish(x, acc);
  }
}

/**
 * Reads permission codes from JWT payload. Supports:
 * - `permission` as string, string[], or nested arrays (some serializers)
 * - duplicate claim keys merged into arrays by some JWT decoders
 * - case-insensitive key match for `permission`
 */
export function decodeJwtPayload(token: string): Record<string, unknown> | null {
  try {
    const parts = token.split('.');
    if (parts.length < 2) return null;
    let base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const pad = base64.length % 4;
    if (pad) base64 += '='.repeat(4 - pad);
    const json = atob(base64);
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}

export function readPermissionCodesFromPayload(payload: Record<string, unknown> | null): string[] {
  if (!payload) return [];
  const acc = new Set<string>();

  for (const [key, raw] of Object.entries(payload)) {
    const k = key.toLowerCase();
    if (k === PERMISSION_CLAIM || k.endsWith('/permission')) {
      collectStringish(raw, acc);
    }
  }

  return [...acc];
}

export function readSubFromPayload(payload: Record<string, unknown> | null): string | null {
  if (!payload) return null;
  const sub = payload['sub'];
  if (typeof sub === 'string') return sub;
  const nameId = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
  if (typeof nameId === 'string') return nameId;
  return null;
}

export function readEmailFromPayload(payload: Record<string, unknown> | null): string | null {
  if (!payload) return null;
  const email = payload['email'];
  if (typeof email === 'string') return email;
  return null;
}

export function readUserNameFromPayload(payload: Record<string, unknown> | null): string | null {
  if (!payload) return null;
  const unique = payload['unique_name'];
  if (typeof unique === 'string') return unique;
  const name = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'];
  if (typeof name === 'string') return name;
  return null;
}
