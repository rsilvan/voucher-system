import { useState, useEffect } from 'react';
import api from '../lib/api';

export default function CampaignsPage() {
  const [campaigns, setCampaigns] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const pid = localStorage.getItem('currentProjectId');

  useEffect(() => { loadCampaigns(); }, [pid]);

  async function loadCampaigns() {
    if (!pid) { setLoading(false); return; }
    try {
      const { data } = await api.get(`/projects/${pid}/campaigns`);
      setCampaigns(data.items || []);
    } catch { setError('Erro ao carregar campanhas'); }
    finally { setLoading(false); }
  }

  if (loading) return <div className="text-slate-400 text-center py-8">Carregando...</div>;
  if (!pid) return <div className="text-slate-400 text-center py-8">Selecione um projeto primeiro.</div>;

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-white">Campanhas</h1>
          <p className="text-slate-400 text-sm mt-1">Gerencie as campanhas de incentivo</p>
        </div>
      </div>
      {error && <div className="mb-4 p-3 bg-red-900/30 border border-red-800/50 rounded-lg text-red-400 text-sm">{error}</div>}
      {campaigns.length === 0 ? (
        <div className="text-center py-12 text-slate-400">Nenhuma campanha encontrada.</div>
      ) : (
        <div className="space-y-3">
          {campaigns.map((c: any) => (
            <div key={c.id} className="bg-slate-900 rounded-xl border border-slate-800 p-4">
              <h3 className="text-white font-semibold">{c.name}</h3>
              <div className="flex gap-2 mt-2 text-sm text-slate-400">
                <span className={`px-2 py-0.5 rounded text-xs font-medium border ${
                  c.status === 'Active' ? 'bg-green-500/20 text-green-400 border-green-800/50' :
                  c.status === 'Draft' ? 'bg-slate-500/20 text-slate-400 border-slate-800/50' :
                  'bg-amber-500/20 text-amber-400 border-amber-800/50'
                }`}>{c.status}</span>
                <span className="text-slate-500">{c.type}</span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
