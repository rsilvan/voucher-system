import {
  createContext,
  useContext,
  useState,
  useCallback,
  useEffect,
  type ReactNode,
} from 'react';
import api from './api';
import type { User, Organization, AuthResponse } from './types';

interface AuthContextType {
  user: User | null;
  organization: Organization | null;
  permissions: string[];
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (
    organizationName: string,
    adminName: string,
    adminEmail: string,
    adminPassword: string,
    confirmPassword: string,
    acceptedTerms: boolean,
  ) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(() => {
    const stored = localStorage.getItem('user');
    return stored ? JSON.parse(stored) : null;
  });
  const [organization, setOrganization] = useState<Organization | null>(() => {
    const stored = localStorage.getItem('organization');
    return stored ? JSON.parse(stored) : null;
  });
  const [permissions, setPermissions] = useState<string[]>(() => {
    const stored = localStorage.getItem('permissions');
    return stored ? JSON.parse(stored) : [];
  });
  const [isLoading, setIsLoading] = useState(true);

  const isAuthenticated = !!user && !!localStorage.getItem('accessToken');

  useEffect(() => {
    // Check if token exists and is valid
    const token = localStorage.getItem('accessToken');
    if (token && user) {
      setIsLoading(false);
    } else {
      setIsLoading(false);
    }
  }, [user]);

  const login = useCallback(async (email: string, password: string) => {
    const { data } = await api.post<AuthResponse>('/auth/login', {
      email,
      password,
    });

    localStorage.setItem('accessToken', data.accessToken);
    localStorage.setItem('refreshToken', data.refreshToken);
    localStorage.setItem('user', JSON.stringify(data.user));
    localStorage.setItem('organization', JSON.stringify(data.organization));
    localStorage.setItem('permissions', JSON.stringify(data.permissions));

    setUser(data.user);
    setOrganization(data.organization);
    setPermissions(data.permissions);
  }, []);

  const register = useCallback(
    async (
      organizationName: string,
      adminName: string,
      adminEmail: string,
      adminPassword: string,
      confirmPassword: string,
      acceptedTerms: boolean,
    ) => {
      await api.post('/self-service/organizations', {
        organizationName,
        responsibleName: adminName,
        email: adminEmail,
        password: adminPassword,
        confirmPassword,
        acceptedTerms,
        acceptedPrivacyPolicy: acceptedTerms,
        country: 'BR',
      });
    },
    [],
  );

  const logout = useCallback(() => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    localStorage.removeItem('organization');
    localStorage.removeItem('permissions');
    setUser(null);
    setOrganization(null);
    setPermissions([]);
  }, []);

  return (
    <AuthContext.Provider
      value={{
        user,
        organization,
        permissions,
        isAuthenticated,
        isLoading,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
