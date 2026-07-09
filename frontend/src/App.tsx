import { BrowserRouter, Routes, Route, Navigate, useOutletContext } from 'react-router-dom';
import { AuthProvider, useAuth } from './lib/auth';
import { Suspense, lazy, type ReactNode } from 'react';
import Card from './components/Card';

// Lazy-loaded page components
const Login = lazy(() => import('./pages/Login'));
const Register = lazy(() => import('./pages/Register'));
const Dashboard = lazy(() => import('./pages/Dashboard'));
const Members = lazy(() => import('./pages/Members'));
const Roles = lazy(() => import('./pages/Roles'));
const Projects = lazy(() => import('./pages/Projects'));
const Brand = lazy(() => import('./pages/Brand'));
const Stores = lazy(() => import('./pages/Stores'));
const Areas = lazy(() => import('./pages/Areas'));
const GeoLocations = lazy(() => import('./pages/GeoLocations'));
const AuditLogs = lazy(() => import('./pages/AuditLogs'));
const Campaigns = lazy(() => import('./pages/Campaigns'));
const Vouchers = lazy(() => import('./pages/Vouchers'));
const Promotions = lazy(() => import('./pages/Promotions'));

// Lazy-loaded heavy components
export const LazyMetadataEditor = lazy(() => import('./components/MetadataEditor'));
export const LazyTimeline = lazy(() => import('./components/Timeline'));

function PageFallback() {
  return (
    <div className="flex items-center justify-center h-64">
      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-500" />
    </div>
  );
}

function ProtectedRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <PageFallback />;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <Suspense fallback={<PageFallback />}>{children}</Suspense>;
}

function PublicRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <PageFallback />;
  }

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  return <Suspense fallback={<PageFallback />}>{children}</Suspense>;
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
        <Route path="campaigns" element={<Campaigns />} />
        <Route path="vouchers" element={<Vouchers />} />
        <Route path="promotions" element={<Promotions />} />
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
        <Card padding="lg">
          <p className="text-sm text-slate-400">Organização</p>
          <p className="text-xl font-bold text-white mt-1">{organization?.name}</p>
        </Card>
        <Card padding="lg">
          <p className="text-sm text-slate-400">Email</p>
          <p className="text-xl font-bold text-white mt-1">{user?.email}</p>
        </Card>
        <Card padding="lg">
          <p className="text-sm text-slate-400">Status</p>
          <p className="text-xl font-bold text-green-400 mt-1">Ativo</p>
        </Card>
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
