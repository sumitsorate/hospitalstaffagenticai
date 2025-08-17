﻿using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dtos.Shift.Response;

namespace HospitalSchedulingApp.Services.Interfaces
{
    public interface IShiftSwapService
    {
        Task<ShiftSwapResponse> SubmitShiftSwapRequestAsync(ShiftSwapRequest request);

        Task<ShiftSwapRequest> ApproveOrRejectShiftSwapRequest(ShiftSwapRequest request);

        Task<List<ShiftSwapRequest>> FetchShiftSwapRequestsAsync(ShiftSwapStatuses status);
    }
}
