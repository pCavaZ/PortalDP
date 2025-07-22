// src/components/Dashboard/CalendarView.tsx - CORREGIDO
import React, { useState, useEffect } from 'react';
import { ChevronLeft, ChevronRight, Clock, AlertCircle, CheckCircle2 } from 'lucide-react';
import { Student, CalendarDay } from '../../types';
import { format, startOfMonth, endOfMonth, eachDayOfInterval, getDay, addMonths, subMonths } from 'date-fns';
import { es } from 'date-fns/locale';

interface CalendarViewProps {
  student: Student;
}

const CalendarView: React.FC<CalendarViewProps> = ({ student }) => {
  const [currentDate, setCurrentDate] = useState(new Date());
  const [calendarData, setCalendarData] = useState<CalendarDay[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    loadCalendarData();
  }, [currentDate, student.id]);

  const loadCalendarData = async () => {
    setIsLoading(true);
    try {
      const year = currentDate.getFullYear();
      const month = currentDate.getMonth() + 1;
      
      // Por ahora usar datos mock hasta que implementemos el endpoint del calendario
      const mockData = generateMockCalendarData(year, month);
      setCalendarData(mockData);
    } catch (error) {
      console.error('Error loading calendar:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const generateMockCalendarData = (year: number, month: number): CalendarDay[] => {
    const startDate = startOfMonth(new Date(year, month - 1));
    const endDate = endOfMonth(startDate);
    const days = eachDayOfInterval({ start: startDate, end: endDate });
    
    return days.map(date => {
      const dayOfWeek = getDay(date);
      const dateStr = format(date, 'yyyy-MM-dd');
      
      // Simular clases basadas en los horarios del estudiante
      const hasRegularClass = student.schedules?.some(schedule => {
        const scheduleDayOfWeek = schedule.dayOfWeek === 7 ? 0 : schedule.dayOfWeek;
        return scheduleDayOfWeek === dayOfWeek;
      });

      const isWorkday = dayOfWeek >= 1 && dayOfWeek <= 5;
      const isPast = date < new Date();
      
      return {
        date: dateStr,
        classes: hasRegularClass ? [{
          id: 1,
          startTime: student.schedules?.[0]?.startTime || '10:00',
          endTime: student.schedules?.[0]?.endTime || '12:00',
          timeRange: student.schedules?.[0]?.timeRange || '10:00-12:00',
          isCancelled: false,
          canCancel: !isPast && Math.random() > 0.8 // 20% de clases se pueden cancelar (ejemplo)
        }] : [],
        recoveryClasses: [],
        cancellations: [],
        isAvailable: isWorkday && !hasRegularClass && !isPast,
        availableSlots: isWorkday && !hasRegularClass && !isPast ? [
          {
            startTime: '10:00',
            endTime: '12:00',
            timeRange: '10:00-12:00',
            availableSpots: Math.floor(Math.random() * 5) + 1,
            totalSpots: 10
          }
        ] : []
      };
    });
  };

  const handlePreviousMonth = () => {
    setCurrentDate(subMonths(currentDate, 1));
  };

  const handleNextMonth = () => {
    setCurrentDate(addMonths(currentDate, 1));
  };

  const getDayClassName = (day: CalendarDay, date: Date) => {
    const isPast = date < new Date();
    
    if (day.classes && day.classes.length > 0) {
      if (day.classes.some(c => c.isCancelled)) {
        return 'calendar-day calendar-day-cancelled';
      }
      return 'calendar-day calendar-day-my-class';
    }
    
    if (day.isAvailable && !isPast) {
      return 'calendar-day calendar-day-available';
    }
    
    if (date.getDay() === 0 || date.getDay() === 6) {
      return 'calendar-day bg-gray-50 text-gray-400';
    }
    
    return 'calendar-day bg-gray-50 text-gray-500';
  };

  const startDate = startOfMonth(currentDate);
  const endDate = endOfMonth(currentDate);
  const days = eachDayOfInterval({ start: startDate, end: endDate });
  
  // Calcular días para mostrar (incluyendo días del mes anterior para completar la semana)
  const firstDayOfWeek = getDay(startDate);
  const daysToShow = [];
  
  // Días del mes anterior
  for (let i = firstDayOfWeek - 1; i >= 0; i--) {
    const prevDate = new Date(startDate);
    prevDate.setDate(prevDate.getDate() - i - 1);
    daysToShow.push(prevDate);
  }
  
  // Días del mes actual
  daysToShow.push(...days);
  
  // Días del mes siguiente para completar la última semana
  const remainingDays = 42 - daysToShow.length; // 6 semanas * 7 días
  for (let i = 1; i <= remainingDays; i++) {
    const nextDate = new Date(endDate);
    nextDate.setDate(nextDate.getDate() + i);
    daysToShow.push(nextDate);
  }

  return (
    <div className="space-y-6">
      {/* Header del calendario */}
      <div className="card p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl font-bold text-gray-800">Mi Calendario</h2>
          
          <div className="flex items-center space-x-4">
            <button
              onClick={handlePreviousMonth}
              className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
            >
              <ChevronLeft className="w-5 h-5 text-gray-600" />
            </button>
            
            <h3 className="text-lg font-semibold text-gray-700 min-w-[200px] text-center">
              {format(currentDate, 'MMMM yyyy', { locale: es })}
            </h3>
            
            <button
              onClick={handleNextMonth}
              className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
            >
              <ChevronRight className="w-5 h-5 text-gray-600" />
            </button>
          </div>
        </div>

        {/* Leyenda */}
        <div className="flex flex-wrap gap-4 mb-6 text-sm">
          <div className="flex items-center space-x-2">
            <div className="w-4 h-4 rounded bg-gradient-to-br from-academia-500 to-academia-600"></div>
            <span>Mis clases</span>
          </div>
          <div className="flex items-center space-x-2">
            <div className="w-4 h-4 rounded bg-yellow-100 border border-yellow-300"></div>
            <span>Canceladas</span>
          </div>
          <div className="flex items-center space-x-2">
            <div className="w-4 h-4 rounded bg-green-50 border border-green-300"></div>
            <span>Disponible para recuperación</span>
          </div>
        </div>

        {/* Días de la semana */}
        <div className="grid grid-cols-7 gap-1 mb-2">
          {['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'].map((day) => (
            <div key={day} className="p-3 text-center text-sm font-semibold text-gray-600 bg-gray-50 rounded-lg">
              {day}
            </div>
          ))}
        </div>

        {/* Calendario */}
        {isLoading ? (
          <div className="flex items-center justify-center h-64">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-academia-600"></div>
          </div>
        ) : (
          <div className="grid grid-cols-7 gap-1">
            {daysToShow.map((date, index) => {
              const dayData = calendarData.find(d => d.date === format(date, 'yyyy-MM-dd'));
              const isCurrentMonth = date.getMonth() === currentDate.getMonth();
              
              return (
                <div
                  key={index}
                  className={`${
                    dayData && isCurrentMonth ? getDayClassName(dayData, date) : 'calendar-day bg-gray-50 text-gray-300'
                  } ${!isCurrentMonth ? 'opacity-30' : ''}`}
                >
                  <span className="text-sm font-medium">{date.getDate()}</span>
                  {/* LÍNEA CORREGIDA - verificar que dayData y classes existen */}
                  {dayData?.classes && dayData.classes.length > 0 && isCurrentMonth && (
                    <div className="mt-1">
                      <Clock className="w-3 h-3 mx-auto" />
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Próximas clases */}
      <div className="card p-6">
        <h3 className="text-lg font-semibold text-gray-800 mb-4">Próximas Clases</h3>
        
        {student.schedules && student.schedules.length > 0 ? (
          <div className="space-y-3">
            {student.schedules.map((schedule) => (
              <div key={schedule.id} className="flex items-center justify-between p-4 bg-gradient-to-r from-academia-50 to-costura-50 rounded-lg border border-academia-200">
                <div className="flex items-center space-x-3">
                  <Clock className="w-5 h-5 text-academia-600" />
                  <div>
                    <p className="font-medium text-academia-700">{schedule.dayName}</p>
                    <p className="text-sm text-academia-600">{schedule.timeRange}</p>
                  </div>
                </div>
                <CheckCircle2 className="w-5 h-5 text-green-500" />
              </div>
            ))}
          </div>
        ) : (
          <div className="text-center py-8 text-gray-500">
            <AlertCircle className="w-12 h-12 mx-auto mb-4 text-gray-400" />
            <p>No tienes horarios asignados</p>
            <p className="text-sm">Contacta con la administración para asignar tus clases</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default CalendarView;