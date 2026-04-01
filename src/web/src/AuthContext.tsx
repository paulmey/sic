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
    api.getMe()
      .then(setUser)
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  useEffect(refresh, [refresh]);

  return (
    <AuthContext.Provider value={{ user, loading, error, refresh }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => useContext(AuthContext);
