import { BrowserRouter, Routes, Route, Navigate, useOutletContext } from 'react-router-dom';
import { AuthProvider, useAuth } from './lib/auth';
import Login from './pages/Login';
import Register from './pages/Register';
import Dashboard from './pages/Dashboard';
import Members from './pages/Members';
import Roles from './pages/Roles';
import Projects from './pages/Projects';
import Brand from './pages/Brand';
import Stores from './pages/Stores';
import Areas from './pages/Areas';
import GeoLocations from './pages/GeoLocations';
import AuditLogs from './pages/AuditLogs';
import type { ReactNode } from 'react';

function ProtectedRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="min-h-screen bg-slate-950 flex items-center justify-center">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-500" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}

function PublicRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="min-h-screen bg-slate-950 flex items-center justify-center">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-500" />
      </div>
    );
  }

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  return <>{children}</>;
}

function BrandWrapper() {
  const { projectId } = useOutletContext<{ projectId: string | null }>();
  return <Brand projectId={projectId || undefined} />;
}

function StoresWrapper() {
  const { projectId } = useOutletContext<{ projectId: string | null }>();
  return <Stores projectId={projectId || undefined} />;
}

function AreasWrapper() {
  const { projectId } = useOutletContext<{ projectId: string | null }>();
  return <Areas projectId={projectId || undefined} />;
}

function GeoLocationsWrapper() {
  const { projectId } = useOutletContext<{ projectId: string | null }>();
  return <GeoLocations projectId={projectId || undefined} />;
}

function AppRoutes() {
  return (
    <Routes>
      <Route
        path="/login"
        element={
          <PublicRoute>
            <Login />
          </PublicRoute>
        }
      />
      <Route
        path="/register"
        element={
          <PublicRoute>
            <Register />
          </PublicRoute>
        }
      />
      <Route
        path="/dashboard"
        element={
          <ProtectedRoute>
            <Dashboard />
          </ProtectedRoute>
        }
      >
        <Route index element={<DashboardHome />} />
        <Route path="members" element={<Members />} />
        <Route path="roles" element={<Roles />} />
        <Route path="projects" element={<Projects />} />
        <Route path="brand" element={<BrandWrapper />} />
        <Route path="stores" element={<StoresWrapper />} />
        <Route path="areas" element={<AreasWrapper />} />
        <Route path="locations" element={<GeoLocationsWrapper />} />
        <Route path="audit" element={<AuditLogs />} />
      </Route>
      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  );
}

function DashboardHome() {
  const { user, organization } = useAuth();
  return (
    <div>
      <h1 className="text-2xl font-bold text-white mb-2">Bem-vindo, {user?.name}!</h1>
      <p className="text-slate-400 mb-6">
        Você está na organização <strong className="text-slate-200">{organization?.name}</strong>.
      </p>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-slate-900 rounded-xl border border-slate-800 p-6">
          <p className="text-sm text-slate-400">Organização</p>
          <p className="text-xl font-bold text-white mt-1">{organization?.name}</p>
        </div>
        <div className="bg-slate-900 rounded-xl border border-slate-800 p-6">
          <p className="text-sm text-slate-400">Email</p>
          <p className="text-xl font-bold text-white mt-1">{user?.email}</p>
        </div>
        <div className="bg-slate-900 rounded-xl border border-slate-800 p-6">
          <p className="text-sm text-slate-400">Status</p>
          <p className="text-xl font-bold text-green-400 mt-1">Ativo</p>
        </div>
      </div>
    </div>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </BrowserRouter>
  );
}
