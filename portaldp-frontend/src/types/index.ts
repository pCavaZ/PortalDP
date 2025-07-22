export interface Student {
  id: number;
  name: string;
  dni: string;
  email?: string;
  phone?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  schedules?: Schedule[];
}

export interface CreateStudent {
  name: string;
  dni: string;
  email: string;
  phone: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface Schedule {
  id: number;
  studentId: number;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  isActive: boolean;
  dayName: string;
  timeRange: string;
}

export interface LoginRequest {
  dni: string;
}

export interface LoginResponse {
  success: boolean;
  message: string;
  token: string;
  isAdmin: boolean;
  expiresAt: string;
  student?: Student;
  data: Data;
}

export interface Data {
  token: string;
  refreshToken: string;
  student: Student;
  isAdmin: boolean;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

// NUEVO: Tipo específico para la respuesta de test/students
export interface TestStudentsResponse {
  success: boolean;
  count: number;
  students: Student[];
}

export interface CalendarDay {
  date: string;
  classes: ClassInfo[];
  recoveryClasses: RecoveryClass[];
  cancellations: ClassCancellation[];
  isAvailable: boolean;
  availableSlots: AvailableSlot[];
}

export interface ClassInfo {
  id: number;
  startTime: string;
  endTime: string;
  timeRange: string;
  isCancelled: boolean;
  canCancel: boolean;
}

export interface RecoveryClass {
  id: number;
  studentId: number;
  classDate: string;
  startTime: string;
  endTime: string;
  timeRange: string;
}

export interface ClassCancellation {
  id: number;
  studentId: number;
  classDate: string;
  originalScheduleId: number;
  cancelledAt: string;
  reason?: string;
}

export interface AvailableSlot {
  startTime: string;
  endTime: string;
  timeRange: string;
  availableSpots: number;
  totalSpots: number;
}

export interface User {
  isAuthenticated: boolean;
  isAdmin: boolean;
  student?: Student;
  token?: string;
}