import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import type { UserDTO } from "@/api/Api";
import { api } from "@/api/client";

type Auth = {
  user: UserDTO | null;
  loading: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
};

const AuthContext = createContext<Auth | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserDTO | null>(null);
  const [loading, setLoading] = useState(true);

  // On page load we can't look for a token (the cookie is HttpOnly),
  // so we always ask the server. 200 = logged in, 401 = not logged in.
  useEffect(() => {
    api.auth
        .authGetMe()
        .then(res => setUser(res.data))
        .catch(() => setUser(null))
        .finally(() => setLoading(false));
  }, []);

  async function login(username: string, password: string) {
    // Server sets the cookie and returns just the user
    const { data } = await api.auth.authLogin({ username, password });
    setUser(data);
  }

  async function logout() {
    // Ask the server to delete HttpOnly cookie
    try {
      await api.auth.authLogout();
    } finally {
      setUser(null);
    }
  }

  return (
      <AuthContext.Provider value={{ user, loading, login, logout }}>
        {children}
      </AuthContext.Provider>
  );
}

export function useAuth(): Auth {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>");
  return ctx;
}