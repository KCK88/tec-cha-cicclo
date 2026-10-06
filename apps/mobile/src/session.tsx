import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { readAccessToken, writeAccessToken } from "./storage";

type SessionValue = {
  token: string | null;
  ready: boolean;
  signIn: (token: string) => Promise<void>;
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

  async function signIn(next: string) {
    await writeAccessToken(next);
    setToken(next);
  }

  async function signOut() {
    await writeAccessToken(null);
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
