"use client";

import { createContext, useCallback, useContext, useEffect, useState } from "react";
import { login as loginRequest, type StaffSummary } from "@/lib/api/auth";
import {
  clearStoredStaff,
  clearToken,
  getStoredStaff,
  getToken,
  setStoredStaff,
  setToken,
} from "@/lib/api/tokenStore";

interface AuthContextValue {
  staff: StaffSummary | null;
  isLoading: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [staff, setStaff] = useState<StaffSummary | null>(null);
  // Starts true so the protected-route guard doesn't redirect to /login
  // before localStorage has been read on first client render.
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const token = getToken();
    const storedStaff = getStoredStaff<StaffSummary>();
    if (token && storedStaff) {
      setStaff(storedStaff);
    }
    setIsLoading(false);
  }, []);

  const login = useCallback(async (username: string, password: string) => {
    const response = await loginRequest(username, password);
    setToken(response.token);
    setStoredStaff(response.staff);
    setStaff(response.staff);
  }, []);

  const logout = useCallback(() => {
    clearToken();
    clearStoredStaff();
    setStaff(null);
  }, []);

  return (
    <AuthContext.Provider value={{ staff, isLoading, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
