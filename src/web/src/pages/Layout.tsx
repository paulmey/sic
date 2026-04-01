import { Outlet, Link } from 'react-router-dom';
import { useAuth } from '../AuthContext';

export default function Layout() {
  const { user } = useAuth();

  return (
    <div className="layout">
      <header>
        <Link to="/" className="logo">sic</Link>
        <nav>
          <Link to="/">Resources</Link>
          {user && (
            <>
              <span className="user-name">{user.displayName}</span>
              <a href="/.auth/logout">Sign out</a>
            </>
          )}
        </nav>
      </header>
      <main>
        <Outlet />
      </main>
    </div>
  );
}
