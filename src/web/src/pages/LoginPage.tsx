import { useAuth } from '../AuthContext';

export default function LoginPage() {
  const { error } = useAuth();

  return (
    <div className="login-page">
      <h1>Sharing is Caring</h1>
      <p>Sign in to manage and book resources.</p>
      {error && <p className="error">{error}</p>}
      <div className="login-buttons">
        <a href="/.auth/login/aad" className="btn">Sign in with Microsoft</a>
        <a href="/.auth/login/google" className="btn">Sign in with Google</a>
      </div>
    </div>
  );
}
