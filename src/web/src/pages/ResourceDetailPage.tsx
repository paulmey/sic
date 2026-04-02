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
  const [startDate, setStartDate] = useState('');
  const [startTime, setStartTime] = useState('');
  const [endDate, setEndDate] = useState('');
  const [endTime, setEndTime] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [editingBookingId, setEditingBookingId] = useState<string | null>(null);
  const [showAll, setShowAll] = useState(false);
  const [showModal, setShowModal] = useState(false);

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
    setStartDate('');
    setStartTime('');
    setEndDate('');
    setEndTime('');
    setEditingBookingId(null);
    setShowModal(false);
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
        startTime: new Date(`${startDate}T${startTime || '00:00'}`).toISOString(),
        endTime: new Date(`${endDate}T${endTime || '23:59'}`).toISOString(),
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

  const toLocalParts = (iso: string) => {
    const d = new Date(iso);
    const offset = d.getTimezoneOffset();
    const local = new Date(d.getTime() - offset * 60000);
    return { date: local.toISOString().slice(0, 10), time: local.toISOString().slice(11, 16) };
  };

  const handleEdit = (booking: Booking) => {
    setEditingBookingId(booking.id);
    setTitle(booking.title);
    setDescription(booking.description ?? '');
    const start = toLocalParts(booking.startTime);
    setStartDate(start.date);
    setStartTime(start.time);
    const end = toLocalParts(booking.endTime);
    setEndDate(end.date);
    setEndTime(end.time);
    setError('');
    setShowModal(true);
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

      <dialog open={showModal} className="booking-modal-backdrop" onClick={(e) => { if (e.target === e.currentTarget) resetForm(); }}>
        <div className="booking-modal">
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
              Start date
              <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} required />
            </label>
            <label>
              Start time <span className="optional">(optional)</span>
              <input type="time" value={startTime} onChange={(e) => setStartTime(e.target.value)} step="1800" placeholder="00:00" />
            </label>
            <label>
              End date
              <input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} required />
            </label>
            <label>
              End time <span className="optional">(optional)</span>
              <input type="time" value={endTime} onChange={(e) => setEndTime(e.target.value)} step="1800" placeholder="23:59" />
            </label>
            <div className="booking-form-actions">
              <button type="submit" disabled={submitting}>
                {submitting ? 'Saving...' : editingBookingId ? 'Update' : 'Book'}
              </button>
              <button type="button" onClick={resetForm}>Cancel</button>
            </div>
          </form>
        </div>
      </dialog>

      <div className="bookings-header">
        <h3>{showAll ? 'All Bookings' : 'Bookings (next 12 months)'}</h3>
        <div>
          <button onClick={() => { setShowModal(true); setError(''); }}>New Booking</button>
          <button
            className={showAll ? 'active' : ''}
            onClick={() => { const next = !showAll; setShowAll(next); loadBookings(next); }}
          >
            {showAll ? 'Next 12 months' : 'Show all'}
          </button>
        </div>
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
                  <button className="btn-sm" onClick={() => handleEdit(b)}>Edit</button>
                  <button className="btn-danger btn-sm" onClick={() => handleDelete(b.id)}>Cancel</button>
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
