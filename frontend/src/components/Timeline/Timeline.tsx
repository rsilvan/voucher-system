import Card from '../Card';

interface TimelineEvent {
  id: string;
  title: string;
  description?: string;
  timestamp: string;
  type?: 'info' | 'success' | 'warning' | 'error';
}

interface TimelineProps {
  events: TimelineEvent[];
  emptyMessage?: string;
}

const typeColors = {
  info: 'border-indigo-500 bg-indigo-500',
  success: 'border-green-500 bg-green-500',
  warning: 'border-amber-500 bg-amber-500',
  error: 'border-red-500 bg-red-500',
} as const;

export default function Timeline({ events, emptyMessage = 'Nenhum evento registrado.' }: TimelineProps) {
  return (
    <Card padding="lg">
      <h3 className="text-sm font-medium text-slate-400 mb-4">Linha do Tempo</h3>
      {events.length === 0 ? (
        <p className="text-sm text-slate-500 text-center py-4">{emptyMessage}</p>
      ) : (
        <div className="space-y-0">
          {events.map((event, idx) => {
            const color = typeColors[event.type || 'info'];
            return (
              <div key={event.id} className="flex gap-4">
                <div className="flex flex-col items-center">
                  <div className={`w-3 h-3 rounded-full border-2 ${color}`} />
                  {idx < events.length - 1 && (
                    <div className="w-px flex-1 bg-slate-800 my-1" />
                  )}
                </div>
                <div className={`pb-4 ${idx === events.length - 1 ? '' : ''}`}>
                  <p className="text-sm font-medium text-white">{event.title}</p>
                  {event.description && (
                    <p className="text-xs text-slate-400 mt-0.5">{event.description}</p>
                  )}
                  <p className="text-xs text-slate-600 mt-1">
                    {new Date(event.timestamp).toLocaleString('pt-BR')}
                  </p>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </Card>
  );
}

export type { TimelineEvent };
