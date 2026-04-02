import { useEffect, useState } from 'react';
import { api } from '../api';
import type { InviteLink } from '../types';

export default function ManageInvitesPage() {
  const [invites, setInvites] = useState<InviteLink[]>([]);
  const [validityDays, setValidityDays] = useState(7);
  const [copiedId, setCopiedId] = useState<string | null>(null);
  const [error, setError] = useState('');

  const load = () => {
    api.getInvites().then(setInvites).catch(e => setError(e.message));
  };

  useEffect(load, []);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      await api.createInvite(validityDays);
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
        <button type="submit">Create Invite</button>
      </form>

      <table className="admin-table">
        <thead>
          <tr><th>Invite ID</th><th>Expires</th><th>Status</th><th>Actions</th></tr>
        </thead>
        <tbody>
          {invites.map(inv => (
            <tr key={inv.id}>
              <td className="monospace">{inv.id.substring(0, 8)}...</td>
              <td>{formatDate(inv.expiresAt)}</td>
              <td>{inv.usedByUserId ? 'Used' : 'Active'}</td>
              <td>
                {!inv.usedByUserId && (
                  <>
                    <button onClick={() => copyLink(inv.id)}>
                      {copiedId === inv.id ? 'Copied!' : 'Copy Link'}
                    </button>
                    <button onClick={() => handleDelete(inv.id)} className="danger">Revoke</button>
                  </>
                )}
              </td>
            </tr>
          ))}
          {invites.length === 0 && <tr><td colSpan={4}>No invites.</td></tr>}
        </tbody>
      </table>
    </div>
  );
}
