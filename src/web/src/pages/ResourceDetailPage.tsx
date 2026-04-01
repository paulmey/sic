import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { api } from '../api';
import { useAuth } from '../AuthContext';
import type { Booking, Resource } from '../types';

export default function ResourceDetailPage() {
  const { resourceId } = useParams<{ resourceId: string }>();
  const { user } = useAuth();
  const [resource, setResource] = useState<Resource | null>(null);
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Booking form
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [startTime, setStartTime] = useState('');
  const [endTime, setEndTime] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const loadBookings = () => {
    if (!resourceId) return;
    const now = new Date();
    const from = new Date(now.getFullYear(), now.getMonth(), now.getDate()).toISOString();
    const to = new Date(now.getFullYear(), now.getMonth() + 1, now.getDate()).toISOString();
    api.getBookings(resourceId, from, to)
      .then(setBookings)
      .catch(console.error);
  };

  useEffect(() => {
    if (!resourceId) return;
    setLoading(true);
    Promise.all([
      api.getResource(resourceId).then(setResource),
      (() => { loadBookings(); return Promise.resolve(); })(),
    ]).finally(() => setLoading(false));
  }, [resourceId]);

  const handleCreateBooking = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!resourceId) return;
    setSubmitting(true);
    setError('');
    try {
      await api.createBooking(resourceId, {
        title,
        description,
        startTime: new Date(startTime).toISOString(),
        endTime: new Date(endTime).toISOString(),
      });
      setTitle('');
      setDescription('');
      setStartTime('');
      setEndTime('');
      loadBookings();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to create booking');
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (bookingId: string) => {
    if (!resourceId) return;
    try {
      await api.deleteBooking(resourceId, bookingId);
      loadBookings();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to delete booking');
    }
  };

  if (loading) return <p>Loading...</p>;
  if (!resource) return <p>Resource not found.</p>;

  return (
    <div className="resource-detail">
      <h2>{resource.name}</h2>
      {resource.description && <p>{resource.description}</p>}

      <h3>New Booking</h3>
      {error && <p className="error">{error}</p>}
      <form onSubmit={handleCreateBooking} className="booking-form">
        <label>
          Title
          <input value={title} onChange={(e) => setTitle(e.target.value)} maxLength={30} required />
        </label>
        <label>
          Description
          <textarea value={description} onChange={(e) => setDescription(e.target.value)} maxLength={1000} />
        </label>
        <label>
          Start
          <input type="datetime-local" value={startTime} onChange={(e) => setStartTime(e.target.value)} required />
        </label>
        <label>
          End
          <input type="datetime-local" value={endTime} onChange={(e) => setEndTime(e.target.value)} required />
        </label>
        <button type="submit" disabled={submitting}>{submitting ? 'Booking...' : 'Book'}</button>
      </form>

      <h3>Upcoming Bookings</h3>
      {bookings.length === 0 ? (
        <p>No bookings for this period.</p>
      ) : (
        <ul className="booking-list">
          {bookings.map((b) => (
            <li key={b.id} className="booking-item">
              <div>
                <strong>{b.title}</strong>
                <span className="booking-time">
                  {new Date(b.startTime).toLocaleString()} &ndash; {new Date(b.endTime).toLocaleString()}
                </span>
              </div>
              {b.userId === user?.id && (
                <button className="btn-delete" onClick={() => handleDelete(b.id)}>Cancel</button>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
