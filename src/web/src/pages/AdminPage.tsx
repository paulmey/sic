import { Link } from 'react-router-dom';
import { useAuth } from '../AuthContext';

export default function AdminPage() {
  const { user } = useAuth();
  if (!user || user.appRoles.length === 0) return <p>Access denied.</p>;

  const hasRole = (role: string) => user.appRoles.includes(role);

  return (
    <div className="admin-page">
      <h2>Admin Panel</h2>
      <div className="admin-links">
        {hasRole('category-admin') && (
          <Link to="/admin/categories" className="admin-card">
            <h3>📁 Categories</h3>
            <p>Create, edit and delete categories</p>
          </Link>
        )}
        {hasRole('resource-admin') && (
          <Link to="/admin/resources" className="admin-card">
            <h3>📦 Resources</h3>
            <p>Manage resources and role assignments</p>
          </Link>
        )}
        {hasRole('user-admin') && (
          <Link to="/admin/users" className="admin-card">
            <h3>👥 Users</h3>
            <p>Manage users and app roles</p>
          </Link>
        )}
        {hasRole('user-admin') && (
          <Link to="/admin/invites" className="admin-card">
            <h3>🔗 Invites</h3>
            <p>Create and manage invite links</p>
          </Link>
        )}
      </div>
    </div>
  );
}
