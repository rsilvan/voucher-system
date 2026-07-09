import { useState, useEffect, useCallback } from 'react';
import api from '../lib/api';
import type { AuditLogEntry, AuditLogListResponse } from '../lib/types';
import Card from '../components/Card';
import DataTable, { type Column } from '../components/DataTable';

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

  if (error) {
    return (
      <div className="bg-red-900/50 border border-red-700 text-red-200 px-4 py-3 rounded-lg">
        {error}
      </div>
    );
  }

  const columns: Column<AuditLogEntry>[] = [
    {
      key: 'timestamp',
      header: 'Timestamp',
      render: (log) => (
        <span className="whitespace-nowrap">{formatTimestamp(log.createdAt)}</span>
      ),
    },
    {
      key: 'action',
      header: 'Ação',
      render: (log) => (
        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-indigo-600/20 text-indigo-300 border border-indigo-800/50">
          {log.action}
        </span>
      ),
      className: 'whitespace-nowrap',
    },
    {
      key: 'resourceType',
      header: 'Tipo de Recurso',
      className: 'whitespace-nowrap',
    },
    {
      key: 'resourceId',
      header: 'ID do Recurso',
      render: (log) => (
        <span className="font-mono max-w-[180px] truncate block">{log.resourceId}</span>
      ),
      className: 'whitespace-nowrap',
    },
    {
      key: 'actor',
      header: 'Ator',
      render: (log) => (
        <span className="whitespace-nowrap">
          {log.actorUserId || <span className="text-slate-600">—</span>}
        </span>
      ),
      className: 'whitespace-nowrap',
    },
    {
      key: 'metadata',
      header: 'Metadados',
      render: (log) => (
        <span className="max-w-[220px] truncate block">
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
        </span>
      ),
    },
  ];

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
      <Card padding="md" className="mb-6">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
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
      </Card>

      {/* Table */}
      <DataTable
        columns={columns}
        data={logs}
        keyField="id"
        loading={loading && logs.length === 0}
        emptyMessage="Nenhum registro de auditoria encontrado."
        pagination={
          totalPages > 1
            ? {
                page,
                totalPages,
                totalCount,
                onPageChange: setPage,
              }
            : undefined
        }
      />

      {/* Loading overlay on subsequent pages */}
      {loading && logs.length > 0 && (
        <div className="flex justify-center mt-4">
          <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-indigo-500" />
        </div>
      )}
    </div>
  );
}
