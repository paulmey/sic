import { createContext, useContext, useCallback, useEffect, useState, type ReactNode } from 'react';
import type { User } from './types';
import { api } from './api';

interface AuthState {
  user: User | null;
  loading: boolean;
  error: string | null;
  refresh: () => void;
}

const AuthContext = createContext<AuthState>({
  user: null,
  loading: true,
  error: null,
  refresh: () => {},
});

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(() => {
    setLoading(true);
    setError(null);

    // Check SWA's built-in auth endpoint first (always 200, no auth required)
    fetch('/.auth/me')
      .then(r => r.json())
      .then(async (authData) => {
        if (!authData.clientPrincipal) {
          // Not authenticated — don't call any API
          setUser(null);
          setLoading(false);
          return;
        }

        const pendingInvite = localStorage.getItem('sic-pending-invite');
        if (pendingInvite) {
          localStorage.removeItem('sic-pending-invite');
        }

        try {
          const user = pendingInvite
            ? await api.redeemInvite(pendingInvite)
            : await api.getMe();
          setUser(user);
        } catch (err: unknown) {
          setError(err instanceof Error ? err.message : 'Authentication failed');
        } finally {
          setLoading(false);
        }
      })
      .catch(() => {
        setUser(null);
        setLoading(false);
      });
  }, []);

  useEffect(refresh, [refresh]);

  return (
    <AuthContext.Provider value={{ user, loading, error, refresh }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => useContext(AuthContext);
