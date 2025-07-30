import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { LoginResponseDto } from '../models/login-response.dto';

@Injectable({
  providedIn: 'root',
})
export class AuthService {

  baseUrl = import.meta.env.VITE_API_BASE_URL;


  private apiUrl = 'http://localhost:5029/api/Auth';

  constructor(private http: HttpClient) { }

  login(data: { username: string; password: string }): Observable<LoginResponseDto> {
    
    console.log(this.baseUrl);
    var response = this.http.post<LoginResponseDto>(`${this.apiUrl}/login`, data);

    return response;
  }

  logout(): Observable<any> {
    const threadId = localStorage.getItem('threadId');
    const token = localStorage.getItem('token');

    if (!threadId || !token) {
      console.warn('Missing threadId or token');
      return of(null); // prevent error if called without data
    }

    return this.http.post(`${this.apiUrl}/logout/${encodeURIComponent(threadId)}`, null, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }
}
