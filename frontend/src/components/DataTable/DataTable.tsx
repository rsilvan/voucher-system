import type { ReactNode } from 'react';

export interface Column<T> {
  key: string;
  header: ReactNode;
  render?: (item: T, index: number) => ReactNode;
  className?: string;
}

interface DataTableProps<T> {
  columns: Column<T>[];
  data: T[];
  keyField: string;
  emptyMessage?: string;
  loading?: boolean;
  pagination?: {
    page: number;
    totalPages: number;
    totalCount?: number;
    onPageChange: (page: number) => void;
  };
}

export default function DataTable<T>({
  columns,
  data,
  keyField,
  emptyMessage = 'Nenhum registro encontrado.',
  loading = false,
  pagination,
}: DataTableProps<T>) {
  if (loading && data.length === 0) {
    return (
      <div className="bg-slate-900 rounded-xl border border-slate-800 overflow-hidden">
        <div className="flex items-center justify-center h-48">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-500" />
        </div>
      </div>
    );
  }

  return (
    <div className="bg-slate-900 rounded-xl border border-slate-800 overflow-hidden">
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead>
            <tr className="border-b border-slate-800">
              {columns.map((col) => (
                <th
                  key={col.key}
                  className={`text-left px-6 py-4 text-sm font-medium text-slate-400 whitespace-nowrap ${col.className ?? ''}`}
                >
                  {col.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800">
            {data.length === 0 ? (
              <tr>
                <td
                  colSpan={columns.length}
                  className="px-6 py-12 text-center text-sm text-slate-500"
                >
                  {emptyMessage}
                </td>
              </tr>
            ) : (
              data.map((item, idx) => (
                <tr
                  key={String((item as any)[keyField])}
                  className="hover:bg-slate-800/50 transition"
                >
                  {columns.map((col) => (
                    <td
                      key={col.key}
                      className={`px-6 py-4 text-sm text-slate-300 ${col.className ?? ''}`}
                    >
                      {col.render
                        ? col.render(item, idx)
                        : String((item as any)[col.key] ?? '')}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {pagination && pagination.totalPages > 1 && (
        <div className="flex items-center justify-between px-6 py-4 border-t border-slate-800">
          <p className="text-sm text-slate-400">
            Página {pagination.page} de {pagination.totalPages}
            {pagination.totalCount != null && ` (${pagination.totalCount} registro${pagination.totalCount !== 1 ? 's' : ''})`}
          </p>
          <div className="flex items-center gap-2">
            <button
              onClick={() => pagination.onPageChange(pagination.page - 1)}
              disabled={pagination.page <= 1}
              className="px-3 py-1.5 text-sm rounded-lg bg-slate-800 text-slate-300 hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition border border-slate-700"
            >
              Anterior
            </button>
            {Array.from(
              { length: Math.min(pagination.totalPages, 5) },
              (_, i) => {
                const startPage = Math.max(
                  1,
                  Math.min(pagination.page - 2, pagination.totalPages - 4),
                );
                const pageNum = startPage + i;
                if (pageNum > pagination.totalPages) return null;
                return (
                  <button
                    key={pageNum}
                    onClick={() => pagination.onPageChange(pageNum)}
                    className={`px-3 py-1.5 text-sm rounded-lg transition border ${
                      pageNum === pagination.page
                        ? 'bg-indigo-600/20 text-indigo-300 border-indigo-800/50'
                        : 'bg-slate-800 text-slate-400 hover:bg-slate-700 border-slate-700'
                    }`}
                  >
                    {pageNum}
                  </button>
                );
              },
            )}
            <button
              onClick={() => pagination.onPageChange(pagination.page + 1)}
              disabled={pagination.page >= pagination.totalPages}
              className="px-3 py-1.5 text-sm rounded-lg bg-slate-800 text-slate-300 hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition border border-slate-700"
            >
              Próximo
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
