import React, { useState, ReactNode } from 'react';
import { X } from 'lucide-react';
import {Schedule, CreateStudent, Student} from '../../types'; 
import apiService from '../../services/api';

// Interfaces

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  children: ReactNode;
}

interface EditStudentFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (data: CreateStudent) => Promise<void>;
}

// Componente Modal interno
const Modal: React.FC<ModalProps> = ({ isOpen, onClose, children }) => {
  if (!isOpen) return null;

  const handleBackdropClick = (e: React.MouseEvent) => {
    if (e.target === e.currentTarget) {
      onClose();
    }
  };

  return (
    <div 
      className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 backdrop-blur-sm"
      onClick={handleBackdropClick}
    >
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full mx-4 max-h-[90vh] overflow-y-auto">
        {children}
      </div>
    </div>
  );
};

// Edit Student
const CreateStudentForm: React.FC<EditStudentFormProps> = ({ isOpen, onClose, onSubmit }) => {
  const [formData, setFormData] = useState<CreateStudent>({
    name: '',
    dni: '',
    email: '',
    phone: '',
    //isActive: true,
   //createdAt: '10:00:00',
    //updatedAt: '12:00:00'
  });

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

//   const handleScheduleChange = (index: number, field: keyof Schedule, value: string | number) => {
//     setFormData(prev => ({
//       ...prev,
//       schedules: prev.schedules.map((schedule, i) => 
//         i === index ? { ...schedule, [field]: value } : schedule
//       )
//     }));
//   };

//   const addSchedule = () => {
//     setFormData(prev => ({
//       ...prev,
//       schedules: [...prev.schedules, { dayOfWeek: 1, startTime: '09:00', endTime: '10:00' }]
//     }));
//   };

//   const removeSchedule = (index: number) => {
//     setFormData(prev => ({
//       ...prev,
//       schedules: prev.schedules.filter((_, i) => i !== index)
//     }));
//   };

  const validateForm = (): boolean => {
    const newErrors: string[] = [];

    if (!formData.name.trim()) newErrors.push('El nombre es requerido');
    if (!formData.dni.trim()) newErrors.push('El DNI es requerido');
    if (!formData.email.trim()) newErrors.push('El email es requerido');
    if (!formData.phone.trim()) newErrors.push('El teléfono es requerido');
    
    // Validar formato de email
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (formData.email && !emailRegex.test(formData.email)) {
      newErrors.push('El formato del email no es válido');
    }

    // Validar DNI (formato español básico)
    const dniRegex = /^\d{8}[A-Za-z]$/;
    if (formData.dni && !dniRegex.test(formData.dni)) {
      newErrors.push('El formato del DNI no es válido (8 números + letra)');
    }

    // Validar Telefono (formato español básico)
    const phoneRegex = /^\d{9}$/;
    if (formData.phone && !phoneRegex.test(formData.phone)) {
      newErrors.push('El formato del telefono no es válido (9 números)');
    }

    // // Validar horarios
    // formData.schedules.forEach((schedule, index) => {
    //   if (schedule.startTime >= schedule.endTime) {
    //     newErrors.push(`El horario ${index + 1}: la hora de inicio debe ser menor que la de fin`);
    //   }
    // });

    setErrors(newErrors);
    return newErrors.length === 0;
  };

  const handleSubmit = async () => {
    if (!validateForm()) return;

    setIsSubmitting(true);
    setErrors([]);

    try {
      const studentData = {
        ...formData,
        dni: formData.dni.toUpperCase(), // Normalizar DNI
        // schedules: formData.schedules.map(schedule => ({
        //   ...schedule,
        //   startTime: schedule.startTime + ':00',
        //   endTime: schedule.endTime + ':00'
        // }))
      };

      //const response = await apiService.createStudent(studentData);
      await onSubmit(studentData);
      
      // Resetear formulario al éxito
      setFormData({
        name: '',
        dni: '',
        email: '',
        phone: '',
        //isActive: true,
        //createdAt: '10:00:00',
        //updatedAt: '12:00:00'
        //schedules: [{ dayOfWeek: 1, startTime: '09:00', endTime: '10:00' }]
      });
      
      onClose();
    } catch (error) {
      console.error('Error al crear estudiante:', error);
      setErrors(['Error al crear el estudiante. Inténtalo de nuevo.']);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    // Resetear errores al cerrar
    setErrors([]);
    onClose();
  };

  const daysOfWeek = [
    { value: 1, label: 'Lunes' },
    { value: 2, label: 'Martes' },
    { value: 3, label: 'Miércoles' },
    { value: 4, label: 'Jueves' },
    { value: 5, label: 'Viernes' },
    { value: 6, label: 'Sábado' },
    { value: 7, label: 'Domingo' }
  ];

  return (
    <Modal isOpen={isOpen} onClose={handleClose}>
      <div className="p-6">
        {/* Header */}
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-2xl font-bold text-gray-800">Crear Nuevo Estudiante</h2>
          <button
            onClick={handleClose}
            className="p-2 hover:bg-gray-100 rounded-full transition-colors"
          >
            <X className="w-5 h-5 text-gray-500" />
          </button>
        </div>

        {/* Errores */}
        {errors.length > 0 && (
          <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-md">
            <ul className="text-sm text-red-600">
              {errors.map((error, index) => (
                <li key={index}>• {error}</li>
              ))}
            </ul>
          </div>
        )}

        {/* Formulario */}
        <div className="space-y-4">
          {/* Información básica */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Nombre *
              </label>
              <input
                type="text"
                name="name"
                value={formData.name}
                onChange={handleInputChange}
                placeholder="Ej: Juan Pérez"
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                DNI *
              </label>
              <input
                type="text"
                name="dni"
                value={formData.dni}
                onChange={handleInputChange}
                placeholder="Ej: 12345678A"
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                required
              />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Email *
            </label>
            <input
              type="email"
              name="email"
              value={formData.email}
              onChange={handleInputChange}
              placeholder="Ej: juan.perez@email.com"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Teléfono
            </label>
            <input
              type="tel"
              name="phone"
              value={formData.phone}
              onChange={handleInputChange}
              placeholder="Ej: 654321987"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
          </div>

          {/* Horarios */}
          <div>
            <div className="flex justify-between items-center mb-3">
              <label className="block text-sm font-medium text-gray-700">
                Horarios de Clase
              </label>
              <button
                type="button"
                //onClick={addSchedule}
                className="px-3 py-1 text-sm bg-blue-500 text-white rounded hover:bg-blue-600 transition-colors"
              >
                + Agregar Horario
              </button>
            </div>

            {/* {formData.schedules.map((schedule, index) => (
              <div key={index} className="flex gap-2 mb-2 p-3 border border-gray-200 rounded-lg bg-gray-50">
                <select
                  value={schedule.dayOfWeek}
                  onChange={(e) => handleScheduleChange(index, 'dayOfWeek', parseInt(e.target.value))}
                  className="flex-1 px-2 py-1 border border-gray-300 rounded text-sm focus:outline-none focus:ring-1 focus:ring-blue-500"
                >
                  {daysOfWeek.map(day => (
                    <option key={day.value} value={day.value}>{day.label}</option>
                  ))}
                </select>

                <div className="flex items-center gap-2">
                  <input
                    type="time"
                    value={schedule.startTime}
                    onChange={(e) => handleScheduleChange(index, 'startTime', e.target.value)}
                    className="px-2 py-1 border border-gray-300 rounded text-sm focus:outline-none focus:ring-1 focus:ring-blue-500"
                  />
                  <span className="text-gray-500 text-sm">-</span>
                  <input
                    type="time"
                    value={schedule.endTime}
                    onChange={(e) => handleScheduleChange(index, 'endTime', e.target.value)}
                    className="px-2 py-1 border border-gray-300 rounded text-sm focus:outline-none focus:ring-1 focus:ring-blue-500"
                  />
                </div>

                {formData.schedules.length > 1 && (
                  <button
                    type="button"
                    onClick={() => removeSchedule(index)}
                    className="px-2 py-1 text-red-500 hover:bg-red-100 rounded transition-colors"
                    title="Eliminar horario"
                  >
                    ✕
                  </button>
                )}
              </div>
            ))} */}
          </div>

          {/* Botones */}
          <div className="flex gap-3 pt-4 border-t border-gray-200">
            <button
              type="button"
              onClick={handleClose}
              disabled={isSubmitting}
              className="flex-1 px-4 py-2 text-gray-600 border border-gray-300 rounded-md hover:bg-gray-50 transition-colors disabled:opacity-50"
            >
              Cancelar
            </button>
            <button
              type="button"
              onClick={handleSubmit}
              disabled={isSubmitting || !formData.name || !formData.dni || !formData.email}
              className="flex-1 px-4 py-2 bg-blue-500 text-white rounded-md hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {isSubmitting ? 'Creando...' : 'Crear Estudiante'}
            </button>
          </div>
        </div>
      </div>
    </Modal>
  );
};

export default CreateStudentForm;