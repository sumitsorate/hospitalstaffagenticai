using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dtos.Shift.Requests;
using HospitalSchedulingApp.Dtos.Shift.Response;

namespace HospitalSchedulingApp.Services.Interfaces
{
    /// <summary>
    /// Service contract for managing planned shifts.
    /// Provides operations for fetching, assigning, unassigning, and creating planned shifts.
    /// </summary>
    public interface IPlannedShiftService
    {

        /// <summary>
        /// Fetches planned shifts based on filter criteria.
        /// </summary>
        /// <param name="filter">The filter criteria (e.g., department, staff, shift type, date range).</param>
        /// <returns>A list of <see cref="PlannedShiftDetailDto"/> matching the filter.</returns>
        Task<List<PlannedShiftDetailDto>> FetchFilteredPlannedShiftsAsync(ShiftFilterDto filter);


        /// <summary>
        /// Adds a new planned shift to the schedule.
        /// </summary>
        /// <param name="plannedShift">The planned shift entity to create.</param>
        /// <returns>
        /// The created <see cref="PlannedShiftDto"/> if successful, or <c>null</c> if creation failed.
        /// </returns>
        Task<PlannedShiftDto?> AddNewPlannedShiftAsync(PlannedShift plannedShift);

        /// <summary>
        /// Fetches all planned shifts within the given date range.
        /// </summary>
        /// <param name="startDate">The start date (inclusive).</param>
        /// <param name="endDate">The end date (inclusive).</param>
        /// <returns>A list of <see cref="PlannedShiftDto"/> representing planned shifts.</returns>
        Task<List<PlannedShiftDto>> FetchPlannedShiftsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Fetches detailed planned shifts (with department, staff, and type info) within the given date range.
        /// </summary>
        /// <param name="startDate">The start date (inclusive).</param>
        /// <param name="endDate">The end date (inclusive).</param>
        /// <returns>A list of <see cref="PlannedShiftDetailDto"/> with extended details.</returns>
        Task<List<PlannedShiftDetailDto>> FetchDetailedPlannedShiftsAsync(DateTime startDate, DateTime endDate);


        /// <summary>
        /// Unassigns a staff member from a planned shift, making the shift vacant.
        /// </summary>
        /// <param name="plannedShiftId">The ID of the planned shift to unassign.</param>
        /// <returns>
        /// The updated <see cref="PlannedShiftDto"/> if successful, or <c>null</c> if the shift was not found.
        /// </returns>
        Task<PlannedShiftDto?> UnassignedShiftFromStaffAsync(int plannedShiftId);

        /// <summary>
        /// Assigns a staff member to a planned shift.
        /// </summary>
        /// <param name="plannedShiftId">The ID of the planned shift.</param>
        /// <param name="staffId">The ID of the staff member to assign.</param>
        /// <returns>
        /// The updated <see cref="PlannedShiftDto"/> if successful, or <c>null</c> if the shift or staff was not found.
        /// </returns>
        Task<PlannedShiftDto?> AssignedShiftToStaffAsync(int plannedShiftId, int staffId);

    }
}
