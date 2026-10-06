import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { api, setAccessRenewal, type AuthResponse } from "./api";
import { readAccessToken, readRefreshToken, writeAccessToken, writeRefreshToken } from "./storage";

type SessionValue = {
  token: string | null;
  ready: boolean;
  signIn: (accessToken: string, refreshToken: string) => Promise<void>;
  signOut: () => Promise<void>;
};

const SessionContext = createContext<SessionValue | null>(null);

export function SessionProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    readAccessToken()
      .then(setToken)
      .finally(() => setReady(true));
  }, []);

  useEffect(() => {
    setAccessRenewal(async () => {
      const refreshToken = await readRefreshToken();
      if (!refreshToken) {
        return null;
      }
      try {
        const auth = await api<AuthResponse>("/auth/refresh", {
          method: "POST",
          body: { refreshToken },
          skipRenewal: true,
        });
        await writeAccessToken(auth.accessToken);
        await writeRefreshToken(auth.refreshToken);
        setToken(auth.accessToken);
        return auth.accessToken;
      } catch {
        return null;
      }
    });
  }, []);

  async function signIn(accessToken: string, refreshToken: string) {
    await writeAccessToken(accessToken);
    await writeRefreshToken(refreshToken);
    setToken(accessToken);
  }

  async function signOut() {
    await writeAccessToken(null);
    await writeRefreshToken(null);
    setToken(null);
  }

  return (
    <SessionContext.Provider value={{ token, ready, signIn, signOut }}>
      {children}
    </SessionContext.Provider>
  );
}

export function useSession() {
  const value = useContext(SessionContext);
  if (!value) {
    throw new Error("useSession must be used inside SessionProvider");
  }
  return value;
}
