import { useState, useEffect, useCallback } from 'react';
import api from '../lib/api';
import type { AuditLogEntry, AuditLogListResponse } from '../lib/types';

const PAGE_SIZE = 20;

const ACTION_OPTIONS = [
  '',
  'CREATE',
  'UPDATE',
  'DELETE',
  'LOGIN',
  'LOGOUT',
  'ACTIVATE',
  'DEACTIVATE',
  'ASSIGN',
  'UNASSIGN',
  'TRANSFER',
  'REDEEM',
  'REFUND',
  'EXPORT',
  'IMPORT',
];

export default function AuditLogs() {
  const [logs, setLogs] = useState<AuditLogEntry[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Filters
  const [actionFilter, setActionFilter] = useState('');
  const [resourceTypeFilter, setResourceTypeFilter] = useState('');
  const [dateStart, setDateStart] = useState('');
  const [dateEnd, setDateEnd] = useState('');

  const fetchLogs = useCallback(async () => {
    setLoading(true);
    setError('');

    const params = new URLSearchParams();
    params.set('page', String(page));
    params.set('pageSize', String(PAGE_SIZE));
    if (actionFilter) params.set('action', actionFilter);
    if (resourceTypeFilter) params.set('resourceType', resourceTypeFilter);
    if (dateStart) params.set('dateStart', dateStart);
    if (dateEnd) params.set('dateEnd', dateEnd);

    try {
      const { data } = await api.get<AuditLogListResponse>(
        `/audit-logs?${params.toString()}`,
      );
      setLogs(data.items);
      setTotalCount(data.totalCount);
    } catch {
      setError('Erro ao carregar logs de auditoria.');
    } finally {
      setLoading(false);
    }
  }, [page, actionFilter, resourceTypeFilter, dateStart, dateEnd]);

  useEffect(() => {
    fetchLogs();
  }, [fetchLogs]);

  // Reset to page 1 when filters change
  function handleFilterChange(
    setter: React.Dispatch<React.SetStateAction<string>>,
  ) {
    return (value: string) => {
      setter(value);
      setPage(1);
    };
  }

  function formatTimestamp(iso: string) {
    return new Date(iso).toLocaleString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });
  }

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  if (loading && logs.length === 0) {
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
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-white">Auditoria</h1>
          <p className="text-slate-400 text-sm mt-1">
            {totalCount} registro(s) encontrado(s)
          </p>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-slate-900 rounded-xl border border-slate-800 p-4 mb-6">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
          {/* Action filter */}
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1">
              Ação
            </label>
            <select
              value={actionFilter}
              onChange={(e) => handleFilterChange(setActionFilter)(e.target.value)}
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-slate-200 focus:outline-none focus:ring-2 focus:ring-indigo-500/50 focus:border-indigo-500"
            >
              <option value="">Todas</option>
              {ACTION_OPTIONS.filter(Boolean).map((action) => (
                <option key={action} value={action}>
                  {action}
                </option>
              ))}
            </select>
          </div>

          {/* Resource type filter */}
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1">
              Tipo de Recurso
            </label>
            <input
              type="text"
              value={resourceTypeFilter}
              onChange={(e) =>
                handleFilterChange(setResourceTypeFilter)(e.target.value)
              }
              placeholder="Ex: Project, User..."
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-slate-200 placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/50 focus:border-indigo-500"
            />
          </div>

          {/* Date start */}
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1">
              Data Inicial
            </label>
            <input
              type="date"
              value={dateStart}
              onChange={(e) =>
                handleFilterChange(setDateStart)(e.target.value)
              }
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-slate-200 focus:outline-none focus:ring-2 focus:ring-indigo-500/50 focus:border-indigo-500"
            />
          </div>

          {/* Date end */}
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1">
              Data Final
            </label>
            <input
              type="date"
              value={dateEnd}
              onChange={(e) =>
                handleFilterChange(setDateEnd)(e.target.value)
              }
              className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-slate-200 focus:outline-none focus:ring-2 focus:ring-indigo-500/50 focus:border-indigo-500"
            />
          </div>

          {/* Clear filters */}
          <div className="flex items-end">
            <button
              onClick={() => {
                setActionFilter('');
                setResourceTypeFilter('');
                setDateStart('');
                setDateEnd('');
                setPage(1);
              }}
              className="w-full px-4 py-2 text-sm text-slate-400 hover:text-white bg-slate-800 hover:bg-slate-700 rounded-lg transition border border-slate-700"
            >
              Limpar Filtros
            </button>
          </div>
        </div>
      </div>

      {/* Table */}
      <div className="bg-slate-900 rounded-xl border border-slate-800 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-slate-800">
                <th className="text-left px-6 py-4 text-sm font-medium text-slate-400 whitespace-nowrap">
                  Timestamp
                </th>
                <th className="text-left px-6 py-4 text-sm font-medium text-slate-400 whitespace-nowrap">
                  Ação
                </th>
                <th className="text-left px-6 py-4 text-sm font-medium text-slate-400 whitespace-nowrap">
                  Tipo de Recurso
                </th>
                <th className="text-left px-6 py-4 text-sm font-medium text-slate-400 whitespace-nowrap">
                  ID do Recurso
                </th>
                <th className="text-left px-6 py-4 text-sm font-medium text-slate-400 whitespace-nowrap">
                  Ator
                </th>
                <th className="text-left px-6 py-4 text-sm font-medium text-slate-400 whitespace-nowrap">
                  Metadados
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800">
              {logs.length === 0 ? (
                <tr>
                  <td
                    colSpan={6}
                    className="px-6 py-12 text-center text-sm text-slate-500"
                  >
                    Nenhum registro de auditoria encontrado.
                  </td>
                </tr>
              ) : (
                logs.map((log) => (
                  <tr
                    key={log.id}
                    className="hover:bg-slate-800/50 transition"
                  >
                    <td className="px-6 py-4 text-sm text-slate-300 whitespace-nowrap">
                      {formatTimestamp(log.createdAt)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-indigo-600/20 text-indigo-300 border border-indigo-800/50">
                        {log.action}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-sm text-slate-300 whitespace-nowrap">
                      {log.resourceType}
                    </td>
                    <td className="px-6 py-4 text-sm text-slate-400 font-mono whitespace-nowrap max-w-[180px] truncate">
                      {log.resourceId}
                    </td>
                    <td className="px-6 py-4 text-sm text-slate-300 whitespace-nowrap">
                      {log.actorUserId || (
                        <span className="text-slate-600">—</span>
                      )}
                    </td>
                    <td className="px-6 py-4 text-sm text-slate-400 max-w-[220px] truncate">
                      {log.metadata ? (
                        <span
                          className="cursor-help border-b border-dotted border-slate-600"
                          title={JSON.stringify(log.metadata, null, 2)}
                        >
                          {JSON.stringify(log.metadata)}
                        </span>
                      ) : (
                        <span className="text-slate-600">—</span>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="flex items-center justify-between px-6 py-4 border-t border-slate-800">
            <p className="text-sm text-slate-400">
              Página {page} de {totalPages}
            </p>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="px-3 py-1.5 text-sm rounded-lg bg-slate-800 text-slate-300 hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition border border-slate-700"
              >
                Anterior
              </button>
              {Array.from({ length: Math.min(totalPages, 5) }, (_, i) => {
                const startPage = Math.max(
                  1,
                  Math.min(page - 2, totalPages - 4),
                );
                const pageNum = startPage + i;
                if (pageNum > totalPages) return null;
                return (
                  <button
                    key={pageNum}
                    onClick={() => setPage(pageNum)}
                    className={`px-3 py-1.5 text-sm rounded-lg transition border ${
                      pageNum === page
                        ? 'bg-indigo-600/20 text-indigo-300 border-indigo-800/50'
                        : 'bg-slate-800 text-slate-400 hover:bg-slate-700 border-slate-700'
                    }`}
                  >
                    {pageNum}
                  </button>
                );
              })}
              <button
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
                className="px-3 py-1.5 text-sm rounded-lg bg-slate-800 text-slate-300 hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition border border-slate-700"
              >
                Próximo
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Loading overlay on subsequent pages */}
      {loading && logs.length > 0 && (
        <div className="flex justify-center mt-4">
          <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-indigo-500" />
        </div>
      )}
    </div>
  );
}
