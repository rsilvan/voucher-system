import { useState, useEffect } from 'react';
import api from '../lib/api';
import type { Member } from '../lib/types';

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

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-500" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-900/50 border border-red-700 text-red-200 px-4 py-3 rounded-lg">
        {error}
      </div>
    );
  }

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

      <div className="bg-slate-900 rounded-xl border border-slate-800 overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="border-b border-slate-800">
              <th className="text-left px-6 py-4 text-sm font-medium text-slate-400">
                Nome
              </th>
              <th className="text-left px-6 py-4 text-sm font-medium text-slate-400">
                Email
              </th>
              <th className="text-left px-6 py-4 text-sm font-medium text-slate-400">
                Role
              </th>
              <th className="text-left px-6 py-4 text-sm font-medium text-slate-400">
                Membro desde
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800">
            {members.map((member) => (
              <tr
                key={member.id}
                className="hover:bg-slate-800/50 transition"
              >
                <td className="px-6 py-4 text-sm text-white">{member.name}</td>
                <td className="px-6 py-4 text-sm text-slate-300">
                  {member.email}
                </td>
                <td className="px-6 py-4">
                  <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-indigo-600/20 text-indigo-300 border border-indigo-800/50">
                    {member.role}
                  </span>
                </td>
                <td className="px-6 py-4 text-sm text-slate-400">
                  {new Date(member.joinedAt).toLocaleDateString('pt-BR')}
                </td>
              </tr>
            ))}
            {members.length === 0 && (
              <tr>
                <td
                  colSpan={4}
                  className="px-6 py-8 text-center text-slate-500"
                >
                  Nenhum membro encontrado.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
