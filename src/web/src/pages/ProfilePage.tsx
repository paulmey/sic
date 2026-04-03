import { useState } from 'react';
import { useAuth } from '../AuthContext';
import { api } from '../api';

export default function ProfilePage() {
  const { user, refresh } = useAuth();
  const [displayName, setDisplayName] = useState(user?.displayName ?? '');
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!displayName.trim()) return;
    setSaving(true);
    setMessage('');
    try {
      await api.updateMe({ displayName: displayName.trim() });
      refresh();
      setMessage('Profile updated.');
    } catch (err: unknown) {
      setMessage(err instanceof Error ? err.message : 'Failed to update profile');
    } finally {
      setSaving(false);
    }
  };

  if (!user) return null;

  return (
    <div className="profile-page">
      <h2>Profile</h2>
      <form onSubmit={handleSubmit} className="profile-form">
        <label>
          Display Name
          <input
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            required
            maxLength={50}
          />
        </label>
        <button type="submit" disabled={saving}>
          {saving ? 'Saving...' : 'Save'}
        </button>
        {message && <p className="message">{message}</p>}
      </form>
      <div className="profile-info">
        <p><strong>Identity Provider:</strong> {user.identityProvider}</p>
        <p><strong>Member since:</strong> {new Date(user.createdAt).toLocaleDateString()}</p>
      </div>
      <a href="/.auth/logout" className="btn btn-danger sign-out">Sign out</a>
    </div>
  );
}
