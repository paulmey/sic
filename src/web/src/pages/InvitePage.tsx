import { useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { useAuth } from '../AuthContext';

export default function InvitePage() {
  const { inviteId } = useParams<{ inviteId: string }>();
  const { user, loading, error } = useAuth();

  useEffect(() => {
    if (inviteId) {
      localStorage.setItem('sic-pending-invite', inviteId);
    }
  }, [inviteId]);

  // Already authenticated and invite was redeemed
  useEffect(() => {
    if (user) {
      window.location.href = '/';
    }
  }, [user]);

  if (loading) return <div className="loading">Accepting invite...</div>;

  if (error) {
    return (
      <div className="login-page">
        <h1>Join via Invite</h1>
        <p>Sign in to accept your invitation.</p>
        <p className="error">{error}</p>
        <div className="login-buttons">
          <a href="/.auth/login/aad" className="btn">Sign in with Microsoft</a>
          <a href="/.auth/login/github" className="btn">Sign in with GitHub</a>
        </div>
      </div>
    );
  }

  return (
    <div className="login-page">
      <h1>Join via Invite</h1>
      <p>Sign in to accept your invitation.</p>
      <div className="login-buttons">
        <a href="/.auth/login/aad" className="btn">Sign in with Microsoft</a>
        <a href="/.auth/login/github" className="btn">Sign in with GitHub</a>
      </div>
    </div>
  );
}
