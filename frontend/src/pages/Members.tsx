import { useState, useEffect } from 'react';
import api from '../lib/api';
import type { Member } from '../lib/types';
import DataTable, { type Column } from '../components/DataTable';

export default function Members() {
  const [members, setMembers] = useState<Member[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    api
      .get<Member[]>('/organizations/current/members')
      .then(({ data }) => setMembers(data))
      .catch(() => setError('Erro ao carregar membros.'))
      .finally(() => setLoading(false));
  }, []);

  if (error) {
    return (
      <div className="bg-red-900/50 border border-red-700 text-red-200 px-4 py-3 rounded-lg">
        {error}
      </div>
    );
  }

  const columns: Column<Member>[] = [
    {
      key: 'name',
      header: 'Nome',
      render: (m) => <span className="text-white">{m.name}</span>,
    },
    { key: 'email', header: 'Email' },
    {
      key: 'role',
      header: 'Role',
      render: (m) => (
        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-indigo-600/20 text-indigo-300 border border-indigo-800/50">
          {m.role}
        </span>
      ),
    },
    {
      key: 'joinedAt',
      header: 'Membro desde',
      render: (m) => new Date(m.joinedAt).toLocaleDateString('pt-BR'),
    },
  ];

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-white">Membros</h1>
          <p className="text-slate-400 text-sm mt-1">
            {members.length} membro(s) na organização
          </p>
        </div>
      </div>

      <DataTable
        columns={columns}
        data={members}
        keyField="id"
        loading={loading}
        emptyMessage="Nenhum membro encontrado."
      />
    </div>
  );
}
