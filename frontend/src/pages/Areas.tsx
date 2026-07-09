import { useState, useEffect } from 'react';
import api from '../lib/api';
import type { AreaResponse } from '../lib/types';
import Card from '../components/Card';

export default function AreasPage({ projectId }: { projectId?: string }) {
  const pid = projectId || localStorage.getItem('currentProjectId') || '';
  const [areas, setAreas] = useState<AreaResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);

  useEffect(() => { if (pid) loadAreas(); else setLoading(false); }, [pid]);

  async function loadAreas() {
    try {
      const { data } = await api.get(`/projects/${pid}/areas`);
      setAreas(Array.isArray(data) ? data : data.items || []);
    } catch { /* empty */ }
    finally { setLoading(false); }
  }

  if (loading) return <div className="text-slate-400 text-center py-8">Carregando...</div>;
  if (!pid) return <div className="text-slate-400 text-center py-8">Selecione um projeto primeiro.</div>;

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-white">Áreas</h1>
          <p className="text-slate-400 text-sm mt-1">Agrupamento hierárquico de lojas</p>
        </div>
        <button onClick={() => setShowCreate(true)}
          className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 text-white rounded-lg text-sm font-medium transition">
          + Nova Área
        </button>
      </div>

      {areas.length === 0 ? (
        <div className="text-center py-12 text-slate-400">Nenhuma área cadastrada.</div>
      ) : (
        <div className="space-y-2">
          {areas.filter(a => !a.parentAreaId).map(area => (
            <AreaNode key={area.id} area={area} allAreas={areas} projectId={pid} onUpdated={loadAreas} depth={0} />
          ))}
        </div>
      )}

      {showCreate && <AreaModal projectId={pid} areas={areas} onClose={() => setShowCreate(false)} onCreated={() => { setShowCreate(false); loadAreas(); }} />}
    </div>
  );
}

function AreaNode({ area, allAreas, projectId, onUpdated, depth }: { area: AreaResponse; allAreas: AreaResponse[]; projectId: string; onUpdated: () => void; depth: number }) {
  const children = allAreas.filter(a => a.parentAreaId === area.id);

  return (
    <div>
      <Card className="flex items-center justify-between hover:border-slate-700" style={{ marginLeft: depth * 20 }}>
        <div className="flex items-center gap-2">
          <span className="text-sm font-medium text-white">{area.name}</span>
          {area.storeCount !== null && area.storeCount !== undefined && (
            <span className="text-xs text-slate-500">{area.storeCount} loja(s)</span>
          )}
          <span className="text-xs text-slate-600">nível {area.depth || 0}</span>
        </div>
        <button onClick={() => onUpdated()} className="text-xs text-slate-500 hover:text-indigo-400">editar</button>
      </Card>
      {children.map(child => (
        <AreaNode key={child.id} area={child} allAreas={allAreas} projectId={projectId} onUpdated={onUpdated} depth={depth + 1} />
      ))}
    </div>
  );
}

function AreaModal({ projectId, areas, onClose, onCreated }: { projectId: string; areas: AreaResponse[]; onClose: () => void; onCreated: () => void }) {
  const [form, setForm] = useState({ name: '', description: '', parentAreaId: '' });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.name.trim()) { setError('Nome é obrigatório'); return; }
    setSaving(true);
    try {
      await api.post(`/projects/${projectId}/areas`, { ...form, parentAreaId: form.parentAreaId || null });
      onCreated();
    } catch { setError('Erro ao criar área'); }
    finally { setSaving(false); }
  }

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50" onClick={onClose}>
      <Card className="w-full max-w-md mx-4" padding="lg" onClick={(e: React.MouseEvent) => e.stopPropagation()}>
        <h2 className="text-lg font-bold text-white mb-4">Nova Área</h2>
        {error && <p className="text-red-400 text-sm mb-3">{error}</p>}
        <form onSubmit={handleSubmit} className="space-y-3">
          <div>
            <label className="block text-sm text-slate-400 mb-1">Nome *</label>
            <input value={form.name} onChange={e => setForm({ ...form, name: e.target.value })}
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm" />
          </div>
          <div>
            <label className="block text-sm text-slate-400 mb-1">Descrição</label>
            <input value={form.description} onChange={e => setForm({ ...form, description: e.target.value })}
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm" />
          </div>
          <div>
            <label className="block text-sm text-slate-400 mb-1">Área pai (opcional)</label>
            <select value={form.parentAreaId} onChange={e => setForm({ ...form, parentAreaId: e.target.value })}
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm">
              <option value="">Nenhuma (raiz)</option>
              {areas.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
            </select>
          </div>
          <div className="flex justify-end gap-3 pt-2">
            <button type="button" onClick={onClose} className="px-4 py-2 text-sm text-slate-400 hover:text-white">Cancelar</button>
            <button type="submit" disabled={saving}
              className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 disabled:bg-indigo-800 text-white rounded-lg text-sm font-medium">
              {saving ? 'Criando...' : 'Criar'}
            </button>
          </div>
        </form>
      </Card>
    </div>
  );
}
