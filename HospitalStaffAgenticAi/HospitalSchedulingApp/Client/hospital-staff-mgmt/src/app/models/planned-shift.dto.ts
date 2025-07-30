export interface PlannedShiftDto {
  plannedShiftId: number;
  shiftDate: string; // or Date if you parse it
  slotNumber: number;
  shiftTypeName: string;
  assignedStaffFullName: string;
  shiftDeparmentName: string;
}
