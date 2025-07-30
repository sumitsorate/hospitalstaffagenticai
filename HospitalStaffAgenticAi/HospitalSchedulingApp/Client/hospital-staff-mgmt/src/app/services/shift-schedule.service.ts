import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { ChatResponse } from '../models/chat-response.model';
import { ShiftScheduleResponse } from '../models/shift-schedule-response.model';
import { PlannedShiftDto } from '../models/planned-shift.dto';

@Injectable({ providedIn: 'root' })
export class ShiftScheduleService {
  constructor(private http: HttpClient) {}

  fetchShiftInformation(): Observable<PlannedShiftDto[]> {    
     return this.http
     .get<PlannedShiftDto[]>('http://localhost:5029/api/PlannedShift/fetch?startDate=2025-08-01&endDate=2025-08-31' );

  }
}