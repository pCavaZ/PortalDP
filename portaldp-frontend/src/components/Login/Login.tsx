import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Scissors, User, Shield } from 'lucide-react';
import authService from '../../services/auth';

const Login: React.FC = () => {
  const [dni, setDni] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!dni.trim()) {
      return;
    }

    setIsLoading(true);
    
    try {
      const user = await authService.login(dni.trim().toUpperCase());
      
      if (user) {
        if (user.isAdmin) {
          navigate('/admin');
        } else {
          navigate('/dashboard');
        }
      }
    } catch (error) {
      console.error('Login error:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleQuickLogin = (quickDni: string) => {
    setDni(quickDni);
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-academia-50 via-white to-costura-50 flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        {/* Header */}
        <div className="text-center mb-8 animate-fade-in">
          <div className="mx-auto w-20 h-20 bg-gradient-to-br from-academia-500 to-costura-500 rounded-full flex items-center justify-center mb-4 shadow-lg">
            <Scissors className="w-10 h-10 text-white" />
          </div>
          <h1 className="text-3xl font-bold text-gray-800 mb-2">
            Academia de Costura
          </h1>
          <p className="text-gray-600">
            Accede a tu portal de clases
          </p>
        </div>

        {/* Login Form */}
        <div className="card p-8 animate-slide-in">
          <form onSubmit={handleSubmit} className="space-y-6">
            <div>
              <label htmlFor="dni" className="block text-sm font-medium text-gray-700 mb-2">
                DNI
              </label>
              <input
                type="text"
                id="dni"
                value={dni}
                onChange={(e) => setDni(e.target.value.toUpperCase())}
                className="input-field"
                placeholder="Introduce tu DNI"
                maxLength={9}
                required
                disabled={isLoading}
              />
              <p className="text-xs text-gray-500 mt-1">
                Introduce tu DNI o "ADMIN" para acceso de administrador
              </p>
            </div>

            <button
              type="submit"
              disabled={isLoading || !dni.trim()}
              className="btn-primary w-full disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isLoading ? (
                <div className="flex items-center justify-center">
                  <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-white mr-2"></div>
                  Accediendo...
                </div>
              ) : (
                'Iniciar Sesión'
              )}
            </button>
          </form>

          {/* Quick Access */}
          <div className="mt-8 pt-6 border-t border-gray-200">
            <p className="text-sm text-gray-600 mb-4 text-center">
              Acceso rápido para pruebas:
            </p>
            
            <div className="grid grid-cols-2 gap-3">
              <button
                onClick={() => handleQuickLogin('ADMIN')}
                className="flex items-center justify-center p-3 border border-academia-200 rounded-lg hover:bg-academia-50 transition-colors"
                disabled={isLoading}
              >
                <Shield className="w-4 h-4 text-academia-600 mr-2" />
                <span className="text-sm font-medium text-academia-600">Admin</span>
              </button>
              
              <button
                onClick={() => handleQuickLogin('12345678A')}
                className="flex items-center justify-center p-3 border border-costura-200 rounded-lg hover:bg-costura-50 transition-colors"
                disabled={isLoading}
              >
                <User className="w-4 h-4 text-costura-600 mr-2" />
                <span className="text-sm font-medium text-costura-600">María</span>
              </button>
            </div>
            
            <div className="grid grid-cols-3 gap-2 mt-3">
              {['87654321B', '11223344C', '55667788D'].map((testDni) => (
                <button
                  key={testDni}
                  onClick={() => handleQuickLogin(testDni)}
                  className="p-2 text-xs border border-gray-200 rounded hover:bg-gray-50 transition-colors"
                  disabled={isLoading}
                >
                  {testDni}
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="text-center mt-6 text-sm text-gray-500">
          <p>Sistema de gestión de clases v1.0</p>
        </div>
      </div>
    </div>
  );
};

export default Login;