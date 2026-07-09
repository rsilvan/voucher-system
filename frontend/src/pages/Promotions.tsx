import { useState, useEffect } from 'react';
import api from '../lib/api';
import Card from '../components/Card';

export default function PromotionsPage() {
  const [promotions, setPromotions] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const pid = localStorage.getItem('currentProjectId');

  useEffect(() => { loadPromotions(); }, [pid]);

  async function loadPromotions() {
    if (!pid) { setLoading(false); return; }
    try {
      const { data } = await api.get(`/projects/${pid}/promotions`);
      setPromotions(data.items || []);
    } catch { setError('Erro ao carregar promoções'); }
    finally { setLoading(false); }
  }

  if (loading) return <div className="text-slate-400 text-center py-8">Carregando...</div>;
  if (!pid) return <div className="text-slate-400 text-center py-8">Selecione um projeto primeiro.</div>;

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-white">Promoções</h1>
          <p className="text-slate-400 text-sm mt-1">Descontos automáticos e regras promocionais</p>
        </div>
        <button onClick={() => setShowCreate(true)}
          className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 text-white rounded-lg text-sm font-medium transition">
          + Nova Promoção
        </button>
      </div>
      {error && <div className="mb-4 p-3 bg-red-900/30 border border-red-800/50 rounded-lg text-red-400 text-sm">{error}</div>}
      {promotions.length === 0 ? (
        <div className="text-center py-12 text-slate-400">Nenhuma promoção encontrada.</div>
      ) : (
        <div className="space-y-3">
          {promotions.map((p: any) => (
            <Card key={p.id} hover>
              <div className="flex items-center gap-3">
                <h3 className="text-white font-semibold">{p.name}</h3>
                <span className={`px-2 py-0.5 rounded text-xs font-medium border ${
                  p.status === 'Active' ? 'bg-green-500/20 text-green-400 border-green-800/50' :
                  p.status === 'Draft' ? 'bg-slate-500/20 text-slate-400 border-slate-800/50' :
                  'bg-amber-500/20 text-amber-400 border-amber-800/50'
                }`}>{p.status}</span>
              </div>
              <p className="text-sm text-slate-400 mt-1">{p.type} · Prioridade {p.priority}</p>
            </Card>
          ))}
        </div>
      )}

      {showCreate && (
        <CreatePromotionModal
          projectId={pid}
          onClose={() => setShowCreate(false)}
          onCreated={() => { setShowCreate(false); loadPromotions(); }}
        />
      )}
    </div>
  );
}

function CreatePromotionModal({ projectId, onClose, onCreated }: { projectId: string; onClose: () => void; onCreated: () => void }) {
  const [form, setForm] = useState({ name: '', description: '', type: 'Automatic', priority: 1, discountType: 'Percentage', discountValue: 10 });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.name.trim()) { setError('Nome é obrigatório'); return; }
    setSaving(true); setError('');
    try {
      await api.post(`/projects/${projectId}/promotions`, {
        name: form.name,
        description: form.description,
        type: form.type,
        priority: form.priority,
        discountDefinitions: [{
          discountType: form.discountType,
          valueType: form.discountType === 'Percentage' ? 'Percentage' : 'Fixed',
          value: form.discountValue,
          applyTo: 'Order'
        }]
      });
      onCreated();
    } catch { setError('Erro ao criar promoção'); }
    finally { setSaving(false); }
  }

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50" onClick={onClose}>
      <Card className="w-full max-w-md mx-4" padding="lg" onClick={(e: React.MouseEvent) => e.stopPropagation()}>
        <h2 className="text-lg font-bold text-white mb-4">Nova Promoção</h2>
        {error && <p className="text-red-400 text-sm mb-3">{error}</p>}
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm text-slate-400 mb-1">Nome *</label>
            <input value={form.name} onChange={e => setForm({ ...form, name: e.target.value })}
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm" />
          </div>
          <div>
            <label className="block text-sm text-slate-400 mb-1">Descrição</label>
            <textarea value={form.description} onChange={e => setForm({ ...form, description: e.target.value })}
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm" rows={2} />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm text-slate-400 mb-1">Tipo</label>
              <select value={form.type} onChange={e => setForm({ ...form, type: e.target.value })}
                className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm">
                <option value="Automatic">Automática</option>
                <option value="CodeTriggered">Por Código</option>
              </select>
            </div>
            <div>
              <label className="block text-sm text-slate-400 mb-1">Prioridade</label>
              <input type="number" value={form.priority} onChange={e => setForm({ ...form, priority: Number(e.target.value) })}
                className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm" />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm text-slate-400 mb-1">Tipo Desc.</label>
              <select value={form.discountType} onChange={e => setForm({ ...form, discountType: e.target.value })}
                className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm">
                <option value="Percentage">Percentual</option>
                <option value="FixedAmount">Valor Fixo</option>
              </select>
            </div>
            <div>
              <label className="block text-sm text-slate-400 mb-1">Valor</label>
              <input type="number" value={form.discountValue} onChange={e => setForm({ ...form, discountValue: Number(e.target.value) })}
                className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm" />
            </div>
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
