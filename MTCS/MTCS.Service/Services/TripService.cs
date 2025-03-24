using Microsoft.EntityFrameworkCore;
using MTCS.Data;
using MTCS.Data.DTOs;
using MTCS.Data.Response;
using MTCS.Service.Interfaces;

namespace MTCS.Service.Services
{
    public class TripService : ITripService
    {
        private readonly UnitOfWork _unitOfWork;

        public TripService(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<List<TripDTO>>> GetDriverAssignedTrips(string driverId)
        {
            if (await _unitOfWork.DriverRepository.GetDriverByIdAsync(driverId) == null)
            {
                return new ApiResponse<List<TripDTO>>(false, null, "Driver not found", null);
            }

            var query = _unitOfWork.TripRepository.GetTripsByDriverIdAsync(driverId);

            if (query == null) return new ApiResponse<List<TripDTO>>(false, null, "No trip found", null);

            var trips = await query
            .Select(t => new TripDTO
            {
                TripId = t.TripId,
                OrderId = t.OrderId,
                TrackingCode = t.Order.TrackingCode,
                DriverName = t.Driver.FullName,
                CurrentStatus = t.TripStatusHistories
                .OrderByDescending(h => h.ChangeTime)
                .Select(h => h.Status.StatusName)
                .FirstOrDefault()
            })
            .ToListAsync();

            return new ApiResponse<List<TripDTO>>(true, trips, "Get trips succesfully", null);
        }

        public async Task<ApiResponse<DetailedTripDTO>> GetTripDetails(string tripId)
        {
            var trip = await _unitOfWork.TripRepository.GetTripDetailsByID(tripId);
            if (trip == null)
            {
                return new ApiResponse<DetailedTripDTO>(false, null, "Trip not found", null);
            }

            var latestStatus = trip.TripStatusHistories
                .OrderByDescending(h => h.ChangeTime)
                .FirstOrDefault();

            var detailedTripDTO = new DetailedTripDTO
            {
                // Basic trip information
                TripId = trip.TripId,
                DriverName = trip.Driver.FullName,
                CurrentStatus = latestStatus?.Status.StatusName ?? "Unknown",
                StartTime = trip.StartTime,
                EndTime = trip.EndTime,
                // Distance = trip.Distance,

                // Vehicle information
                TractorInfo = new VehicleInfoDTO
                {
                    Id = trip.TractorId,
                    Brand = trip.Tractor.Brand,
                    LicensePlate = trip.Tractor.LicensePlate,
                },

                TrailerInfo = new VehicleInfoDTO
                {
                    Id = trip.TrailerId,
                    Brand = trip.Trailer.Brand,
                    LicensePlate = trip.Trailer.LicensePlate,
                },

                // Order information
                OrderInfo = new OrderInfoDTO
                {
                    OrderId = trip.Order.OrderId,
                    TrackingCode = trip.Order.TrackingCode,
                    PickupLocation = trip.Order.PickUpLocation,
                    DeliveryLocation = trip.Order.DeliveryLocation,
                    PickupDate = trip.Order.PickUpDate,
                    DeliveryDate = trip.Order.DeliveryDate,
                    Weight = trip.Order.Weight,
                    Temperature = trip.Order.Temperature,
                    Status = trip.Order.Status
                },

                // Status history
                StatusHistory = trip.TripStatusHistories
                    .OrderByDescending(h => h.ChangeTime)
                    .Select(h => new StatusHistoryDTO
                    {
                        StatusName = h.Status.StatusName,
                        ChangeTime = h.ChangeTime,
                        Notes = h.Notes
                    })
                    .ToList(),

                //DeliveryReportsCount = trip.DeliveryReports.Count,
                //FuelReportsCount = trip.FuelReports.Count,
                //IncidentReportsCount = trip.IncidentReports.Count,
                //InspectionLogsCount = trip.InspectionLogs.Count
            };

            return new ApiResponse<DetailedTripDTO>(true, detailedTripDTO, "Trip details retrieved successfully", null);
        }

    }
}
