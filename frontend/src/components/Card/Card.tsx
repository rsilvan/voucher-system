import type { ReactNode, HTMLAttributes } from 'react';

const paddingMap = {
  sm: 'p-3',
  md: 'p-4',
  lg: 'p-6',
  xl: 'p-8',
} as const;

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
  hover?: boolean;
  padding?: keyof typeof paddingMap;
}

export default function Card({
  children,
  className = '',
  hover = false,
  padding = 'md',
  ...rest
}: CardProps) {
  return (
    <div
      className={`bg-slate-900 rounded-xl border border-slate-800 ${paddingMap[padding]} ${
        hover ? 'hover:border-slate-700 transition' : ''
      } ${className}`}
      {...rest}
    >
      {children}
    </div>
  );
}
