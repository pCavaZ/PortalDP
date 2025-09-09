import axios, { AxiosInstance, AxiosResponse } from 'axios';
import toast from 'react-hot-toast';
import { 
  Student, 
  CreateStudent,
  LoginResponse, 
  ApiResponse, 
  CalendarDay, 
  AvailableSlot,
  TestStudentsResponse  // NUEVO TIPO
} from '../types';

class ApiService {
  private client: AxiosInstance;
  private baseURL: string;

  constructor() {
    this.baseURL = process.env.REACT_APP_API_URL || 'https://localhost:53513';
    
    this.client = axios.create({
      baseURL: this.baseURL,
      timeout: 10000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.setupInterceptors();
  }

  private setupInterceptors() {
    // Request interceptor - agregar token
    this.client.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem('token');
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => {
        return Promise.reject(error);
      }
    );

    // Response interceptor - manejar errores
    this.client.interceptors.response.use(
      (response: AxiosResponse) => {
        return response;
      },
      (error) => {
        if (error.response?.status === 401) {
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          window.location.href = '/login';
          toast.error('Sesión expirada. Por favor, inicia sesión nuevamente.');
        } else if (error.response?.status >= 500) {
          toast.error('Error del servidor. Inténtalo más tarde.');
        } else if (error.code === 'ECONNABORTED') {
          toast.error('Tiempo de espera agotado. Verifica tu conexión.');
        }
        return Promise.reject(error);
      }
    );
  }

  // Métodos helper
  private async handleRequest<T>(request: Promise<AxiosResponse<T>>): Promise<T> {
    try {
      const response = await request;
      return response.data;
    } catch (error: any) {
      console.error('API Error:', error);
      throw error;
    }
  }

  // Auth endpoints
  async login(dni: string): Promise<LoginResponse> {
    return this.handleRequest(
      this.client.post<LoginResponse>('/api/auth/login', { dni })
    );
  }

  async validateDni(dni: string): Promise<ApiResponse<boolean>> {
    return this.handleRequest(
      this.client.post<ApiResponse<boolean>>('/api/auth/validate-dni', { dni })
    );
  }

  // Test endpoints
  async testConnection(): Promise<any> {
    return this.handleRequest(
      this.client.get('/api/test')
    );
  }

  async testDatabase(): Promise<any> {
    return this.handleRequest(
      this.client.get('/api/test/database')
    );
  }

  // CORREGIDO: Usar el tipo específico para test/students
  async getTestStudents(): Promise<TestStudentsResponse> {
    return this.handleRequest(
      this.client.get<TestStudentsResponse>('/api/test/students')
    );
  }

  // Students endpoints
  async getAllStudents(): Promise<ApiResponse<Student[]>> {
    return this.handleRequest(
      this.client.get<ApiResponse<Student[]>>('/api/students')
    );
  }

  async getStudent(id: number): Promise<ApiResponse<Student>> {
    return this.handleRequest(
      this.client.get<ApiResponse<Student>>(`/api/students/${id}`)
    );
  }

  async createStudent(createStudentDto: CreateStudent): Promise<ApiResponse<CreateStudent>> {
    return this.handleRequest(
      this.client.post<ApiResponse<CreateStudent>>('/api/students', createStudentDto )
    )
  }

  async deleteStudent(id: number): Promise<ApiResponse<Boolean>> {
    return this.handleRequest(
      this.client.delete<ApiResponse<Boolean>>(`/api/students/${id}`)
    )
  }

  async getCurrentStudent(): Promise<ApiResponse<Student>> {
    return this.handleRequest(
      this.client.get<ApiResponse<Student>>('/api/students/me')
    );
  }

  async whoAmI(): Promise<any> {
    return this.handleRequest(
      this.client.get('/api/students/whoami')
    );
  }

  // Calendar endpoints (cuando los implementemos en el backend)
  async getStudentCalendar(studentId: number, year: number, month: number): Promise<ApiResponse<CalendarDay[]>> {
    return this.handleRequest(
      this.client.get<ApiResponse<CalendarDay[]>>(`/api/calendar/student/${studentId}/${year}/${month}`)
    );
  }

  async getMyCalendar(year: number, month: number): Promise<ApiResponse<CalendarDay[]>> {
    return this.handleRequest(
      this.client.get<ApiResponse<CalendarDay[]>>(`/api/calendar/my-calendar/${year}/${month}`)
    );
  }

  async getAvailableSlots(date: string): Promise<ApiResponse<AvailableSlot[]>> {
    return this.handleRequest(
      this.client.get<ApiResponse<AvailableSlot[]>>(`/api/calendar/available-slots/${date}`)
    );
  }
}

export const apiService = new ApiService();
export default apiService;