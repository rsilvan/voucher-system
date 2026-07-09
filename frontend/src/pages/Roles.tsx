import { useState, useEffect } from 'react';
import api from '../lib/api';
import type { Role } from '../lib/types';
import Card from '../components/Card';

export default function Roles() {
  const [roles, setRoles] = useState<Role[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    api
      .get<Role[]>('/roles')
      .then(({ data }) => setRoles(data))
      .catch(() => setError('Erro ao carregar roles.'))
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
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-white">Roles</h1>
        <p className="text-slate-400 text-sm mt-1">
          {roles.length} role(s) disponíveis
        </p>
      </div>

      <div className="grid gap-4">
        {roles.map((role) => (
          <Card key={role.id} padding="lg" hover>
            <div className="flex items-start justify-between mb-3">
              <div>
                <h3 className="text-lg font-semibold text-white">
                  {role.name}
                </h3>
                {role.description && (
                  <p className="text-sm text-slate-400 mt-1">
                    {role.description}
                  </p>
                )}
              </div>
            </div>

            {role.permissions && role.permissions.length > 0 && (
              <div className="flex flex-wrap gap-2 mt-3">
                {role.permissions.map((perm) => (
                  <span
                    key={perm}
                    className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-slate-800 text-slate-300 border border-slate-700"
                  >
                    {perm}
                  </span>
                ))}
              </div>
            )}
          </Card>
        ))}
        {roles.length === 0 && (
          <Card padding="xl" className="text-center text-slate-500">
            Nenhuma role encontrada.
          </Card>
        )}
      </div>
    </div>
  );
}
