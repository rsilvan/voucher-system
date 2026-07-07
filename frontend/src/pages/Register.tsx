import { useState, type FormEvent } from 'react';
import { useAuth } from '../lib/auth';
import { useNavigate, Link } from 'react-router-dom';

export default function Register() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [organizationName, setOrganizationName] = useState('');
  const [adminName, setAdminName] = useState('');
  const [adminEmail, setAdminEmail] = useState('');
  const [adminPassword, setAdminPassword] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setSuccess('');
    setLoading(true);
    try {
      await register(organizationName, adminName, adminEmail, adminPassword);
      setSuccess('Organização cadastrada com sucesso! Redirecionando para o login...');
      setTimeout(() => navigate('/login'), 2000);
    } catch (err: unknown) {
      const message =
        err instanceof Object &&
        (err as { response?: { data?: { message?: string } } }).response?.data
          ?.message;
      setError(message || 'Falha ao cadastrar. Tente novamente.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-950 px-4">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-white">Voucher System</h1>
          <p className="text-slate-400 mt-2">Cadastre sua organização</p>
        </div>

        <form
          onSubmit={handleSubmit}
          className="bg-slate-900 rounded-xl shadow-2xl border border-slate-800 p-8 space-y-5"
        >
          {error && (
            <div className="bg-red-900/50 border border-red-700 text-red-200 px-4 py-3 rounded-lg text-sm">
              {error}
            </div>
          )}
          {success && (
            <div className="bg-green-900/50 border border-green-700 text-green-200 px-4 py-3 rounded-lg text-sm">
              {success}
            </div>
          )}

          <div>
            <label className="block text-sm font-medium text-slate-300 mb-1.5">
              Nome da Organização
            </label>
            <input
              type="text"
              value={organizationName}
              onChange={(e) => setOrganizationName(e.target.value)}
              required
              className="w-full px-4 py-2.5 bg-slate-800 border border-slate-700 rounded-lg text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition"
              placeholder="Minha Empresa"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-300 mb-1.5">
              Nome do Administrador
            </label>
            <input
              type="text"
              value={adminName}
              onChange={(e) => setAdminName(e.target.value)}
              required
              className="w-full px-4 py-2.5 bg-slate-800 border border-slate-700 rounded-lg text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition"
              placeholder="João Silva"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-300 mb-1.5">
              Email do Administrador
            </label>
            <input
              type="email"
              value={adminEmail}
              onChange={(e) => setAdminEmail(e.target.value)}
              required
              className="w-full px-4 py-2.5 bg-slate-800 border border-slate-700 rounded-lg text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition"
              placeholder="joao@email.com"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-300 mb-1.5">
              Senha
            </label>
            <input
              type="password"
              value={adminPassword}
              onChange={(e) => setAdminPassword(e.target.value)}
              required
              className="w-full px-4 py-2.5 bg-slate-800 border border-slate-700 rounded-lg text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition"
              placeholder="••••••••"
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full py-2.5 px-4 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed text-white font-medium rounded-lg transition duration-200"
          >
            {loading ? 'Cadastrando...' : 'Criar Organização'}
          </button>

          <p className="text-center text-sm text-slate-400">
            Já tem uma conta?{' '}
            <Link
              to="/login"
              className="text-indigo-400 hover:text-indigo-300 font-medium"
            >
              Faça login
            </Link>
          </p>
        </form>
      </div>
    </div>
  );
}
