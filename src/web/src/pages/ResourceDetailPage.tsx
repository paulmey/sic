import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { api } from '../api';
import { useAuth } from '../AuthContext';
import type { Booking, Resource, ResourceRole } from '../types';

export default function ResourceDetailPage() {
  const { resourceId } = useParams<{ resourceId: string }>();
  const { user } = useAuth();
  const [resource, setResource] = useState<Resource | null>(null);
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [userRole, setUserRole] = useState<ResourceRole | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Booking form
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [startTime, setStartTime] = useState('');
  const [endTime, setEndTime] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [editingBookingId, setEditingBookingId] = useState<string | null>(null);
  const [showAll, setShowAll] = useState(false);

  const [expandedBookings, setExpandedBookings] = useState<Set<string>>(new Set());

  const toggleExpand = (id: string) => {
    setExpandedBookings(prev => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id); else next.add(id);
      return next;
    });
  };

  const getDateRange = (all: boolean) => {
    const now = new Date();
    if (all) {
      return {
        from: new Date(now.getTime() - 90 * 24 * 60 * 60 * 1000).toISOString(),
        to: new Date(9999, 11, 31).toISOString(),
      };
    }
    return {
      from: new Date(now.getFullYear(), now.getMonth(), now.getDate()).toISOString(),
      to: new Date(now.getFullYear() + 1, now.getMonth(), now.getDate()).toISOString(),
    };
  };

  const loadBookings = (all?: boolean) => {
    if (!resourceId) return;
    const { from, to } = getDateRange(all ?? showAll);
    api.getBookings(resourceId, from, to)
      .then(setBookings)
      .catch(console.error);
  };

  useEffect(() => {
    if (!resourceId) return;
    setLoading(true);
    const { from, to } = getDateRange(showAll);
    Promise.all([
      api.getResource(resourceId).then(setResource),
      api.getBookings(resourceId, from, to).then(setBookings),
      api.getResourceRoles(resourceId).then((roles) => {
        if (user) {
          const myRole = roles.find((r) => r.userId === user.id) ?? null;
          setUserRole(myRole);
        }
      }),
    ]).catch(console.error).finally(() => setLoading(false));
  }, [resourceId]);

  const resetForm = () => {
    setTitle('');
    setDescription('');
    setStartTime('');
    setEndTime('');
    setEditingBookingId(null);
  };

  const handleSubmitBooking = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!resourceId) return;
    setSubmitting(true);
    setError('');
    try {
      const data = {
        title,
        description,
        startTime: new Date(startTime).toISOString(),
        endTime: new Date(endTime).toISOString(),
      };
      if (editingBookingId) {
        await api.updateBooking(resourceId, editingBookingId, data);
      } else {
        await api.createBooking(resourceId, data);
      }
      resetForm();
      loadBookings();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to save booking');
    } finally {
      setSubmitting(false);
    }
  };

  const toLocalDateTimeString = (iso: string) => {
    const d = new Date(iso);
    const offset = d.getTimezoneOffset();
    const local = new Date(d.getTime() - offset * 60000);
    return local.toISOString().slice(0, 16);
  };

  const handleEdit = (booking: Booking) => {
    setEditingBookingId(booking.id);
    setTitle(booking.title);
    setDescription(booking.description ?? '');
    setStartTime(toLocalDateTimeString(booking.startTime));
    setEndTime(toLocalDateTimeString(booking.endTime));
    setError('');
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
      {resource.imageUrl && <img src={resource.imageUrl} alt={resource.name} className="resource-detail-image" />}
      {resource.description && <p>{resource.description}</p>}

      <h3>{editingBookingId ? 'Edit Booking' : 'New Booking'}</h3>
      {error && <p className="error">{error}</p>}
      <form onSubmit={handleSubmitBooking} className="booking-form">
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
        <button type="submit" disabled={submitting}>
          {submitting ? 'Saving...' : editingBookingId ? 'Update' : 'Book'}
        </button>
        {editingBookingId && (
          <button type="button" onClick={resetForm}>Cancel Edit</button>
        )}
      </form>

      <div className="bookings-header">
        <h3>{showAll ? 'All Bookings' : 'Bookings (next 12 months)'}</h3>
        <button
          className={showAll ? 'active' : ''}
          onClick={() => { const next = !showAll; setShowAll(next); loadBookings(next); }}
        >
          {showAll ? 'Next 12 months' : 'Show all'}
        </button>
      </div>
      {bookings.length === 0 ? (
        <p>No bookings for this period.</p>
      ) : (
        <ul className="booking-list">
          {bookings.map((b) => {
            const isPast = new Date(b.endTime) < new Date();
            return (
            <li key={b.id} className={`booking-item${isPast ? ' past' : ''}`}>
              <div>
                <strong>{b.title}</strong>
                {b.description && (
                  <span className="booking-toggle" onClick={() => toggleExpand(b.id)} title="Toggle description">
                    {expandedBookings.has(b.id) ? '▾' : '▸'}
                  </span>
                )}
                <span className="booking-time">
                  {new Date(b.startTime).toLocaleString()} &ndash; {new Date(b.endTime).toLocaleString()}
                </span>
                {expandedBookings.has(b.id) && b.description && (
                  <p className="booking-description">{b.description}</p>
                )}
              </div>
              <span className="booking-owner">by {b.userDisplayName ?? 'Unknown'}</span>
              {!isPast && (b.userId === user?.id || userRole?.role === 'manager') && (
                <>
                  <button className="btn-edit" onClick={() => handleEdit(b)}>Edit</button>
                  <button className="btn-delete" onClick={() => handleDelete(b.id)}>Cancel</button>
                </>
              )}
            </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
