import { useState, useEffect } from 'react';
import api from '../lib/api';
import type { ProjectSummary, ProjectListResponse, CreateProjectRequest, UpdateProjectRequest } from '../lib/types';
import { getStatusBadge, getEnvBadge, ENVIRONMENTS, CURRENCIES, TIMEZONES, LOCALES, COUNTRIES } from '../lib/types';

export default function ProjectsPage() {
  const [projects, setProjects] = useState<ProjectSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);

  useEffect(() => { loadProjects(); }, []);

  async function loadProjects() {
    setLoading(true);
    try {
      const { data } = await api.get<ProjectListResponse>('/projects');
      setProjects(data.items);
    } catch (err: unknown) {
      setError('Erro ao carregar projetos');
    } finally {
      setLoading(false);
    }
  }

  async function handleAction(id: string, action: string) {
    try {
      await api.post(`/projects/${id}/${action}`);
      await loadProjects();
    } catch (err: unknown) {
      setError(`Erro ao executar ${action}`);
    }
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-white">Projetos</h1>
          <p className="text-slate-400 text-sm mt-1">Gerencie os projetos da sua organização</p>
        </div>
        <button
          onClick={() => setShowCreate(true)}
          className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 text-white rounded-lg text-sm font-medium transition"
        >
          + Novo Projeto
        </button>
      </div>

      {error && (
        <div className="mb-4 p-3 bg-red-900/30 border border-red-800/50 rounded-lg text-red-400 text-sm">
          {error}
        </div>
      )}

      {/* Overview cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
        <div className="bg-slate-900 rounded-xl border border-slate-800 p-4">
          <p className="text-sm text-slate-400">Total</p>
          <p className="text-2xl font-bold text-white mt-1">{projects.length}</p>
        </div>
        <div className="bg-slate-900 rounded-xl border border-slate-800 p-4">
          <p className="text-sm text-slate-400">Ativos</p>
          <p className="text-2xl font-bold text-green-400 mt-1">
            {projects.filter(p => p.status === 'Active').length}
          </p>
        </div>
        <div className="bg-slate-900 rounded-xl border border-slate-800 p-4">
          <p className="text-sm text-slate-400">Produção</p>
          <p className="text-2xl font-bold text-amber-400 mt-1">
            {projects.filter(p => p.environment === 'Production').length}
          </p>
        </div>
        <div className="bg-slate-900 rounded-xl border border-slate-800 p-4">
          <p className="text-sm text-slate-400">Arquivados</p>
          <p className="text-2xl font-bold text-slate-400 mt-1">
            {projects.filter(p => p.status === 'Archived').length}
          </p>
        </div>
      </div>

      {/* Project list */}
      {loading ? (
        <div className="text-center py-12 text-slate-400">Carregando...</div>
      ) : projects.length === 0 ? (
        <div className="text-center py-12 text-slate-400">
          Nenhum projeto encontrado. Crie seu primeiro projeto.
        </div>
      ) : (
        <div className="space-y-3">
          {projects.map(project => (
            <ProjectCard
              key={project.id}
              project={project}
              onAction={handleAction}
            />
          ))}
        </div>
      )}

      {/* Create modal */}
      {showCreate && (
        <ProjectModal
          onClose={() => setShowCreate(false)}
          onCreated={() => { setShowCreate(false); loadProjects(); }}
        />
      )}

      {/* Edit modal */}
      {editingId && (
        <EditModal
          projectId={editingId}
          onClose={() => setEditingId(null)}
          onUpdated={() => { setEditingId(null); loadProjects(); }}
        />
      )}
    </div>
  );
}

function ProjectCard({ project, onAction }: { project: ProjectSummary; onAction: (id: string, action: string) => void }) {
  const statusBadge = getStatusBadge(project.status);
  const envBadge = getEnvBadge(project.environment);
  const [menuOpen, setMenuOpen] = useState(false);

  return (
    <div className="bg-slate-900 rounded-xl border border-slate-800 p-5 hover:border-slate-700 transition">
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="flex items-center gap-3">
            <h3 className="text-lg font-semibold text-white">
              {project.isPrimary && <span className="text-amber-400 mr-1">★</span>}
              {project.name}
            </h3>
            <span className={`px-2 py-0.5 rounded-full text-xs font-medium border ${envBadge.color}`}>
              {envBadge.label}
            </span>
            <span className={`px-2 py-0.5 rounded-full text-xs font-medium border ${statusBadge.color}`}>
              {statusBadge.label}
            </span>
          </div>
          <div className="flex items-center gap-4 mt-2 text-sm text-slate-400">
            <span>Moeda: {project.currency}</span>
            <span>Criado: {new Date(project.createdAt).toLocaleDateString('pt-BR')}</span>
          </div>
        </div>

        <div className="relative">
          <button
            onClick={() => setMenuOpen(!menuOpen)}
            className="p-2 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800 transition"
          >
            ⠇
          </button>
          {menuOpen && (
            <div className="absolute right-0 mt-1 w-44 bg-slate-800 border border-slate-700 rounded-lg shadow-xl z-10 py-1">
              {project.status === 'Active' && (
                <>
                  <button onClick={() => { setMenuOpen(false); onAction(project.id, 'disable'); }} className="w-full text-left px-4 py-2 text-sm text-slate-300 hover:bg-slate-700">Desativar</button>
                  <button onClick={() => { setMenuOpen(false); onAction(project.id, 'archive'); }} className="w-full text-left px-4 py-2 text-sm text-slate-300 hover:bg-slate-700">Arquivar</button>
                </>
              )}
              {project.status === 'Disabled' && (
                <>
                  <button onClick={() => { setMenuOpen(false); onAction(project.id, 'enable'); }} className="w-full text-left px-4 py-2 text-sm text-slate-300 hover:bg-slate-700">Reativar</button>
                  <button onClick={() => { setMenuOpen(false); onAction(project.id, 'archive'); }} className="w-full text-left px-4 py-2 text-sm text-slate-300 hover:bg-slate-700">Arquivar</button>
                </>
              )}
              {project.status === 'Archived' && (
                <button onClick={() => { setMenuOpen(false); onAction(project.id, 'restore'); }} className="w-full text-left px-4 py-2 text-sm text-slate-300 hover:bg-slate-700">Restaurar</button>
              )}
              {project.isPrimary === false && project.status === 'Active' && (
                <button onClick={() => { setMenuOpen(false); onAction(project.id, 'make-primary'); }} className="w-full text-left px-4 py-2 text-sm text-slate-300 hover:bg-slate-700">Definir como Principal</button>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function ProjectModal({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const [form, setForm] = useState<CreateProjectRequest>({
    name: '',
    description: '',
    environment: 'Sandbox',
    currency: 'BRL',
    timeZone: 'America/Sao_Paulo',
    locale: 'pt-BR',
    country: 'BR',
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.name.trim()) { setError('Nome é obrigatório'); return; }
    setSaving(true);
    setError('');
    try {
      await api.post('/projects', form);
      onCreated();
    } catch (err: unknown) {
      setError('Erro ao criar projeto');
    } finally {
      setSaving(false);
    }
  }

  const isProductionForm = form.environment === 'Production';
  const [confirmText, setConfirmText] = useState('');

  // This is used before submit for Production confirmation
  function handlePreSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.name.trim()) { setError('Nome é obrigatório'); return; }
    if (form.environment === 'Production') {
      if (confirmText !== form.name) {
        setError('Digite o nome do projeto para confirmar a criação em Produção.');
        return;
      }
    }
    handleSubmit(e);
  }

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50" onClick={onClose}>
      <div className="bg-slate-900 rounded-xl border border-slate-800 p-6 w-full max-w-lg mx-4" onClick={e => e.stopPropagation()}>
        <h2 className="text-lg font-bold text-white mb-4">Novo Projeto</h2>
        {error && <p className="text-red-400 text-sm mb-3">{error}</p>}
        <form onSubmit={handlePreSubmit} className="space-y-4">
          <div>
            <label className="block text-sm text-slate-400 mb-1">Nome *</label>
            <input value={form.name} onChange={e => setForm({ ...form, name: e.target.value })}
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500" />
          </div>
          <div>
            <label className="block text-sm text-slate-400 mb-1">Descrição</label>
            <textarea value={form.description} onChange={e => setForm({ ...form, description: e.target.value })}
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500" rows={2} />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm text-slate-400 mb-1">Ambiente</label>
              <select value={form.environment} onChange={e => setForm({ ...form, environment: e.target.value })}
                className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500">
                {ENVIRONMENTS.map(e => <option key={e} value={e}>{e}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-sm text-slate-400 mb-1">Moeda</label>
              <select value={form.currency} onChange={e => setForm({ ...form, currency: e.target.value })}
                className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500">
                {CURRENCIES.map(c => <option key={c} value={c}>{c}</option>)}
              </select>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm text-slate-400 mb-1">Fuso Horário</label>
              <select value={form.timeZone} onChange={e => setForm({ ...form, timeZone: e.target.value })}
                className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500">
                {TIMEZONES.map(t => <option key={t} value={t}>{t}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-sm text-slate-400 mb-1">Locale</label>
              <select value={form.locale} onChange={e => setForm({ ...form, locale: e.target.value })}
                className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500">
                {LOCALES.map(l => <option key={l} value={l}>{l}</option>)}
              </select>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm text-slate-400 mb-1">País</label>
              <select value={form.country} onChange={e => setForm({ ...form, country: e.target.value })}
                className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500">
                {COUNTRIES.map(c => <option key={c} value={c}>{c}</option>)}
              </select>
            </div>
          </div>
          {isProductionForm && (
            <div className="bg-amber-600/10 border border-amber-800/30 rounded-lg p-3">
              <p className="text-xs text-amber-400 font-medium mb-2">
                ⚠ Você está criando um projeto em <strong>Produção</strong>. 
                Digite o nome do projeto para confirmar:
              </p>
              <input
                value={confirmText}
                onChange={e => setConfirmText(e.target.value)}
                placeholder="Digite o nome do projeto..."
                className="w-full bg-slate-800 border border-amber-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-amber-500"
              />
            </div>
          )}
          <div className="flex justify-end gap-3 pt-2">
            <button type="button" onClick={onClose} className="px-4 py-2 text-sm text-slate-400 hover:text-white transition">Cancelar</button>
            <button type="submit" disabled={saving}
              className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 disabled:bg-indigo-800 text-white rounded-lg text-sm font-medium transition">
              {saving ? 'Criando...' : 'Criar Projeto'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function EditModal({ projectId, onClose, onUpdated }: { projectId: string; onClose: () => void; onUpdated: () => void }) {
  const [form, setForm] = useState<UpdateProjectRequest>({});
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setError('');
    try {
      await api.patch(`/projects/${projectId}`, form);
      onUpdated();
    } catch (err: unknown) {
      setError('Erro ao atualizar projeto');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50" onClick={onClose}>
      <div className="bg-slate-900 rounded-xl border border-slate-800 p-6 w-full max-w-lg mx-4" onClick={e => e.stopPropagation()}>
        <h2 className="text-lg font-bold text-white mb-4">Editar Projeto</h2>
        {error && <p className="text-red-400 text-sm mb-3">{error}</p>}
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm text-slate-400 mb-1">Nome</label>
            <input value={form.name ?? ''} onChange={e => setForm({ ...form, name: e.target.value })}
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500" />
          </div>
          <div>
            <label className="block text-sm text-slate-400 mb-1">Descrição</label>
            <textarea value={form.description ?? ''} onChange={e => setForm({ ...form, description: e.target.value })}
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500" rows={2} />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm text-slate-400 mb-1">Moeda</label>
              <select value={form.currency ?? ''} onChange={e => setForm({ ...form, currency: e.target.value })}
                className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500">
                <option value="">Manter atual</option>
                {CURRENCIES.map(c => <option key={c} value={c}>{c}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-sm text-slate-400 mb-1">Fuso Horário</label>
              <select value={form.timeZone ?? ''} onChange={e => setForm({ ...form, timeZone: e.target.value })}
                className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500">
                <option value="">Manter atual</option>
                {TIMEZONES.map(t => <option key={t} value={t}>{t}</option>)}
              </select>
            </div>
          </div>
          <div className="flex justify-end gap-3 pt-2">
            <button type="button" onClick={onClose} className="px-4 py-2 text-sm text-slate-400 hover:text-white transition">Cancelar</button>
            <button type="submit" disabled={saving}
              className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 disabled:bg-indigo-800 text-white rounded-lg text-sm font-medium transition">
              {saving ? 'Salvando...' : 'Salvar'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
