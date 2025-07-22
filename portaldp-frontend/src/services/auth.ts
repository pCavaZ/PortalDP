import { User, LoginResponse } from '../types';
import apiService from './api';
import toast from 'react-hot-toast';

class AuthService {
  private readonly TOKEN_KEY = 'token';
  private readonly USER_KEY = 'user';

  async login(dni: string): Promise<User | null> {
    try {
      const response = await apiService.login(dni);
      
      if (response.success) {
        // Guardar token y datos del usuario
        localStorage.setItem(this.TOKEN_KEY, response.data.token);
        
        const user: User = {
          isAuthenticated: true,
          isAdmin: response.data.isAdmin,
          student: response.data.student,
          token: response.data.token,
        };
        
        localStorage.setItem(this.USER_KEY, JSON.stringify(user));
        
        toast.success(response.message);
        return user;
      } else {
        toast.error(response.message);
        return null;
      }
    } catch (error: any) {
      const message = error.response?.data?.message || 'Error de conexión';
      toast.error(message);
      return null;
    }
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    toast.success('Sesión cerrada correctamente');
  }

  getCurrentUser(): User | null {
    try {
      const userStr = localStorage.getItem(this.USER_KEY);
      if (userStr) {
        return JSON.parse(userStr);
      }
      return null;
    } catch {
      return null;
    }
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  isAuthenticated(): boolean {
    const user = this.getCurrentUser();
    const token = this.getToken();
    return !!(user?.isAuthenticated && token);
  }

  isAdmin(): boolean {
    const user = this.getCurrentUser();
    return user?.isAdmin || false;
  }
}

export const authService = new AuthService();
export default authService;