import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api';
import type { Category, Resource } from '../types';

export default function ManageResourcesPage() {
  const [resources, setResources] = useState<Resource[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [editingId, setEditingId] = useState<string | null>(null);
  const [error, setError] = useState('');

  const load = () => {
    api.getResources().then(setResources).catch(e => setError(e.message));
    api.getCategories().then(setCategories).catch(e => setError(e.message));
  };

  useEffect(load, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      if (editingId) {
        await api.updateResource(editingId, { name, categoryId, description });
      } else {
        await api.createResource({ name, categoryId, description });
      }
      setName('');
      setDescription('');
      setCategoryId('');
      setEditingId(null);
      load();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to save resource');
    }
  };

  const startEdit = (res: Resource) => {
    setEditingId(res.id);
    setName(res.name);
    setDescription(res.description);
    setCategoryId(res.categoryId);
  };

  const cancelEdit = () => {
    setEditingId(null);
    setName('');
    setDescription('');
    setCategoryId('');
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this resource?')) return;
    try {
      await api.deleteResource(id);
      load();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to delete');
    }
  };

  const getCategoryName = (id: string) => categories.find(c => c.id === id)?.name ?? '';

  return (
    <div className="admin-section">
      <h2>Manage Resources</h2>
      {error && <p className="error">{error}</p>}

      <form onSubmit={handleSubmit} className="admin-form">
        <input value={name} onChange={e => setName(e.target.value)} placeholder="Resource name" required />
        <input value={description} onChange={e => setDescription(e.target.value)} placeholder="Description" />
        <select value={categoryId} onChange={e => setCategoryId(e.target.value)}>
          <option value="">No category</option>
          {categories.map(c => <option key={c.id} value={c.id}>{c.icon} {c.name}</option>)}
        </select>
        <button type="submit">{editingId ? 'Update' : 'Create'}</button>
        {editingId && <button type="button" onClick={cancelEdit}>Cancel</button>}
      </form>

      <table className="admin-table">
        <thead>
          <tr><th>Name</th><th>Category</th><th>Description</th><th>Actions</th></tr>
        </thead>
        <tbody>
          {resources.map(res => (
            <tr key={res.id}>
              <td>{res.name}</td>
              <td>{getCategoryName(res.categoryId)}</td>
              <td>{res.description}</td>
              <td>
                <button onClick={() => startEdit(res)}>Edit</button>
                <Link to={`/admin/resources/${res.id}/roles`}><button>Roles</button></Link>
                <button onClick={() => handleDelete(res.id)} className="danger">Delete</button>
              </td>
            </tr>
          ))}
          {resources.length === 0 && <tr><td colSpan={4}>No resources yet.</td></tr>}
        </tbody>
      </table>
    </div>
  );
}
