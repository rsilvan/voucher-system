import { useState } from 'react';
import Card from '../Card';

interface MetadataEditorProps {
  metadata: Record<string, string>;
  onChange: (metadata: Record<string, string>) => void;
  readonly?: boolean;
}

export default function MetadataEditor({ metadata, onChange, readonly = false }: MetadataEditorProps) {
  const [entries, setEntries] = useState<[string, string][]>(
    () => Object.entries(metadata)
  );

  function addEntry() {
    setEntries([...entries, ['', '']]);
  }

  function updateKey(idx: number, key: string) {
    const next = [...entries];
    next[idx] = [key, next[idx][1]];
    setEntries(next);
    if (key) syncToParent(next);
  }

  function updateValue(idx: number, value: string) {
    const next = [...entries];
    next[idx] = [next[idx][0], value];
    setEntries(next);
    syncToParent(next);
  }

  function removeEntry(idx: number) {
    const next = entries.filter((_, i) => i !== idx);
    setEntries(next);
    syncToParent(next);
  }

  function syncToParent(entries: [string, string][]) {
    const obj: Record<string, string> = {};
    for (const [k, v] of entries) {
      if (k) obj[k] = v;
    }
    onChange(obj);
  }

  return (
    <Card padding="lg">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-sm font-medium text-slate-400">Metadados</h3>
        {!readonly && (
          <button
            onClick={addEntry}
            className="px-3 py-1 text-xs bg-indigo-600/20 text-indigo-300 border border-indigo-800/50 rounded-lg hover:bg-indigo-600/30 transition"
          >
            + Adicionar
          </button>
        )}
      </div>
      <div className="space-y-2">
        {entries.length === 0 && (
          <p className="text-sm text-slate-500 text-center py-4">
            Nenhum metadado definido.
          </p>
        )}
        {entries.map(([key, value], idx) => (
          <div key={idx} className="flex gap-2">
            {readonly ? (
              <>
                <span className="flex-1 bg-slate-800 rounded-lg px-3 py-2 text-sm text-slate-300 font-mono">
                  {key}
                </span>
                <span className="flex-1 bg-slate-800 rounded-lg px-3 py-2 text-sm text-slate-400">
                  {value}
                </span>
              </>
            ) : (
              <>
                <input
                  value={key}
                  onChange={(e) => updateKey(idx, e.target.value)}
                  placeholder="Chave"
                  className="flex-1 bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-indigo-500"
                />
                <input
                  value={value}
                  onChange={(e) => updateValue(idx, e.target.value)}
                  placeholder="Valor"
                  className="flex-1 bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-indigo-500"
                />
                <button
                  onClick={() => removeEntry(idx)}
                  className="px-2 text-slate-500 hover:text-red-400 transition text-sm"
                >
                  ✕
                </button>
              </>
            )}
          </div>
        ))}
      </div>
    </Card>
  );
}
