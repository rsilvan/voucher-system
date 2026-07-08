import { useState, useEffect } from 'react';
import api from '../lib/api';

export default function VouchersPage() {
  const [vouchers, setVouchers] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const pid = localStorage.getItem('currentProjectId');

  useEffect(() => { loadVouchers(); }, [pid]);

  async function loadVouchers() {
    if (!pid) { setLoading(false); return; }
    try {
      const { data } = await api.get(`/projects/${pid}/vouchers`);
      setVouchers(data.items || []);
    } catch { setError('Erro ao carregar vouchers'); }
    finally { setLoading(false); }
  }

  if (loading) return <div className="text-slate-400 text-center py-8">Carregando...</div>;
  if (!pid) return <div className="text-slate-400 text-center py-8">Selecione um projeto primeiro.</div>;

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-white">Vouchers</h1>
          <p className="text-slate-400 text-sm mt-1">Códigos de desconto e incentivos</p>
        </div>
        <button onClick={() => setShowCreate(true)}
          className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 text-white rounded-lg text-sm font-medium transition">
          + Novo Voucher
        </button>
      </div>
      {error && <div className="mb-4 p-3 bg-red-900/30 border border-red-800/50 rounded-lg text-red-400 text-sm">{error}</div>}
      {vouchers.length === 0 ? (
        <div className="text-center py-12 text-slate-400">Nenhum voucher encontrado.</div>
      ) : (
        <div className="space-y-3">
          {vouchers.map((v: any) => (
            <div key={v.id} className="bg-slate-900 rounded-xl border border-slate-800 p-4">
              <div className="flex items-center gap-3">
                <span className="text-white font-mono font-semibold">{v.code}</span>
                <span className={`px-2 py-0.5 rounded text-xs font-medium border ${
                  v.status === 'Active' ? 'bg-green-500/20 text-green-400 border-green-800/50' :
                  'bg-red-500/20 text-red-400 border-red-800/50'
                }`}>{v.status}</span>
                <span className="text-sm text-slate-400">
                  {v.discountType === 'Percentage' ? `${v.discountValue}%` : v.discountValue ? `R$ ${v.discountValue}` : ''}
                </span>
              </div>
              <div className="text-xs text-slate-500 mt-1">
                {v.redemptionCount}/{v.maxRedemptions ?? '∞'} usos
              </div>
            </div>
          ))}
        </div>
      )}

      {showCreate && (
        <CreateVoucherModal
          projectId={pid}
          onClose={() => setShowCreate(false)}
          onCreated={() => { setShowCreate(false); loadVouchers(); }}
        />
      )}
    </div>
  );
}

function CreateVoucherModal({ projectId, onClose, onCreated }: { projectId: string; onClose: () => void; onCreated: () => void }) {
  const [form, setForm] = useState({ code: '', discountType: 'Percentage', discountValue: 10, maxRedemptions: 1 });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.code.trim()) { setError('Código é obrigatório'); return; }
    setSaving(true); setError('');
    try {
      await api.post(`/projects/${projectId}/vouchers`, form);
      onCreated();
    } catch (err: any) {
      setError(err?.response?.data?.error || 'Erro ao criar voucher');
    }
    finally { setSaving(false); }
  }

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50" onClick={onClose}>
      <div className="bg-slate-900 rounded-xl border border-slate-800 p-6 w-full max-w-md mx-4" onClick={e => e.stopPropagation()}>
        <h2 className="text-lg font-bold text-white mb-4">Novo Voucher</h2>
        {error && <p className="text-red-400 text-sm mb-3">{error}</p>}
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm text-slate-400 mb-1">Código *</label>
            <input value={form.code} onChange={e => setForm({ ...form, code: e.target.value.toUpperCase() })}
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm font-mono"
              placeholder="PROMO-123" />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm text-slate-400 mb-1">Tipo de Desconto</label>
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
          <div>
            <label className="block text-sm text-slate-400 mb-1">Limite de Usos</label>
            <input type="number" value={form.maxRedemptions} onChange={e => setForm({ ...form, maxRedemptions: Number(e.target.value) })}
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm" />
          </div>
          <div className="flex justify-end gap-3 pt-2">
            <button type="button" onClick={onClose} className="px-4 py-2 text-sm text-slate-400 hover:text-white">Cancelar</button>
            <button type="submit" disabled={saving}
              className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 disabled:bg-indigo-800 text-white rounded-lg text-sm font-medium">
              {saving ? 'Criando...' : 'Criar'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
