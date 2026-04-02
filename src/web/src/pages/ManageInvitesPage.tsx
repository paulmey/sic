import { useEffect, useState } from 'react';
import { api } from '../api';
import type { InviteLink, Resource } from '../types';

export default function ManageInvitesPage() {
  const [invites, setInvites] = useState<InviteLink[]>([]);
  const [resources, setResources] = useState<Resource[]>([]);
  const [validityDays, setValidityDays] = useState(7);
  const [selectedResourceId, setSelectedResourceId] = useState('');
  const [copiedId, setCopiedId] = useState<string | null>(null);
  const [error, setError] = useState('');

  const load = () => {
    api.getInvites().then(setInvites).catch(e => setError(e.message));
  };

  useEffect(() => {
    load();
    api.getResources().then(setResources).catch((e: unknown) =>
      setError(e instanceof Error ? e.message : 'Failed to load resources')
    );
  }, []);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      await api.createInvite(validityDays, selectedResourceId || undefined);
      load();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to create invite');
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Revoke this invite?')) return;
    try {
      await api.deleteInvite(id);
      load();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to delete invite');
    }
  };

  const copyLink = (id: string) => {
    const url = `${window.location.origin}/invite/${id}`;
    navigator.clipboard.writeText(url);
    setCopiedId(id);
    setTimeout(() => setCopiedId(null), 2000);
  };

  const formatDate = (iso: string) => new Date(iso).toLocaleDateString();

  const getResourceName = (id: string | null) => {
    if (!id) return '—';
    return resources.find(r => r.id === id)?.name ?? id.substring(0, 8);
  };

  return (
    <div className="admin-section">
      <h2>Manage Invites</h2>
      {error && <p className="error">{error}</p>}

      <form onSubmit={handleCreate} className="admin-form">
        <label>
          Validity (days)
          <input
            type="number"
            value={validityDays}
            onChange={e => setValidityDays(Number(e.target.value))}
            min={1}
            max={90}
          />
        </label>
        <label>
          Grant access to resource
          <select value={selectedResourceId} onChange={e => setSelectedResourceId(e.target.value)}>
            <option value="">None</option>
            {resources.map(r => (
              <option key={r.id} value={r.id}>{r.name}</option>
            ))}
          </select>
        </label>
        <button type="submit">Create Invite</button>
      </form>

      <table className="admin-table">
        <thead>
          <tr><th>Invite ID</th><th>Resource</th><th>Expires</th><th>Status</th><th>Actions</th></tr>
        </thead>
        <tbody>
          {invites.map(inv => (
            <tr key={inv.id}>
              <td className="monospace">{inv.id.substring(0, 8)}...</td>
              <td>{getResourceName(inv.resourceId)}</td>
              <td>{formatDate(inv.expiresAt)}</td>
              <td>{inv.usedByUserId ? 'Used' : 'Active'}</td>
              <td>
                {!inv.usedByUserId && (
                  <>
                    <button className="btn-sm" onClick={() => copyLink(inv.id)}>
                      {copiedId === inv.id ? 'Copied!' : 'Copy Link'}
                    </button>
                    <button className="btn-sm btn-danger" onClick={() => handleDelete(inv.id)}>Revoke</button>
                  </>
                )}
              </td>
            </tr>
          ))}
          {invites.length === 0 && <tr><td colSpan={5}>No invites.</td></tr>}
        </tbody>
      </table>
    </div>
  );
}
