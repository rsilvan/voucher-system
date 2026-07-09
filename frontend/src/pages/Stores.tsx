import { useState, useEffect } from 'react';
import api from '../lib/api';
import type { StoreSummaryResponse } from '../lib/types';
import Card from '../components/Card';

export default function StoresPage({ projectId }: { projectId?: string }) {
  const pid = projectId || localStorage.getItem('currentProjectId') || '';
  const [stores, setStores] = useState<StoreSummaryResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);

  useEffect(() => { if (pid) loadStores(); else setLoading(false); }, [pid]);

  async function loadStores() {
    try {
      const { data } = await api.get(`/projects/${pid}/stores`);
      setStores(data.items || data || []);
    } catch { /* empty */ }
    finally { setLoading(false); }
  }

  if (loading) return <div className="text-slate-400 text-center py-8">Carregando...</div>;
  if (!pid) return <div className="text-slate-400 text-center py-8">Selecione um projeto primeiro.</div>;

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-white">Lojas</h1>
          <p className="text-slate-400 text-sm mt-1">Unidades físicas e canais digitais</p>
        </div>
        <button onClick={() => setShowCreate(true)}
          className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 text-white rounded-lg text-sm font-medium transition">
          + Nova Loja
        </button>
      </div>

      {stores.length === 0 ? (
        <div className="text-center py-12 text-slate-400">Nenhuma loja cadastrada neste projeto.</div>
      ) : (
        <div className="grid gap-3">
          {stores.map(store => (
            <StoreCard key={store.id} store={store} projectId={pid} onUpdated={loadStores} />
          ))}
        </div>
      )}

      {showCreate && <StoreModal projectId={pid} onClose={() => setShowCreate(false)} onCreated={() => { setShowCreate(false); loadStores(); }} />}
    </div>
  );
}

function StoreCard({ store, projectId, onUpdated }: { store: StoreSummaryResponse; projectId: string; onUpdated: () => void }) {
  const [menuOpen, setMenuOpen] = useState(false);

  async function handleAction(action: string) {
    try {
      await api.post(`/projects/${projectId}/stores/${store.id}/${action}`);
      onUpdated();
    } catch { /* ignore */ }
  }

  const statusColor = store.status === 'Active' ? 'text-green-400' : store.status === 'Inactive' ? 'text-red-400' : 'text-slate-400';

  return (
    <Card hover>
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-2">
            <span className="text-sm font-semibold text-white">{store.name}</span>
            <span className={`text-xs ${statusColor}`}>● {store.status === 'Active' ? 'Ativo' : store.status === 'Inactive' ? 'Inativo' : 'Arquivado'}</span>
            <span className="text-xs text-slate-500">#{store.code}</span>
          </div>
          {store.city && <p className="text-xs text-slate-500 mt-1">{store.city}{store.country ? ` (${store.country})` : ''}</p>}
        </div>
        <div className="relative">
          <button onClick={() => setMenuOpen(!menuOpen)} className="p-1 text-slate-400 hover:text-white rounded">⠇</button>
          {menuOpen && (
            <div className="absolute right-0 mt-1 w-36 bg-slate-800 border border-slate-700 rounded-lg shadow-xl z-10 py-1">
              {store.status === 'Active' && <button onClick={() => { setMenuOpen(false); handleAction('deactivate'); }} className="w-full text-left px-3 py-1.5 text-sm text-slate-300 hover:bg-slate-700">Desativar</button>}
              {store.status === 'Inactive' && <button onClick={() => { setMenuOpen(false); handleAction('activate'); }} className="w-full text-left px-3 py-1.5 text-sm text-slate-300 hover:bg-slate-700">Ativar</button>}
              {store.status !== 'Archived' && <button onClick={() => { setMenuOpen(false); handleAction('archive'); }} className="w-full text-left px-3 py-1.5 text-sm text-slate-300 hover:bg-slate-700">Arquivar</button>}
            </div>
          )}
        </div>
      </div>
    </Card>
  );
}

function StoreModal({ projectId, onClose, onCreated }: { projectId: string; onClose: () => void; onCreated: () => void }) {
  const [form, setForm] = useState({ name: '', code: '', description: '', storeType: 'Physical', addressLine1: '', city: '', state: '', postalCode: '', country: 'BR', contactEmail: '', contactPhone: '' });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.name.trim()) { setError('Nome é obrigatório'); return; }
    setSaving(true);
    try {
      await api.post(`/projects/${projectId}/stores`, form);
      onCreated();
    } catch { setError('Erro ao criar loja'); }
    finally { setSaving(false); }
  }

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50" onClick={onClose}>
      <Card className="w-full max-w-lg mx-4" padding="lg" onClick={(e: React.MouseEvent) => e.stopPropagation()}>
        <h2 className="text-lg font-bold text-white mb-4">Nova Loja</h2>
        {error && <p className="text-red-400 text-sm mb-3">{error}</p>}
        <form onSubmit={handleSubmit} className="space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-sm text-slate-400 mb-1">Nome *</label>
              <input value={form.name} onChange={e => setForm({ ...form, name: e.target.value })}
                className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm" />
            </div>
            <div>
              <label className="block text-sm text-slate-400 mb-1">Código</label>
              <input value={form.code} onChange={e => setForm({ ...form, code: e.target.value })}
                className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm" />
            </div>
          </div>
          <div>
            <label className="block text-sm text-slate-400 mb-1">Tipo</label>
            <select value={form.storeType} onChange={e => setForm({ ...form, storeType: e.target.value })}
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm">
              <option value="Physical">Física</option>
              <option value="Digital">Digital</option>
            </select>
          </div>
          <div>
            <label className="block text-sm text-slate-400 mb-1">Descrição</label>
            <input value={form.description} onChange={e => setForm({ ...form, description: e.target.value })}
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm" />
          </div>
          <div>
            <label className="block text-sm text-slate-400 mb-1">Endereço</label>
            <input value={form.addressLine1} onChange={e => setForm({ ...form, addressLine1: e.target.value })}
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm" placeholder="Logradouro" />
          </div>
          <div className="grid grid-cols-3 gap-3">
            <input value={form.city} onChange={e => setForm({ ...form, city: e.target.value })}
              className="bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm" placeholder="Cidade" />
            <input value={form.state} onChange={e => setForm({ ...form, state: e.target.value })}
              className="bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm" placeholder="Estado" />
            <input value={form.country} onChange={e => setForm({ ...form, country: e.target.value })}
              className="bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm" placeholder="País" maxLength={2} />
          </div>
          <div className="flex justify-end gap-3 pt-2">
            <button type="button" onClick={onClose} className="px-4 py-2 text-sm text-slate-400 hover:text-white">Cancelar</button>
            <button type="submit" disabled={saving}
              className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 disabled:bg-indigo-800 text-white rounded-lg text-sm font-medium">
              {saving ? 'Criando...' : 'Criar Loja'}
            </button>
          </div>
        </form>
      </Card>
    </div>
  );
}
