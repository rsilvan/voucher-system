import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../lib/auth';
import { useState, useEffect } from 'react';
import api from '../lib/api';
import type { ProjectSummary, ProjectListResponse } from '../lib/types';
import { getEnvBadge } from '../lib/types';

const navItems = [
  { to: '/dashboard', label: 'Dashboard', icon: '📊' },
  { to: '/dashboard/members', label: 'Membros', icon: '👥' },
  { to: '/dashboard/roles', label: 'Roles', icon: '🔐' },
  { to: '/dashboard/projects', label: 'Projetos', icon: '📁' },
  { to: '/dashboard/brand', label: 'Marca', icon: '🎨' },
  { to: '/dashboard/stores', label: 'Lojas', icon: '🏪' },
  { to: '/dashboard/areas', label: 'Áreas', icon: '🗂️' },
  { to: '/dashboard/locations', label: 'Localizações', icon: '📍' },
  { to: '/dashboard/audit', label: 'Auditoria', icon: '📋' },
];

export default function Dashboard() {
  const { user, organization, logout } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [projects, setProjects] = useState<ProjectSummary[]>([]);
  const [currentProjectId, setCurrentProjectId] = useState<string | null>(
    localStorage.getItem('currentProjectId')
  );
  const [switcherOpen, setSwitcherOpen] = useState(false);

  useEffect(() => {
    api.get<ProjectListResponse>('/projects').then(({ data }) => {
      setProjects(data.items);
      if (!currentProjectId && data.items.length > 0) {
        const primary = data.items.find(p => p.isPrimary) || data.items[0];
        setCurrentProjectId(primary.id);
        localStorage.setItem('currentProjectId', primary.id);
      }
    }).catch(() => {});
  }, []);

  function handleSwitchProject(id: string) {
    setCurrentProjectId(id);
    localStorage.setItem('currentProjectId', id);
    setSwitcherOpen(false);
  }

  const currentProject = projects.find(p => p.id === currentProjectId);
  const envBadge = currentProject ? getEnvBadge(currentProject.environment) : null;

  function handleLogout() {
    logout();
    navigate('/login');
  }

  return (
    <div className="min-h-screen bg-slate-950 flex">
      {/* Sidebar */}
      <aside className="w-64 bg-slate-900 border-r border-slate-800 flex flex-col shrink-0">
        <div className="p-6 border-b border-slate-800">
          <h2 className="text-xl font-bold text-white">Voucher System</h2>
          {organization && (
            <p className="text-sm text-slate-400 mt-1">{organization.name}</p>
          )}

          {/* Project Switcher */}
          <div className="relative mt-3">
            <button
              onClick={() => setSwitcherOpen(!switcherOpen)}
              className="w-full flex items-center gap-2 px-3 py-2 bg-slate-800 rounded-lg text-sm text-slate-300 hover:bg-slate-700 transition"
            >
              <span className="flex-1 truncate text-left">
                {currentProject ? currentProject.name : 'Selecionar projeto'}
              </span>
              {envBadge && (
                <span className={`px-1.5 py-0.5 rounded text-[10px] font-medium border ${envBadge.color}`}>
                  {envBadge.label}
                </span>
              )}
              <span className="text-slate-500">▼</span>
            </button>

            {switcherOpen && (
              <div className="absolute top-full left-0 right-0 mt-1 bg-slate-800 border border-slate-700 rounded-lg shadow-xl z-20 max-h-48 overflow-y-auto">
                {projects.filter(p => p.status === 'Active').map(p => {
                  const eBadge = getEnvBadge(p.environment);
                  return (
                    <button
                      key={p.id}
                      onClick={() => handleSwitchProject(p.id)}
                      className={`w-full flex items-center gap-2 px-3 py-2 text-sm hover:bg-slate-700 transition ${
                        p.id === currentProjectId ? 'text-indigo-300 bg-indigo-900/20' : 'text-slate-300'
                      }`}
                    >
                      <span className="flex-1 truncate text-left">{p.name}</span>
                      <span className={`px-1.5 py-0.5 rounded text-[10px] font-medium border ${eBadge.color}`}>
                        {eBadge.label}
                      </span>
                    </button>
                  );
                })}
                {projects.filter(p => p.status !== 'Active').map(p => (
                  <button
                    key={p.id}
                    disabled
                    className="w-full flex items-center gap-2 px-3 py-2 text-sm text-slate-600 cursor-not-allowed"
                  >
                    <span className="flex-1 truncate text-left">{p.name}</span>
                    <span className="text-[10px] text-slate-600">inativo</span>
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>

        <nav className="flex-1 p-4 space-y-1">
          {navItems.map((item) => {
            const isActive =
              item.to === '/dashboard'
                ? location.pathname === '/dashboard'
                : location.pathname.startsWith(item.to);
            return (
              <Link
                key={item.to}
                to={item.to}
                className={`flex items-center gap-3 px-4 py-2.5 rounded-lg text-sm font-medium transition ${
                  isActive
                    ? 'bg-indigo-600/20 text-indigo-300 border border-indigo-800/50'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800'
                }`}
              >
                <span>{item.icon}</span>
                {item.label}
              </Link>
            );
          })}
        </nav>

        <div className="p-4 border-t border-slate-800">
          {user && (
            <div className="mb-3 px-1">
              <p className="text-sm font-medium text-slate-200 truncate">
                {user.name}
              </p>
              <p className="text-xs text-slate-500 truncate">{user.email}</p>
            </div>
          )}
          <button
            onClick={handleLogout}
            className="w-full px-4 py-2 text-sm text-slate-400 hover:text-white hover:bg-slate-800 rounded-lg transition text-left"
          >
            Sair
          </button>
        </div>
      </aside>

      {/* Main content with Production banner */}
      <main className="flex-1 overflow-auto">
        {currentProject?.environment === 'Production' && (
          <div className="bg-amber-600/10 border-b border-amber-800/30 px-8 py-2">
            <p className="text-xs text-amber-400 font-medium text-center">
              ⚠ Ambiente de Produção — alterações afetam dados reais
            </p>
          </div>
        )}
        <div className="p-8">
          <Outlet context={{ projectId: currentProjectId }} />
        </div>
      </main>
    </div>
  );
}
