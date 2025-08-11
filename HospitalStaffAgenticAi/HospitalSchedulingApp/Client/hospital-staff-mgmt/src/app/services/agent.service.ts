import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { ChatResponse } from '../models/chat-response.model';

@Injectable({ providedIn: 'root' })
export class AgentService {
  constructor(private http: HttpClient) { }

  askAgent(userMessage: string): Observable<ChatResponse> {
    const threadId = localStorage.getItem('threadId');
    const body = {
      message: userMessage,
      threadId:'thread_ulXj2b0COMmqvLkayb1D4TKX'
      //threadId: threadId
    }; // Matches the C# model
    return this.http.post<ChatResponse>('http://localhost:5029/api/AgentChat/ask', body);

  }

  refresh( ): Observable<any> {
    return this.http.post<any>('http://localhost:5029/api/AgentChat/refresh', {});
  }
}