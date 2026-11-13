import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import type { UserDTO } from "@/api/Api";
import { api } from "@/api/client";
import { clearToken, getToken, setToken } from "@/api/token";

type Auth = {
  user: UserDTO | null;
  loading: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
};

const AuthContext = createContext<Auth | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserDTO | null>(null);
  const [loading, setLoading] = useState(true);

  // On page load: if we have a token, ask the server who we are.
  // An expired/invalid token gives 401, so we throw the token away.
  useEffect(() => {
    if (!getToken()) {
      setLoading(false);
      return;
    }
    api.auth
      .authGetMe()
      .then(res => setUser(res.data))
      .catch(() => clearToken())
      .finally(() => setLoading(false));
  }, []);

  async function login(username: string, password: string) {
    const { data } = await api.auth.authLogin({ username, password });
    if (!data.token || !data.user) throw new Error("Ugyldigt svar fra serveren");
    setToken(data.token);
    setUser(data.user);
  }

  function logout() {
    clearToken();
    setUser(null);
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
