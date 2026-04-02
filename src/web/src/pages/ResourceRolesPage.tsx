import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { api } from '../api';
import type { Resource, ResourceRole, User } from '../types';

export default function ResourceRolesPage() {
  const { resourceId } = useParams<{ resourceId: string }>();
  const [resource, setResource] = useState<Resource | null>(null);
  const [roles, setRoles] = useState<ResourceRole[]>([]);
  const [users, setUsers] = useState<User[]>([]);
  const [selectedUserId, setSelectedUserId] = useState('');
  const [selectedRole, setSelectedRole] = useState('user');
  const [error, setError] = useState('');

  const load = () => {
    if (!resourceId) return;
    api.getResource(resourceId).then(setResource).catch(e => setError(e.message));
    api.getResourceRoles(resourceId).then(setRoles).catch(e => setError(e.message));
    api.getUsers().then(setUsers).catch(e => setError(e.message));
  };

  useEffect(load, [resourceId]);

  const handleAdd = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!resourceId || !selectedUserId) return;
    setError('');
    try {
      await api.createResourceRole(resourceId, { userId: selectedUserId, role: selectedRole });
      setSelectedUserId('');
      setSelectedRole('user');
      load();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to add role');
    }
  };

  const handleChangeRole = async (userId: string, newRole: string) => {
    if (!resourceId) return;
    try {
      await api.updateResourceRole(resourceId, userId, { role: newRole });
      load();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to update role');
    }
  };

  const handleRemove = async (userId: string) => {
    if (!resourceId || !confirm('Remove this user from the resource?')) return;
    try {
      await api.deleteResourceRole(resourceId, userId);
      load();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to remove role');
    }
  };

  const getUserName = (id: string) => users.find(u => u.id === id)?.displayName ?? id;
  const assignedUserIds = new Set(roles.map(r => r.userId));
  const availableUsers = users.filter(u => !assignedUserIds.has(u.id));

  if (!resource) return <p>Loading...</p>;

  return (
    <div className="admin-section">
      <h2>Roles for: {resource.name}</h2>
      <Link to="/admin/resources">← Back to Resources</Link>
      {error && <p className="error">{error}</p>}

      <form onSubmit={handleAdd} className="admin-form">
        <select value={selectedUserId} onChange={e => setSelectedUserId(e.target.value)} required>
          <option value="">Select user...</option>
          {availableUsers.map(u => <option key={u.id} value={u.id}>{u.displayName}</option>)}
        </select>
        <select value={selectedRole} onChange={e => setSelectedRole(e.target.value)}>
          <option value="user">User</option>
          <option value="manager">Manager</option>
        </select>
        <button type="submit">Add</button>
      </form>

      <table className="admin-table">
        <thead>
          <tr><th>User</th><th>Role</th><th>Actions</th></tr>
        </thead>
        <tbody>
          {roles.map(role => (
            <tr key={role.userId}>
              <td>{getUserName(role.userId)}</td>
              <td>
                <select
                  value={role.role}
                  onChange={e => handleChangeRole(role.userId, e.target.value)}
                >
                  <option value="user">User</option>
                  <option value="manager">Manager</option>
                </select>
              </td>
              <td>
                <button className="btn-sm btn-danger" onClick={() => handleRemove(role.userId)}>Remove</button>
              </td>
            </tr>
          ))}
          {roles.length === 0 && <tr><td colSpan={3}>No roles assigned yet.</td></tr>}
        </tbody>
      </table>
    </div>
  );
}
