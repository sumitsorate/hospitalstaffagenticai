import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { FullCalendarComponent, FullCalendarModule } from '@fullcalendar/angular'; // standalone module
import { CalendarOptions } from '@fullcalendar/core/index.js';
import dayGridPlugin from '@fullcalendar/daygrid';
import interactionPlugin from '@fullcalendar/interaction';
import timeGridPlugin from '@fullcalendar/timegrid';
import { ShiftScheduleService } from '../services/shift-schedule.service';
import { PlannedShiftDto } from '../models/planned-shift.dto';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-shift-calendar',
  standalone: true,
  imports: [FullCalendarModule, CommonModule],
  templateUrl: './shift-calendar.component.html',
  styleUrl: './shift-calendar.component.css'
})
export class ShiftCalendarComponent implements OnInit {

  isLoading: boolean = false;
  constructor(private scheduleService: ShiftScheduleService, private cdRef: ChangeDetectorRef) { }
  @ViewChild('calendar') calendarComponent!: FullCalendarComponent;
  calendarOptions: CalendarOptions = {
    plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
    initialView: 'dayGridMonth',
    aspectRatio: 1.4, // Optional: makes cells taller
    headerToolbar: {
      left: 'prev,next today',
      center: 'title',
      right: 'dayGridMonth,timeGridWeek,listWeek'
    },
    eventOrder: 'extendedProps.shiftOrder',
    events: [], // Initially empty
    dateClick: this.handleDateClick.bind(this),
    eventClick: this.handleEventClick.bind(this),
eventContent: (arg) => {
  const { staffName, departmentName, shiftType, isVacant } = arg.event.extendedProps;

  const container = document.createElement('div');
  container.classList.add('shift-card', isVacant ? 'vacant-shift' : 'assigned-shift');

  const emoji = isVacant ? '🟡' : '👩‍⚕️';
  const nameDiv = document.createElement('div');
  nameDiv.classList.add('staff-name');
  nameDiv.innerText = `${emoji} ${staffName}`;

  const detailDiv = document.createElement('div');
  detailDiv.classList.add('shift-details');
  detailDiv.innerText = `${departmentName} · ${shiftType}`;

  container.appendChild(nameDiv);
  container.appendChild(detailDiv);

  return { domNodes: [container] };
}



    // eventContent: (arg) => {
    //   const { staffName, departmentName, shiftType, isVacant } = arg.event.extendedProps;

    //   const container = document.createElement('div');
    //   container.classList.add('shift-card', isVacant ? 'vacant-shift' : 'assigned-shift');

    //   const emoji = isVacant ? '🟡' : '👩‍⚕️';
    //   const nameDiv = document.createElement('div');
    //   nameDiv.classList.add('staff-name');
    //   nameDiv.innerText = `${emoji} ${staffName}`;

    //   const detailDiv = document.createElement('div');
    //   detailDiv.classList.add('shift-details');
    //   detailDiv.innerText = `${departmentName} · ${shiftType}`;

    //   container.appendChild(nameDiv);
    //   container.appendChild(detailDiv);

    //   return { domNodes: [container] };
    // }

    // 👉 Add this here
    // eventDidMount: (info) => {
    //   console.log("info", info)
    //   const { staffName, departmentName, shiftType } = info.event.extendedProps;

    //   const content = `
    //   <div class="shift-title">
    //     <div class="staff-name">${staffName}</div>
    //     <div class="details">(${departmentName} - ${shiftType})</div>
    //   </div>
    // `;

    //   const titleEl = info.el.querySelector('.fc-event-title');
    //   if (titleEl) {
    //     titleEl.innerHTML = content;
    //   }
    // }
  };

  refreshCalendar() {
    this.loadCalendar();
  }

  loadCalendar() {
    this.isLoading = true; // 🔄 Start spinner
    const startDate = '2025-07-01';
    const endDate = '2025-07-31';

    this.scheduleService.fetchShiftInformation().subscribe({
      next: (shifts: PlannedShiftDto[]) => {
        const events = this.transformShiftsToEvents(shifts);
        console.log('Fetched shifts:', events);

        // Sort events by start date first, then by shiftType: morning, evening, night
        const shiftOrder: { [key: string]: number } = { morning: 1, evening: 2, night: 3 };
        const sortedEvents = events.slice().sort((a, b) => {
          const aDate = new Date(a.start).getTime();
          const bDate = new Date(b.start).getTime();
          if (aDate !== bDate) {
            return aDate - bDate;
          }
          const aType: string = (a.extendedProps.shiftType || '').toLowerCase();
          const bType: string = (b.extendedProps.shiftType || '').toLowerCase();
          return (shiftOrder[aType as keyof typeof shiftOrder] || 99) - (shiftOrder[bType as keyof typeof shiftOrder] || 99);
        });
 
        this.calendarOptions = {
          ...this.calendarOptions,
          events: sortedEvents
        };
        // this.calendarOptions = {
        //   ...this.calendarOptions,
        //   events: events
        // };
        this.cdRef.detectChanges();
      },
      error: err => {
        console.error('Failed to fetch shifts:', err);
      },
      complete: () => {
        this.isLoading = false; // ✅ Stop spinner after both success and error
        this.cdRef.detectChanges();
      }
    });
  }




  ngOnInit(): void {
    this.loadCalendar();
  }

  handleDateClick(arg: any): void {
    alert('Date clicked: ' + arg.dateStr);
  }

  handleEventClick(arg: any): void {
    alert('Shift: ' + arg.event.title);
  }

  transformShiftsToEvents(shifts: PlannedShiftDto[]): any[] {
    const shiftOrder: { [key: string]: number } = { morning: 1, evening: 2, night: 3 };
    return shifts.map(shift => {
      const isVacant = shift.shiftStatusId === 5;
      if (isVacant) {
        shift.assignedStaffFullName = 'Vacant';
      }
      const shiftOrder: { [key: string]: number } = { morning: 1, evening: 2, night: 3 };
      return {
        title: isVacant
          ? `🟡 Vacant (${shift.shiftTypeName})`
          : `${shift.assignedStaffFullName} (${shift.shiftTypeName})`,
        start: shift.shiftDate,
        end: shift.shiftDate,
        allDay: true,
        extendedProps: {
          staffName: shift.assignedStaffFullName,
          departmentName: shift.shiftDeparmentName,
          shiftType: shift.shiftTypeName,
          isVacant: isVacant,
          shiftOrder: shiftOrder[(shift.shiftTypeName || '').toLowerCase()] || 99
        },
        backgroundColor: isVacant
          ? '#fff3cd'
          : this.getShiftColor(shift.shiftTypeName),
        borderColor: isVacant
          ? '#ffc107'
          : this.getBorderColor(shift.shiftTypeName),
        textColor: isVacant ? '#856404' : undefined
      };
    });
  }


  // transformShiftsToEvents(shifts: any[]): any[] {
  //   return shifts.map(shift => {
  //     const isVacant = shift.staffName === 'Vacant';

  //     return {
  //       title: isVacant
  //         ? `🟡 Vacant (${shift.shiftType})`
  //         : `${shift.staffName} (${shift.shiftType})`,
  //       start: shift.shiftDate,
  //       end: shift.shiftDate, // Optional: calculate based on shift type duration
  //       allDay: true,
  //       extendedProps: {
  //         staffName: shift.staffName,
  //         departmentName: shift.departmentName,
  //         shiftType: shift.shiftType,
  //         role: shift.role,
  //         isVacant: isVacant
  //       },
  //       backgroundColor: isVacant
  //         ? '#fff3cd' // light yellow
  //         : this.getShiftColor(shift.shiftType),
  //       borderColor: isVacant
  //         ? '#ffc107' // yellow border
  //         : this.getBorderColor(shift.shiftType),
  //       textColor: isVacant ? '#856404' : undefined
  //     };
  //   });
  // }


  getShiftColor(shiftType: string): string {
    switch (shiftType.toLowerCase()) {
      case 'morning': return '#4caf50';
      case 'evening': return '#ff9800';
      case 'night': return '#2196f3';
      default: return '#9e9e9e';
    }
  }

  getBorderColor(shiftType: string): string {
    switch (shiftType.toLowerCase()) {
      case 'morning': return '#388e3c';
      case 'evening': return '#f57c00';
      case 'night': return '#1976d2';
      default: return '#616161';
    }
  }
}
