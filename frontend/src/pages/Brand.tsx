import { useState, useEffect } from 'react';
import api from '../lib/api';
import type { BrandResponse, BrandAddress } from '../lib/types';
import Card from '../components/Card';

export default function BrandPage({ projectId }: { projectId?: string }) {
  const [brand, setBrand] = useState<BrandResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [editMode, setEditMode] = useState(false);
  const [error, setError] = useState('');

  const [form, setForm] = useState({
    name: '', description: '', websiteUrl: '', termsUrl: '', privacyUrl: '',
    supportEmail: '', logoUrl: '', primaryColor: '#6366f1', secondaryColor: '#8b5cf6',
    address: { street: '', city: '', state: '', zipCode: '', country: 'BR' } as BrandAddress,
  });

  const pid = projectId || localStorage.getItem('currentProjectId') || '';

  useEffect(() => { loadBrand(); }, [pid]);

  async function loadBrand() {
    if (!pid) { setLoading(false); return; }
    try {
      const { data } = await api.get<BrandResponse>(`/projects/${pid}/brand`);
      setBrand(data);
      setForm({
        name: data.name || '',
        description: data.description || '',
        websiteUrl: data.websiteUrl || '',
        termsUrl: data.termsUrl || '',
        privacyUrl: data.privacyUrl || '',
        supportEmail: data.supportEmail || '',
        logoUrl: data.logoUrl || '',
        primaryColor: data.primaryColor || '#6366f1',
        secondaryColor: data.secondaryColor || '#8b5cf6',
        address: data.address || { street: '', city: '', state: '', zipCode: '', country: 'BR' },
      });
    } catch {
      // 404 = no brand yet
    } finally {
      setLoading(false);
    }
  }

  async function handleSave() {
    if (!form.name.trim()) { setError('Nome é obrigatório'); return; }
    setSaving(true); setError('');
    try {
      if (brand) {
        await api.put(`/projects/${pid}/brand`, form);
      } else {
        await api.post(`/projects/${pid}/brand`, form);
      }
      await loadBrand();
      setEditMode(false);
    } catch (err: unknown) {
      setError('Erro ao salvar marca');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete() {
    if (!confirm('Remover a marca deste projeto?')) return;
    try {
      await api.delete(`/projects/${pid}/brand`);
      setBrand(null);
      setEditMode(true);
    } catch {
      setError('Erro ao remover marca');
    }
  }

  if (loading) return <div className="text-slate-400 text-center py-8">Carregando...</div>;
  if (!pid) return <div className="text-slate-400 text-center py-8">Selecione um projeto primeiro.</div>;

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-white">Marca</h1>
          <p className="text-slate-400 text-sm mt-1">Identidade visual e informações do projeto</p>
        </div>
        <div className="flex gap-2">
          {!editMode && (
            <button onClick={() => setEditMode(true)} className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 text-white rounded-lg text-sm font-medium transition">
              Editar
            </button>
          )}
          {brand && !editMode && (
            <button onClick={handleDelete} className="px-4 py-2 bg-red-600/20 hover:bg-red-600/30 text-red-400 rounded-lg text-sm font-medium transition">
              Remover
            </button>
          )}
        </div>
      </div>

      {error && <div className="mb-4 p-3 bg-red-900/30 border border-red-800/50 rounded-lg text-red-400 text-sm">{error}</div>}

      {/* Preview */}
      {brand && !editMode && (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-6">
          <div className="lg:col-span-2 space-y-4">
            <Card padding="lg">
              <h2 className="text-lg font-semibold text-white mb-3">{brand.name}</h2>
              {brand.description && <p className="text-slate-400 text-sm mb-3">{brand.description}</p>}
              <div className="grid grid-cols-2 gap-3 text-sm">
                {brand.websiteUrl && <div><span className="text-slate-500">Site:</span> <a href={brand.websiteUrl} className="text-indigo-400 hover:underline">{brand.websiteUrl}</a></div>}
                {brand.supportEmail && <div><span className="text-slate-500">Email:</span> <span className="text-slate-300">{brand.supportEmail}</span></div>}
                {brand.primaryColor && <div><span className="text-slate-500">Cor primária:</span> <span className="inline-block w-4 h-4 rounded align-middle ml-1" style={{ backgroundColor: brand.primaryColor }} /> {brand.primaryColor}</div>}
                {brand.secondaryColor && <div><span className="text-slate-500">Cor secundária:</span> <span className="inline-block w-4 h-4 rounded align-middle ml-1" style={{ backgroundColor: brand.secondaryColor }} /> {brand.secondaryColor}</div>}
              </div>
            </Card>

            {brand.address?.street && (
              <Card padding="lg">
                <h3 className="text-sm font-medium text-slate-400 mb-2">Endereço</h3>
                <p className="text-sm text-slate-300">
                  {brand.address.street}{brand.address.city ? `, ${brand.address.city}` : ''}
                  {brand.address.state ? ` - ${brand.address.state}` : ''}
                  {brand.address.zipCode ? `, ${brand.address.zipCode}` : ''}
                  {brand.address.country ? ` (${brand.address.country})` : ''}
                </p>
              </Card>
            )}
          </div>

          <div className="space-y-4">
            {brand.logoUrl && (
              <Card padding="lg" className="text-center">
                <p className="text-sm text-slate-400 mb-2">Logo</p>
                <img src={brand.logoUrl} alt="Logo" className="max-h-24 mx-auto" />
              </Card>
            )}
            <Card padding="lg">
              <p className="text-sm text-slate-400 mb-2">Preview de cores</p>
              <div className="h-12 rounded-lg mb-2" style={{ background: `linear-gradient(135deg, ${brand.primaryColor || '#6366f1'}, ${brand.secondaryColor || '#8b5cf6'})` }} />
              <div className="flex gap-2">
                <button className="px-3 py-1 text-xs text-white rounded" style={{ backgroundColor: brand.primaryColor ?? undefined }}>Primária</button>
                <button className="px-3 py-1 text-xs text-white rounded" style={{ backgroundColor: brand.secondaryColor ?? undefined }}>Secundária</button>
              </div>
            </Card>
          </div>
        </div>
      )}

      {/* Edit/Create form */}
      {(editMode || !brand) && (
        <Card padding="lg">
          <h2 className="text-lg font-semibold text-white mb-4">{brand ? 'Editar Marca' : 'Criar Marca'}</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm text-slate-400 mb-1">Nome *</label>
              <input value={form.name} onChange={e => setForm({ ...form, name: e.target.value })}
                className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500" />
            </div>
            <div>
              <label className="block text-sm text-slate-400 mb-1">Website</label>
              <input value={form.websiteUrl} onChange={e => setForm({ ...form, websiteUrl: e.target.value })}
                className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500" />
            </div>
            <div className="md:col-span-2">
              <label className="block text-sm text-slate-400 mb-1">Descrição</label>
              <textarea value={form.description} onChange={e => setForm({ ...form, description: e.target.value })}
                className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500" rows={2} />
            </div>
            <div>
              <label className="block text-sm text-slate-400 mb-1">Email de Suporte</label>
              <input value={form.supportEmail} onChange={e => setForm({ ...form, supportEmail: e.target.value })}
                className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500" />
            </div>
            <div>
              <label className="block text-sm text-slate-400 mb-1">Logo URL</label>
              <input value={form.logoUrl} onChange={e => setForm({ ...form, logoUrl: e.target.value })}
                className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500" />
            </div>
            <div>
              <label className="block text-sm text-slate-400 mb-1">Cor Primária</label>
              <div className="flex gap-2">
                <input type="color" value={form.primaryColor} onChange={e => setForm({ ...form, primaryColor: e.target.value })}
                  className="h-9 w-12 bg-slate-800 border border-slate-700 rounded cursor-pointer" />
                <input value={form.primaryColor} onChange={e => setForm({ ...form, primaryColor: e.target.value })}
                  className="flex-1 bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500" />
              </div>
            </div>
            <div>
              <label className="block text-sm text-slate-400 mb-1">Cor Secundária</label>
              <div className="flex gap-2">
                <input type="color" value={form.secondaryColor} onChange={e => setForm({ ...form, secondaryColor: e.target.value })}
                  className="h-9 w-12 bg-slate-800 border border-slate-700 rounded cursor-pointer" />
                <input value={form.secondaryColor} onChange={e => setForm({ ...form, secondaryColor: e.target.value })}
                  className="flex-1 bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500" />
              </div>
            </div>
            <div className="md:col-span-2 border-t border-slate-800 pt-4 mt-2">
              <h3 className="text-sm font-medium text-slate-400 mb-3">Endereço (opcional)</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <div className="md:col-span-2">
                  <label className="block text-sm text-slate-400 mb-1">Logradouro</label>
                  <input value={form.address.street} onChange={e => setForm({ ...form, address: { ...form.address, street: e.target.value } })}
                    className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500" />
                </div>
                <div>
                  <label className="block text-sm text-slate-400 mb-1">Cidade</label>
                  <input value={form.address.city} onChange={e => setForm({ ...form, address: { ...form.address, city: e.target.value } })}
                    className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500" />
                </div>
                <div>
                  <label className="block text-sm text-slate-400 mb-1">Estado</label>
                  <input value={form.address.state} onChange={e => setForm({ ...form, address: { ...form.address, state: e.target.value } })}
                    className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500" />
                </div>
                <div>
                  <label className="block text-sm text-slate-400 mb-1">CEP</label>
                  <input value={form.address.zipCode} onChange={e => setForm({ ...form, address: { ...form.address, zipCode: e.target.value } })}
                    className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500" />
                </div>
                <div>
                  <label className="block text-sm text-slate-400 mb-1">País</label>
                  <input value={form.address.country} onChange={e => setForm({ ...form, address: { ...form.address, country: e.target.value } })}
                    className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500" maxLength={2} />
                </div>
              </div>
            </div>
          </div>

          {/* Live preview */}
          {(form.primaryColor || form.secondaryColor) && (
            <div className="mt-4 p-4 bg-slate-800 rounded-lg">
              <p className="text-sm text-slate-400 mb-2">Preview ao vivo</p>
              <div className="h-8 rounded-lg" style={{ background: `linear-gradient(135deg, ${form.primaryColor || '#6366f1'}, ${form.secondaryColor || '#8b5cf6'})` }} />
              <div className="flex gap-2 mt-2">
                <button className="px-3 py-1 text-xs text-white rounded" style={{ backgroundColor: form.primaryColor }}>Primária</button>
                <button className="px-3 py-1 text-xs text-white rounded" style={{ backgroundColor: form.secondaryColor }}>Secundária</button>
              </div>
            </div>
          )}

          <div className="flex justify-end gap-3 mt-6">
            <button onClick={() => setEditMode(false)} className="px-4 py-2 text-sm text-slate-400 hover:text-white transition">Cancelar</button>
            <button onClick={handleSave} disabled={saving}
              className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 disabled:bg-indigo-800 text-white rounded-lg text-sm font-medium transition">
              {saving ? 'Salvando...' : 'Salvar'}
            </button>
          </div>
        </Card>
      )}
    </div>
  );
}
