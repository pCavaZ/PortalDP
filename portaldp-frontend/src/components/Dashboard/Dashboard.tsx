import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Calendar, User, Settings, LogOut, Scissors } from 'lucide-react';
import authService from '../../services/auth';
import apiService from '../../services/api';
import { Student } from '../../types';
import CalendarView from './CalendarView'; // SIN .tsx
import ProfileView from './ProfileView';   // SIN .tsx

const Dashboard: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'calendar' | 'profile' | 'settings'>('calendar');
  const [student, setStudent] = useState<Student | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    loadStudentData();
  }, []);

  const loadStudentData = async () => {
    try {
      const user = authService.getCurrentUser();
      if (!user || !user.isAuthenticated) {
        navigate('/login');
        return;
      }

      if (user.isAdmin) {
        navigate('/admin');
        return;
      }

      if (user.student) {
        setStudent(user.student);
      } else {
        // Cargar datos del estudiante actual
        const response = await apiService.getCurrentStudent();
        if (response.success && response.data) {
          setStudent(response.data);
        }
      }
    } catch (error) {
      console.error('Error loading student data:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleLogout = () => {
    authService.logout();
    navigate('/login');
  };

  const tabs = [
    { id: 'calendar', label: 'Mi Calendario', icon: Calendar },
    { id: 'profile', label: 'Mi Perfil', icon: User },
    { id: 'settings', label: 'Configuración', icon: Settings },
  ] as const;

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="flex items-center space-x-3">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-academia-600"></div>
          <span className="text-gray-600">Cargando...</span>
        </div>
      </div>
    );
  }

  if (!student) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <p className="text-gray-600 mb-4">No se pudieron cargar los datos del estudiante</p>
          <button onClick={() => navigate('/login')} className="btn-primary">
            Volver al Login
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow-sm border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            {/* Logo */}
            <div className="flex items-center">
              <div className="w-8 h-8 bg-gradient-to-br from-academia-500 to-costura-500 rounded-lg flex items-center justify-center mr-3">
                <Scissors className="w-5 h-5 text-white" />
              </div>
              <h1 className="text-xl font-bold text-gray-800">Academia de Costura</h1>
            </div>

            {/* User Info */}
            <div className="flex items-center space-x-4">
              <div className="text-right">
                <p className="text-sm font-medium text-gray-800">{student.name}</p>
                <p className="text-xs text-gray-500">{student.dni}</p>
              </div>
              <button
                onClick={handleLogout}
                className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition-colors"
                title="Cerrar sesión"
              >
                <LogOut className="w-5 h-5" />
              </button>
            </div>
          </div>
        </div>
      </header>

      {/* Navigation Tabs */}
      <nav className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex space-x-8">
            {tabs.map((tab) => {
              const Icon = tab.icon;
              return (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`flex items-center space-x-2 py-4 px-1 border-b-2 font-medium text-sm transition-colors ${
                    activeTab === tab.id
                      ? 'border-academia-500 text-academia-600'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                  }`}
                >
                  <Icon className="w-5 h-5" />
                  <span>{tab.label}</span>
                </button>
              );
            })}
          </div>
        </div>
      </nav>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {activeTab === 'calendar' && <CalendarView student={student} />}
        {activeTab === 'profile' && <ProfileView student={student} />}
        {activeTab === 'settings' && (
          <div className="card p-8 text-center">
            <h2 className="text-2xl font-bold text-gray-800 mb-4">Configuración</h2>
            <p className="text-gray-600">Esta sección estará disponible próximamente.</p>
          </div>
        )}
      </main>
    </div>
  );
};

export default Dashboard;