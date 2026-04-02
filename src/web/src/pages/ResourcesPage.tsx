import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api';
import type { Category, Resource } from '../types';

export default function ResourcesPage() {
  const [resources, setResources] = useState<Resource[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<string>('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.getCategories().then(setCategories).catch(console.error);
  }, []);

  useEffect(() => {
    setLoading(true);
    api.getResources(selectedCategory || undefined)
      .then(setResources)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [selectedCategory]);

  return (
    <div className="resources-page">

      {categories.length > 0 && (
        <div className="category-filter">
          <button
            className={selectedCategory === '' ? 'active' : ''}
            onClick={() => setSelectedCategory('')}
          >
            All
          </button>
          {categories.map((c) => (
            <button
              key={c.id}
              className={selectedCategory === c.id ? 'active' : ''}
              onClick={() => setSelectedCategory(c.id)}
            >
              {c.icon && <span>{c.icon} </span>}
              {c.name}
            </button>
          ))}
        </div>
      )}

      {loading ? (
        <p>Loading resources...</p>
      ) : resources.length === 0 ? (
        <p>No resources found.</p>
      ) : (
        <div className="resource-grid">
          {resources.map((r) => (
            <Link to={`/resources/${r.id}`} key={r.id} className="resource-card">
              {r.imageUrl && <img src={r.imageUrl} alt={r.name} className="resource-image" />}
              <h3>{r.name}</h3>
              {r.description && <p>{r.description}</p>}
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
