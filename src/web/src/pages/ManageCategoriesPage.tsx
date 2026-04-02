import { useEffect, useState } from 'react';
import { api } from '../api';
import type { Category } from '../types';

export default function ManageCategoriesPage() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [name, setName] = useState('');
  const [icon, setIcon] = useState('');
  const [editingId, setEditingId] = useState<string | null>(null);
  const [error, setError] = useState('');

  const load = () => {
    api.getCategories().then(setCategories).catch(e => setError(e.message));
  };

  useEffect(load, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      if (editingId) {
        await api.updateCategory(editingId, { name, icon });
      } else {
        await api.createCategory({ name, icon });
      }
      setName('');
      setIcon('');
      setEditingId(null);
      load();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to save category');
    }
  };

  const startEdit = (cat: Category) => {
    setEditingId(cat.id);
    setName(cat.name);
    setIcon(cat.icon);
  };

  const cancelEdit = () => {
    setEditingId(null);
    setName('');
    setIcon('');
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this category?')) return;
    try {
      await api.deleteCategory(id);
      load();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to delete');
    }
  };

  return (
    <div className="admin-section">
      <h2>Manage Categories</h2>
      {error && <p className="error">{error}</p>}

      <form onSubmit={handleSubmit} className="admin-form">
        <input value={name} onChange={e => setName(e.target.value)} placeholder="Category name" required />
        <input value={icon} onChange={e => setIcon(e.target.value)} placeholder="Icon (emoji)" />
        <button type="submit">{editingId ? 'Update' : 'Create'}</button>
        {editingId && <button type="button" onClick={cancelEdit}>Cancel</button>}
      </form>

      <table className="admin-table">
        <thead>
          <tr><th>Icon</th><th>Name</th><th>Actions</th></tr>
        </thead>
        <tbody>
          {categories.map(cat => (
            <tr key={cat.id}>
              <td>{cat.icon}</td>
              <td>{cat.name}</td>
              <td>
                <button onClick={() => startEdit(cat)}>Edit</button>
                <button onClick={() => handleDelete(cat.id)} className="danger">Delete</button>
              </td>
            </tr>
          ))}
          {categories.length === 0 && <tr><td colSpan={3}>No categories yet.</td></tr>}
        </tbody>
      </table>
    </div>
  );
}
