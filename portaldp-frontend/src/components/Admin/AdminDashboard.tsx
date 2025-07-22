import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Users, Calendar, Settings, LogOut, Scissors, Plus, Search } from 'lucide-react';
import authService from '../../services/auth';
import apiService from '../../services/api';
import { Student } from '../../types';
import toast from 'react-hot-toast';
import CreateStudentForm from './CreateStudent';

const AdminDashboard: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'students' | 'calendar' | 'settings'>('students');
  const [students, setStudents] = useState<Student[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    const user = authService.getCurrentUser();
    if (!user || !user.isAdmin) {
      navigate('/login');
      return;
    }
    
    loadStudents();
  }, [navigate]);

  const loadStudents = async () => {
    setIsLoading(true);
    try {
      console.log('Cargando estudiantes...');
      const response = await apiService.getTestStudents();
      console.log('Respuesta del servidor:', response);
      
      // El tipo TestStudentsResponse tiene la propiedad 'students'
      if (response.success && response.students) {
        setStudents(response.students);
        toast.success(`${response.students.length} estudiantes cargados`);
      } else {
        console.warn('Respuesta inesperada:', response);
        // Usar datos mock si la respuesta no es la esperada
        loadMockData();
      }
    } catch (error: any) {
      console.error('Error cargando estudiantes:', error);
      
      // Verificar si es un error de red o del servidor
      if (error.code === 'ECONNABORTED' || error.message.includes('Network Error')) {
        toast.error('Error de conexión con el servidor');
      } else if (error.response?.status === 401) {
        toast.error('No autorizado - redirigiendo al login');
        navigate('/login');
        return;
      } else {
        toast.error('Error al cargar estudiantes');
      }
      
      // Cargar datos mock como respaldo
      loadMockData();
    } finally {
      setIsLoading(false);
    }
  };

  const loadMockData = () => {
    const mockStudents: Student[] = [
      {
        id: 1,
        name: "María García López",
        dni: "12345678A",
        email: "maria.garcia@email.com",
        phone: "666123456",
        isActive: true,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      },
      {
        id: 2,
        name: "Ana López Martín",
        dni: "87654321B",
        email: "ana.lopez@email.com",
        phone: "666654321",
        isActive: true,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      },
      {
        id: 3,
        name: "Carmen Ruiz Sánchez",
        dni: "11223344C",
        email: "carmen.ruiz@email.com",
        phone: "666112233",
        isActive: true,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      },
      {
        id: 4,
        name: "Marta Sánchez Rodríguez",
        dni: "55667788D",
        email: "marta.sanchez@email.com",
        phone: "666556677",
        isActive: true,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      },
      {
        id: 5,
        name: "Rosa Martín",
        dni: "99887766E",
        email: "rosa.martin@email.com",
        phone: "666998877",
        isActive: true,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      }
    ];
    
    setStudents(mockStudents);
    console.log('Datos mock cargados:', mockStudents.length, 'estudiantes');
  };

  const handleCreateStudent = async (studentData: any) => {
    try {
      const token = localStorage.getItem('authToken');

      const response = await fetch('/api/students', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(studentData)
      });

      if (!response.ok) {
        throw new Error('Error al crear estudiante');
      }

      const result = await response.json();
      console.log('Estudiante creado:', result);
      
      // Aquí puedes refrescar la lista o mostrar un mensaje de éxito
      
    } catch (error) {
      console.error('Error:', error);
      throw error;
    }
  };

  const handleLogout = () => {
    authService.logout();
    navigate('/login');
  };

  const filteredStudents = students.filter(student =>
    student.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    student.dni.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const tabs = [
    { id: 'students', label: 'Estudiantes', icon: Users },
    { id: 'calendar', label: 'Calendario', icon: Calendar },
    { id: 'settings', label: 'Configuración', icon: Settings },
  ] as const;

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
              <h1 className="text-xl font-bold text-gray-800">Panel de Administración</h1>
            </div>

            {/* Admin Info */}
            <div className="flex items-center space-x-4">
              <div className="text-right">
                <p className="text-sm font-medium text-gray-800">Administrador</p>
                <p className="text-xs text-gray-500">Academia de Costura</p>
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
        {activeTab === 'students' && (
          <div className="space-y-6">
            {/* Students Header */}
            <div className="flex justify-between items-center">
              <div>
                <h2 className="text-2xl font-bold text-gray-800">Gestión de Estudiantes</h2>
                <p className="text-gray-600">
                  Administra los estudiantes de la academia ({students.length} total)
                </p>
              </div>
              <button 
                className="btn-primary flex items-center space-x-2"
                onClick={() => setIsCreateModalOpen(true)}
                //onClick={() => toast.success('Función próximamente disponible')}
              >
                <Plus className="w-5 h-5" />
                <span>Nuevo Estudiante</span>
              </button>
            </div>

            {/* Search */}
            <div className="card p-4">
              <div className="relative">
                  <CreateStudentForm
                    isOpen={isCreateModalOpen}
                    onClose={() => setIsCreateModalOpen(false)}
                    onSubmit={handleCreateStudent}
                  />
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-5 h-5" />
                <input
                  type="text"
                  placeholder="Buscar por nombre o DNI..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-academia-500 focus:border-transparent"
                />
              </div>
              {searchTerm && (
                <p className="text-sm text-gray-500 mt-2">
                  Mostrando {filteredStudents.length} de {students.length} estudiantes
                </p>
              )}
            </div>

            {/* Students List */}
            <div className="card">
              {isLoading ? (
                <div className="flex items-center justify-center p-8">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-academia-600 mr-3"></div>
                  <span>Cargando estudiantes...</span>
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Estudiante
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          DNI
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Contacto
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Estado
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Acciones
                        </th>
                      </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                      {filteredStudents.map((student) => (
                        <tr key={student.id} className="hover:bg-gray-50">
                          <td className="px-6 py-4 whitespace-nowrap">
                            <div className="flex items-center">
                              <div className="w-10 h-10 bg-gradient-to-br from-academia-500 to-costura-500 rounded-full flex items-center justify-center text-white font-semibold">
                                {student.name.charAt(0).toUpperCase()}
                              </div>
                              <div className="ml-4">
                                <div className="text-sm font-medium text-gray-900">{student.name}</div>
                              </div>
                            </div>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                            {student.dni}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                            <div>{student.email || 'Sin email'}</div>
                            <div>{student.phone || 'Sin teléfono'}</div>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                              student.isActive
                                ? 'bg-green-100 text-green-800'
                                : 'bg-red-100 text-red-800'
                            }`}>
                              {student.isActive ? 'Activo' : 'Inactivo'}
                            </span>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                            <button 
                              className="text-academia-600 hover:text-academia-900 mr-3"
                              onClick={() => toast.success('Función próximamente')}
                            >
                              Editar
                            </button>
                            <button 
                              className="text-red-600 hover:text-red-900"
                              onClick={() => toast.success('Función próximamente')}
                            >
                              Eliminar
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                  
                  {filteredStudents.length === 0 && !isLoading && (
                    <div className="text-center py-8 text-gray-500">
                      <Users className="w-12 h-12 mx-auto mb-4 text-gray-400" />
                      <p>
                        {searchTerm 
                          ? `No se encontraron estudiantes que coincidan con "${searchTerm}"`
                          : 'No hay estudiantes registrados'
                        }
                      </p>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        )}
        
        {activeTab === 'calendar' && (
          <div className="card p-8 text-center">
            <Calendar className="w-16 h-16 mx-auto mb-4 text-gray-400" />
            <h2 className="text-2xl font-bold text-gray-800 mb-4">Calendario General</h2>
            <p className="text-gray-600">Vista del calendario de toda la academia estará disponible próximamente.</p>
          </div>
        )}
        
        {activeTab === 'settings' && (
          <div className="card p-8 text-center">
            <Settings className="w-16 h-16 mx-auto mb-4 text-gray-400" />
            <h2 className="text-2xl font-bold text-gray-800 mb-4">Configuración del Sistema</h2>
            <p className="text-gray-600">Configuraciones generales estarán disponibles próximamente.</p>
          </div>
        )}
      </main>
    </div>
  );
};

export default AdminDashboard;