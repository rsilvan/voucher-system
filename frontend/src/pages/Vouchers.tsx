import { useState, useEffect } from 'react';
import api from '../lib/api';

export default function VouchersPage() {
  const [vouchers, setVouchers] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
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
                  {v.discountType === 'Percentage' ? `${v.discountValue}%` : `R$ ${v.discountValue}`}
                </span>
              </div>
              <div className="text-xs text-slate-500 mt-1">
                {v.redemptionCount}/{v.maxRedemptions ?? '∞'} usos
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
