using System.Security.Claims;
using MTCS.Data;
using MTCS.Data.Models;
using MTCS.Data.Request;
using MTCS.Service.Base;

namespace MTCS.Service.Services
{
    public interface IDeliveryStatusService
    {
        Task<BusinessResult> GetDeliveryStatuses();
        Task<BusinessResult> GetDeliveryStatusById(string id);

        Task<BusinessResult> CreateDeliveryStatus(List<CreateDeliveryStatusRequest> deliveryStatus, ClaimsPrincipal claims);
    }

    public class DeliveryStatusService : IDeliveryStatusService
    {
        private readonly UnitOfWork _unitOfWork;

        public DeliveryStatusService(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        #region CreateDeliveryStatus
        public async Task<BusinessResult> CreateDeliveryStatus(List<CreateDeliveryStatusRequest> deliveryStatus, ClaimsPrincipal claims)
        {
            try
            {
                // Check if any trip is currently being processed (not completed, not not_started, not canceled)
                var processingTrips = await _unitOfWork.TripRepository.GetAllTripsAsync();

                // If any trip has status not in ["completed", "not_started", "canceled"], block creation
                var hasProcessingTrip = processingTrips.Any(t =>
                    t.Status != "completed" &&
                    t.Status != "not_started" &&
                    t.Status != "canceled"
                );

                if (hasProcessingTrip)
                {
                    return new BusinessResult(400, "Can not create or update delivery status when there is a trip in use");
                }

                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    // Get all existing statuses
                    var existingStatuses = (await _unitOfWork.DeliveryStatusRepository.GetDeliveryStatusesAsync()).ToDictionary(s => s.StatusId);

                    // Separate statuses by category
                    var notStartedStatus = deliveryStatus.FirstOrDefault(s => s.StatusId == "not_started");
                    var completedStatus = deliveryStatus.FirstOrDefault(s => s.StatusId == "completed");
                    var activeMiddleStatuses = deliveryStatus
                        .Where(s => s.StatusId != "not_started" && s.StatusId != "completed" && s.IsActive == 1)
                        .OrderBy(s => s.StatusIndex)
                        .ToList();
                    var inactiveMiddleStatuses = deliveryStatus
                        .Where(s => s.StatusId != "not_started" && s.StatusId != "completed" && s.IsActive != 1)
                        .OrderBy(s => s.StatusIndex)
                        .ToList();

                    // Track indices for each status category
                    int currentIndex = 0;
                    int maxActiveIndex = 0;

                    // 1. Process not_started (always index 0)
                    if (notStartedStatus != null)
                    {
                        currentIndex = await ProcessStatus("not_started", notStartedStatus, 0, existingStatuses, userName);
                    }

                    // 2. Process active middle statuses (indices start at 1)
                    currentIndex = 1;
                    foreach (var status in activeMiddleStatuses)
                    {
                        currentIndex = await ProcessStatus(status.StatusId, status, currentIndex, existingStatuses, userName);
                        maxActiveIndex = currentIndex - 1; // Track the highest active status index
                    }

                    // 3. Process completed status (right after active statuses)
                    int completedIndex = maxActiveIndex + 1;
                    if (completedStatus != null)
                    {
                        currentIndex = await ProcessStatus("completed", completedStatus, completedIndex, existingStatuses, userName);
                    }

                    // 4. Process inactive middle statuses (after completed)
                    foreach (var status in inactiveMiddleStatuses)
                    {
                        currentIndex = await ProcessStatus(status.StatusId, status, currentIndex, existingStatuses, userName);
                    }

                    await _unitOfWork.CommitTransactionAsync();
                    return new BusinessResult(200, "Create delivery status success");
                }
                catch (Exception)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BusinessResult(500, ex.Message);
            }
        }

        // Helper method to process a single status - returns the next index to use
        private async Task<int> ProcessStatus(string statusId, CreateDeliveryStatusRequest statusRequest, int assignedIndex, Dictionary<string, DeliveryStatus> existingStatuses, string userName)
        {
            if (existingStatuses.TryGetValue(statusId, out var existingStatus))
            {
                // Update existing status
                existingStatus.StatusName = statusRequest.StatusName;
                existingStatus.StatusIndex = assignedIndex;
                existingStatus.IsActive = statusRequest.IsActive;
                existingStatus.ModifiedBy = userName;
                existingStatus.ModifiedDate = DateTime.Now;
                _unitOfWork.DeliveryStatusRepository.Update(existingStatus);
            }
            else
            {
                // Create new status
                await _unitOfWork.DeliveryStatusRepository.CreateAsync(new DeliveryStatus
                {
                    StatusId = statusId,
                    StatusName = statusRequest.StatusName,
                    StatusIndex = assignedIndex,
                    IsActive = statusRequest.IsActive,
                    CreatedBy = userName,
                    CreatedDate = DateTime.Now
                });
            }

            return assignedIndex + 1; // Return the next index to use
        }
        #endregion


        #region GetDeliveryStatusById
        public async Task<BusinessResult> GetDeliveryStatusById(string id)
        {
            try
            {
                var deliveryStatus = _unitOfWork.DeliveryStatusRepository.GetDeliveryStatusByIdAsync(id);
                if (deliveryStatus == null)
                {
                    return new BusinessResult(404, "Not Found");
                }
                return new BusinessResult(200, "Success", deliveryStatus);
            }
            catch (Exception ex)
            {
                return new BusinessResult(500, ex.Message);
            }
        }
        #endregion

        #region GetDeliveryStatusById
        public async Task<BusinessResult> GetDeliveryStatuses()
        {
            try
            {
                var deliveryStatuses = await _unitOfWork.DeliveryStatusRepository.GetDeliveryStatusesAsync();
                return new BusinessResult(200, "Success", deliveryStatuses);
            }
            catch (Exception ex)
            {
                return new BusinessResult(500, ex.Message);
            }
        }
        #endregion
    }
}
