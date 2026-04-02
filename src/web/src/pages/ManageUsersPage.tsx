import { useEffect, useState } from 'react';
import { api } from '../api';
import { useAuth } from '../AuthContext';
import type { User } from '../types';

const ALL_ROLES = ['user-admin', 'category-admin', 'resource-admin'];

export default function ManageUsersPage() {
  const { user: currentUser } = useAuth();
  const [users, setUsers] = useState<User[]>([]);
  const [error, setError] = useState('');

  const load = () => {
    api.getUsers().then(setUsers).catch(e => setError(e.message));
  };

  useEffect(load, []);

  const toggleRole = async (user: User, role: string) => {
    const newRoles = user.appRoles.includes(role)
      ? user.appRoles.filter(r => r !== role)
      : [...user.appRoles, role];
    try {
      await api.updateUser(user.id, { appRoles: newRoles });
      load();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to update roles');
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this user? This cannot be undone.')) return;
    try {
      await api.deleteUser(id);
      load();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to delete user');
    }
  };

  return (
    <div className="admin-section">
      <h2>Manage Users</h2>
      {error && <p className="error">{error}</p>}

      <table className="admin-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Provider</th>
            {ALL_ROLES.map(r => <th key={r}>{r.replace('-admin', '')}</th>)}
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {users.map(u => (
            <tr key={u.id}>
              <td>{u.displayName}</td>
              <td>{u.identityProvider}</td>
              {ALL_ROLES.map(role => (
                <td key={role}>
                  <input
                    type="checkbox"
                    checked={u.appRoles.includes(role)}
                    onChange={() => toggleRole(u, role)}
                  />
                </td>
              ))}
              <td>
                {u.id !== currentUser?.id && (
                  <button onClick={() => handleDelete(u.id)} className="danger">Delete</button>
                )}
              </td>
            </tr>
          ))}
          {users.length === 0 && <tr><td colSpan={3 + ALL_ROLES.length}>No users.</td></tr>}
        </tbody>
      </table>
    </div>
  );
}
