import { useState, useEffect, useRef } from 'react';
import api from '../lib/api';
import type { GeoLocationResponse } from '../lib/types';

export default function GeoLocationsPage({ projectId }: { projectId?: string }) {
  const pid = projectId || localStorage.getItem('currentProjectId') || '';
  const [locations, setLocations] = useState<GeoLocationResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);
  const [selected, setSelected] = useState<GeoLocationResponse | null>(null);

  useEffect(() => { if (pid) loadLocations(); else setLoading(false); }, [pid]);

  async function loadLocations() {
    try {
      const { data } = await api.get(`/projects/${pid}/locations`);
      setLocations(Array.isArray(data) ? data : data.items || []);
    } catch { /* empty */ }
    finally { setLoading(false); }
  }

  if (loading) return <div className="text-slate-400 text-center py-8">Carregando...</div>;
  if (!pid) return <div className="text-slate-400 text-center py-8">Selecione um projeto primeiro.</div>;

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-white">Localizações</h1>
          <p className="text-slate-400 text-sm mt-1">Áreas geográficas para regras de elegibilidade</p>
        </div>
        <button onClick={() => setShowCreate(true)}
          className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 text-white rounded-lg text-sm font-medium transition">
          + Nova Localização
        </button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-1 space-y-2">
          {locations.length === 0 ? (
            <div className="text-slate-400 text-sm py-4">Nenhuma localização cadastrada.</div>
          ) : (
            locations.map(loc => (
              <button key={loc.id} onClick={() => setSelected(loc)}
                className={`w-full text-left p-3 rounded-lg border transition ${
                  selected?.id === loc.id ? 'bg-indigo-900/30 border-indigo-800/50' : 'bg-slate-900 border-slate-800 hover:border-slate-700'
                }`}>
                <p className="text-sm font-medium text-white">{loc.name}</p>
                <p className="text-xs text-slate-500 mt-0.5">{loc.type}{loc.latitude ? ` · ${loc.latitude.toFixed(4)}, ${loc.longitude?.toFixed(4)}` : ''}</p>
              </button>
            ))
          )}
        </div>

        <div className="lg:col-span-2">
          {selected ? (
            <div className="bg-slate-900 rounded-xl border border-slate-800 p-5">
              <h2 className="text-lg font-semibold text-white mb-3">{selected.name}</h2>
              {selected.description && <p className="text-sm text-slate-400 mb-4">{selected.description}</p>}
              
              {/* SVG Preview */}
              <div className="bg-slate-950 rounded-lg p-4 mb-4 flex items-center justify-center min-h-[200px]">
                <GeoPreview location={selected} />
              </div>

              <div className="grid grid-cols-2 gap-3 text-sm">
                <div><span className="text-slate-500">Tipo:</span> <span className="text-slate-300">{selected.type}</span></div>
                {selected.latitude !== null && <div><span className="text-slate-500">Latitude:</span> <span className="text-slate-300">{selected.latitude}</span></div>}
                {selected.longitude !== null && <div><span className="text-slate-500">Longitude:</span> <span className="text-slate-300">{selected.longitude}</span></div>}
                {selected.radius !== null && <div><span className="text-slate-500">Raio:</span> <span className="text-slate-300">{selected.radius} {selected.unit || 'km'}</span></div>}
              </div>
            </div>
          ) : (
            <div className="bg-slate-900 rounded-xl border border-slate-800 p-5 flex items-center justify-center min-h-[200px]">
              <p className="text-slate-500">Selecione uma localização para visualizar</p>
            </div>
          )}
        </div>
      </div>

      {showCreate && <GeoLocationModal projectId={pid} onClose={() => setShowCreate(false)} onCreated={() => { setShowCreate(false); loadLocations(); }} />}
    </div>
  );
}

function GeoPreview({ location }: { location: GeoLocationResponse }) {
  const svgRef = useRef<SVGSVGElement>(null);
  const size = 200;
  const cx = size / 2;
  const cy = size / 2;

  if (location.type === 'Circle' && location.radius) {
    // Scale radius: max 500km = half the SVG
    const r = Math.min(location.radius, 500) / 500 * (size / 2 - 10);
    return (
      <svg ref={svgRef} width={size} height={size} viewBox={`0 0 ${size} ${size}`}>
        <circle cx={cx} cy={cy} r={r} fill="none" stroke="#6366f1" strokeWidth="2" strokeDasharray="4 2" />
        <circle cx={cx} cy={cy} r="3" fill="#818cf8" />
        <text x={cx} y={cy + r + 16} textAnchor="middle" fill="#94a3b8" fontSize="10">{location.radius}{location.unit || 'km'}</text>
        <text x={cx} y={cy - r - 6} textAnchor="middle" fill="#6366f1" fontSize="9">Circle</text>
      </svg>
    );
  }

  if (location.type === 'Polygon') {
    try {
      const coords = JSON.parse(location.coordinates);
      const ring = coords.coordinates?.[0] || coords[0] || [];
      if (ring.length > 2) {
        // Normalize to fit in SVG
        const lats = ring.map((p: number[]) => p[1]);
        const lngs = ring.map((p: number[]) => p[0]);
        const minLat = Math.min(...lats), maxLat = Math.max(...lats);
        const minLng = Math.min(...lngs), maxLng = Math.max(...lngs);
        const pad = 20;
        const scaleX = (size - pad * 2) / (maxLng - minLng || 1);
        const scaleY = (size - pad * 2) / (maxLat - minLat || 1);
        const scale = Math.min(scaleX, scaleY) * 0.8;
        const points = ring.map((p: number[]) => {
          const x = pad + (p[0] - minLng) * scale;
          const y = size - pad - (p[1] - minLat) * scale;
          return `${x},${y}`;
        }).join(' ');

        return (
          <svg ref={svgRef} width={size} height={size} viewBox={`0 0 ${size} ${size}`}>
            <polygon points={points} fill="rgba(99, 102, 241, 0.15)" stroke="#6366f1" strokeWidth="2" />
          </svg>
        );
      }
    } catch { /* invalid JSON */ }
  }

  if (location.type === 'MultiPolygon') {
    try {
      const coords = JSON.parse(location.coordinates);
      const polygons = coords.coordinates || coords;
      return (
        <svg ref={svgRef} width={size} height={size} viewBox={`0 0 ${size} ${size}`}>
          {polygons.map((poly: number[][][], i: number) => {
            const ring = poly[0] || [];
            if (ring.length < 3) return null;
            const lats = ring.map((p: number[]) => p[1]);
            const lngs = ring.map((p: number[]) => p[0]);
            const minLat = Math.min(...lats), maxLat = Math.max(...lats);
            const minLng = Math.min(...lngs), maxLng = Math.max(...lngs);
            const pad = 20;
            const scaleX = (size - pad * 2) / (maxLng - minLng || 1);
            const scaleY = (size - pad * 2) / (maxLat - minLat || 1);
            const scale = Math.min(scaleX, scaleY) * 0.8;
            const points = ring.map((p: number[]) => {
              const x = pad + (p[0] - minLng) * scale;
              const y = size - pad - (p[1] - minLat) * scale;
              return `${x},${y}`;
            }).join(' ');
            return <polygon key={i} points={points} fill="rgba(99, 102, 241, 0.15)" stroke="#6366f1" strokeWidth="2" />;
          })}
        </svg>
      );
    } catch { /* invalid JSON */ }
  }

  return <p className="text-slate-500 text-sm">Preview não disponível para {location.type}</p>;
}

function GeoLocationModal({ projectId, onClose, onCreated }: { projectId: string; onClose: () => void; onCreated: () => void }) {
  const [form, setForm] = useState({ name: '', description: '', type: 'Circle', latitude: '', longitude: '', radius: '', unit: 'km', coordinates: '{}' });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.name.trim()) { setError('Nome é obrigatório'); return; }
    setSaving(true);
    try {
      const payload: Record<string, unknown> = { name: form.name, description: form.description, type: form.type };
      if (form.type === 'Circle') {
        payload.latitude = parseFloat(form.latitude) || 0;
        payload.longitude = parseFloat(form.longitude) || 0;
        payload.radius = parseFloat(form.radius) || 0;
        payload.unit = form.unit;
        payload.coordinates = JSON.stringify({ type: 'Circle', coordinates: [form.longitude, form.latitude] });
      } else {
        payload.coordinates = form.coordinates;
      }
      await api.post(`/projects/${projectId}/locations`, payload);
      onCreated();
    } catch { setError('Erro ao criar localização'); }
    finally { setSaving(false); }
  }

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50" onClick={onClose}>
      <div className="bg-slate-900 rounded-xl border border-slate-800 p-6 w-full max-w-lg mx-4" onClick={e => e.stopPropagation()}>
        <h2 className="text-lg font-bold text-white mb-4">Nova Localização</h2>
        {error && <p className="text-red-400 text-sm mb-3">{error}</p>}
        <form onSubmit={handleSubmit} className="space-y-3">
          <div>
            <label className="block text-sm text-slate-400 mb-1">Nome *</label>
            <input value={form.name} onChange={e => setForm({ ...form, name: e.target.value })}
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm" />
          </div>
          <div>
            <label className="block text-sm text-slate-400 mb-1">Tipo</label>
            <select value={form.type} onChange={e => setForm({ ...form, type: e.target.value })}
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm">
              <option value="Circle">Círculo</option>
              <option value="Polygon">Polígono</option>
              <option value="MultiPolygon">MultiPolygon</option>
            </select>
          </div>
          {form.type === 'Circle' ? (
            <div className="grid grid-cols-3 gap-3">
              <div>
                <label className="block text-sm text-slate-400 mb-1">Latitude</label>
                <input type="number" step="any" value={form.latitude} onChange={e => setForm({ ...form, latitude: e.target.value })}
                  className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm" />
              </div>
              <div>
                <label className="block text-sm text-slate-400 mb-1">Longitude</label>
                <input type="number" step="any" value={form.longitude} onChange={e => setForm({ ...form, longitude: e.target.value })}
                  className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm" />
              </div>
              <div>
                <label className="block text-sm text-slate-400 mb-1">Raio</label>
                <div className="flex gap-1">
                  <input type="number" min="0" step="any" value={form.radius} onChange={e => setForm({ ...form, radius: e.target.value })}
                    className="flex-1 bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm" />
                  <select value={form.unit} onChange={e => setForm({ ...form, unit: e.target.value })}
                    className="w-16 bg-slate-800 border border-slate-700 rounded-lg px-2 py-2 text-white text-sm">
                    <option value="km">km</option>
                    <option value="mi">mi</option>
                  </select>
                </div>
              </div>
            </div>
          ) : (
            <div>
              <label className="block text-sm text-slate-400 mb-1">GeoJSON</label>
              <textarea value={form.coordinates} onChange={e => setForm({ ...form, coordinates: e.target.value })}
                className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm font-mono" rows={5} />
              <p className="text-xs text-slate-500 mt-1">Cole o GeoJSON completo do polígono</p>
            </div>
          )}
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
