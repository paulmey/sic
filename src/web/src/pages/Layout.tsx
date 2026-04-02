import { Outlet, Link } from 'react-router-dom';
import { useAuth } from '../AuthContext';

export default function Layout() {
  const { user } = useAuth();

  return (
    <div className="layout">
      <header>
        <Link to="/" className="logo"><span className="logo-full">Sharing is Caring</span><span className="logo-short">S=C</span></Link>
        <nav>
          <Link to="/">Bookings</Link>
          {user && user.appRoles.length > 0 && (
            <Link to="/admin">Admin</Link>
          )}
          {user && (
            <>
              <Link to="/profile" className="user-name">{user.displayName}</Link>
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
