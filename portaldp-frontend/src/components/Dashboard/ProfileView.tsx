import React from 'react';
import { Mail, Phone, Calendar, User } from 'lucide-react';
import { Student } from '../../types';
import format from 'date-fns/format';
import { es } from 'date-fns/locale';

interface ProfileViewProps {
  student: Student;
}

const ProfileView: React.FC<ProfileViewProps> = ({ student }) => {
  return (
    <div className="space-y-6">
      <div className="card p-6">
        <h2 className="text-2xl font-bold text-gray-800 mb-6">Mi Perfil</h2>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {/* Información Personal */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold text-gray-700 border-b border-gray-200 pb-2">
              Información Personal
            </h3>
            
            <div className="space-y-3">
              <div className="flex items-center space-x-3">
                <User className="w-5 h-5 text-gray-400" />
                <div>
                  <p className="text-sm text-gray-500">Nombre completo</p>
                  <p className="font-medium text-gray-800">{student.name}</p>
                </div>
              </div>
              
              <div className="flex items-center space-x-3">
                <div className="w-5 h-5 flex items-center justify-center">
                  <span className="text-sm font-mono bg-gray-100 px-2 py-1 rounded">DNI</span>
                </div>
                <div>
                  <p className="text-sm text-gray-500">Documento de identidad</p>
                  <p className="font-medium text-gray-800">{student.dni}</p>
                </div>
              </div>
              
              {student.email && (
                <div className="flex items-center space-x-3">
                  <Mail className="w-5 h-5 text-gray-400" />
                  <div>
                    <p className="text-sm text-gray-500">Correo electrónico</p>
                    <p className="font-medium text-gray-800">{student.email}</p>
                  </div>
                </div>
              )}
              
              {student.phone && (
                <div className="flex items-center space-x-3">
                  <Phone className="w-5 h-5 text-gray-400" />
                  <div>
                    <p className="text-sm text-gray-500">Teléfono</p>
                    <p className="font-medium text-gray-800">{student.phone}</p>
                  </div>
                </div>
              )}
            </div>
          </div>
          
          {/* Información de la cuenta */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold text-gray-700 border-b border-gray-200 pb-2">
              Información de la Cuenta
            </h3>
            
            <div className="space-y-3">
              <div className="flex items-center space-x-3">
                <Calendar className="w-5 h-5 text-gray-400" />
                <div>
                  <p className="text-sm text-gray-500">Fecha de registro</p>
                  <p className="font-medium text-gray-800">
                    {format(new Date(student.createdAt), 'dd \'de\' MMMM \'de\' yyyy', { locale: es })}
                  </p>
                </div>
              </div>
              
              <div className="flex items-center space-x-3">
                <div className="w-5 h-5 flex items-center justify-center">
                  <div className={`w-3 h-3 rounded-full ${student.isActive ? 'bg-green-500' : 'bg-red-500'}`}></div>
                </div>
                <div>
                  <p className="text-sm text-gray-500">Estado de la cuenta</p>
                  <p className={`font-medium ${student.isActive ? 'text-green-600' : 'text-red-600'}`}>
                    {student.isActive ? 'Activa' : 'Inactiva'}
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
        
        {/* Horarios */}
        {student.schedules && student.schedules.length > 0 && (
          <div className="mt-8">
            <h3 className="text-lg font-semibold text-gray-700 border-b border-gray-200 pb-2 mb-4">
              Mis Horarios de Clase
            </h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              {student.schedules.map((schedule) => (
                <div key={schedule.id} className="bg-gradient-to-r from-academia-50 to-costura-50 p-4 rounded-lg border border-academia-200">
                  <h4 className="font-semibold text-academia-700">{schedule.dayName}</h4>
                  <p className="text-academia-600">{schedule.timeRange}</p>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default ProfileView;