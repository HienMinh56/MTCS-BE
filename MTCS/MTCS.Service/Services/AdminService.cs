using MTCS.Data;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Helpers;
using MTCS.Data.Models;
using MTCS.Data.Repository;
using MTCS.Data.Response;

namespace MTCS.Service.Services
{
    public interface IAdminService
    {
        Task<ApiResponse<RevenueAnalyticsDTO>> GetRevenueAnalyticsAsync(RevenuePeriodType periodType, DateTime startDate, DateTime? endDate = null);
        Task<ApiResponse<TripFinancialDTO>> GetTripFinancialDetailsAsync(string tripId);
        Task<ApiResponse<List<TripFinancialDTO>>> GetTripsFinancialDetailsAsync(DateTime? startDate = null, DateTime? endDate = null, string customerId = null);
        Task<ApiResponse<decimal>> GetAverageFuelCostPerDistanceAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<ApiResponse<TripPerformanceDTO>> GetTripPerformanceAsync(DateTime startDate, DateTime endDate);
        Task<ApiResponse<PagedList<CustomerRevenueDTO>>> GetRevenueByCustomerAsync(PaginationParams paginationParams, DateTime? startDate = null, DateTime? endDate = null);
        Task<ApiResponse<InternalUser>> GetInternalUserProfileAsync(string userId);

    }

    public class AdminService : IAdminService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly FinancialRepository _financialRepository;

        public AdminService(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _financialRepository = unitOfWork.FinancialRepository;
        }

        public async Task<ApiResponse<RevenueAnalyticsDTO>> GetRevenueAnalyticsAsync(RevenuePeriodType periodType, DateTime startDate, DateTime? endDate = null)
        {
            try
            {
                if (periodType == RevenuePeriodType.Custom && !endDate.HasValue)
                {
                    return new ApiResponse<RevenueAnalyticsDTO>(
                        false,
                        null,
                        "End date must be provided for custom period type",
                        "Ngày kết thúc phải được cung cấp cho loại kỳ tùy chỉnh",
                        null);
                }

                if (endDate.HasValue && startDate > endDate)
                {
                    return new ApiResponse<RevenueAnalyticsDTO>(
                        false,
                        null,
                        "Start date cannot be later than end date",
                        "Ngày bắt đầu không thể muộn hơn ngày kết thúc",
                        null);
                }

                var result = await _financialRepository.GetRevenueAnalyticsAsync(periodType, startDate, endDate);

                string periodTypeDescription = periodType switch
                {
                    RevenuePeriodType.Weekly => "weekly",
                    RevenuePeriodType.Monthly => "monthly",
                    RevenuePeriodType.Yearly => "yearly",
                    RevenuePeriodType.Custom => "custom period",
                    _ => "period"
                };

                string periodTypeDescriptionVN = periodType switch
                {
                    RevenuePeriodType.Weekly => "theo tuần",
                    RevenuePeriodType.Monthly => "theo tháng",
                    RevenuePeriodType.Yearly => "theo năm",
                    RevenuePeriodType.Custom => "theo kỳ tùy chỉnh",
                    _ => "kỳ"
                };

                return new ApiResponse<RevenueAnalyticsDTO>(
                    true,
                    result,
                    $"Revenue data for {periodTypeDescription} retrieved successfully",
                    $"Dữ liệu doanh thu {periodTypeDescriptionVN} đã được truy xuất thành công",
                    null);
            }
            catch (Exception ex)
            {
                return new ApiResponse<RevenueAnalyticsDTO>(
                    false,
                    null,
                    "Failed to retrieve revenue data",
                    "Không thể truy xuất dữ liệu doanh thu",
                    ex.Message);
            }
        }

        public async Task<ApiResponse<PagedList<CustomerRevenueDTO>>> GetRevenueByCustomerAsync(
            PaginationParams paginationParams,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                var result = await _financialRepository.GetRevenueByCustomerAsync(
                    paginationParams,
                    startDate,
                    endDate);

                return new ApiResponse<PagedList<CustomerRevenueDTO>>(
                    true,
                    result,
                    "Customer revenue data retrieved successfully",
                    "Dữ liệu doanh thu theo khách hàng đã được truy xuất thành công",
                    null);
            }
            catch (Exception ex)
            {
                return new ApiResponse<PagedList<CustomerRevenueDTO>>(
                    false,
                    null,
                    "Failed to retrieve customer revenue data",
                    "Không thể truy xuất dữ liệu doanh thu theo khách hàng",
                    ex.Message);
            }
        }


        public async Task<ApiResponse<TripFinancialDTO>> GetTripFinancialDetailsAsync(string tripId)
        {
            try
            {
                if (string.IsNullOrEmpty(tripId))
                {
                    return new ApiResponse<TripFinancialDTO>(
                        false,
                        null,
                        "Trip ID cannot be empty",
                        "ID chuyến đi không được để trống",
                        null);
                }

                var result = await _financialRepository.GetTripFinancialDetailsAsync(tripId);

                if (result == null)
                {
                    return new ApiResponse<TripFinancialDTO>(
                        false,
                        null,
                        $"No trip found with ID: {tripId}",
                        $"Không tìm thấy chuyến đi với ID: {tripId}",
                        null);
                }

                return new ApiResponse<TripFinancialDTO>(
                    true,
                    result,
                    "Trip financial details retrieved successfully",
                    "Chi tiết tài chính của chuyến đi đã được truy xuất thành công",
                    null);
            }
            catch (Exception ex)
            {
                return new ApiResponse<TripFinancialDTO>(
                    false,
                    null,
                    "Failed to retrieve trip financial details",
                    "Không thể truy xuất chi tiết tài chính của chuyến đi",
                    ex.Message);
            }
        }

        public async Task<ApiResponse<List<TripFinancialDTO>>> GetTripsFinancialDetailsAsync(
            DateTime? startDate = null, DateTime? endDate = null, string customerId = null)
        {
            try
            {
                var result = await _financialRepository.GetTripsFinancialDetailsAsync(startDate, endDate, customerId);

                if (result == null || result.Count == 0)
                {
                    return new ApiResponse<List<TripFinancialDTO>>(
                        true,
                        new List<TripFinancialDTO>(),
                        "No trips found matching the criteria",
                        "Không tìm thấy chuyến đi nào phù hợp với tiêu chí",
                        null);
                }

                return new ApiResponse<List<TripFinancialDTO>>(
                    true,
                    result,
                    "Trips financial details retrieved successfully",
                    "Chi tiết tài chính của các chuyến đi đã được truy xuất thành công",
                    null);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<TripFinancialDTO>>(
                    false,
                    null,
                    "Failed to retrieve trips financial details",
                    "Không thể truy xuất chi tiết tài chính của các chuyến đi",
                    ex.Message);
            }
        }

        public async Task<ApiResponse<decimal>> GetAverageFuelCostPerDistanceAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                {
                    return new ApiResponse<decimal>(
                        false,
                        0,
                        "Start date cannot be later than end date",
                        "Ngày bắt đầu không thể muộn hơn ngày kết thúc",
                        null);
                }

                var result = await _financialRepository.GetAverageFuelCostPerDistanceAsync(startDate, endDate);
                return new ApiResponse<decimal>(
                    true,
                    result,
                    "Average fuel cost per distance retrieved successfully",
                    "Chi phí nhiên liệu trung bình trên mỗi đơn vị khoảng cách đã được truy xuất thành công",
                    null);
            }
            catch (Exception ex)
            {
                return new ApiResponse<decimal>(
                    false,
                    0,
                    "Failed to retrieve average fuel cost per distance",
                    "Không thể truy xuất chi phí nhiên liệu trung bình trên mỗi đơn vị khoảng cách",
                    ex.Message);
            }
        }

        public async Task<ApiResponse<TripPerformanceDTO>> GetTripPerformanceAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                if (startDate > endDate)
                {
                    return new ApiResponse<TripPerformanceDTO>(
                        false,
                        null,
                        "Start date cannot be later than end date",
                        "Ngày bắt đầu không thể muộn hơn ngày kết thúc",
                        null);
                }

                var result = await _financialRepository.GetTripPerformanceAsync(startDate, endDate);
                return new ApiResponse<TripPerformanceDTO>(
                    true,
                    result,
                    "Trip performance data retrieved successfully",
                    "Dữ liệu hiệu suất chuyến đi đã được truy xuất thành công",
                    null);
            }
            catch (Exception ex)
            {
                return new ApiResponse<TripPerformanceDTO>(
                    false,
                    null,
                    "Failed to retrieve trip performance data",
                    "Không thể truy xuất dữ liệu hiệu suất chuyến đi",
                    ex.Message);
            }
        }

        public async Task<ApiResponse<InternalUser>> GetInternalUserProfileAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return new ApiResponse<InternalUser>(
                        false,
                        null,
                        $"No user found with ID: {userId}",
                        $"Không tìm thấy người dùng với ID: {userId}",
                        null);
                }

                var result = await _unitOfWork.InternalUserRepository.GetUserByIdAsync(userId);

                if (result == null)
                {
                    return new ApiResponse<InternalUser>(
                        false,
                        null,
                        $"No staff found with ID: {userId}",
                        $"Không tìm thấy nhân viên với ID: {userId}",
                        null);
                }

                return new ApiResponse<InternalUser>(
                    true,
                    result,
                    "Staff details retrieved successfully",
                    "Chi tiết nhân viên đã được truy xuất thành công",
                    null);
            }
            catch (Exception ex)
            {
                return new ApiResponse<InternalUser>(
                    false,
                    null,
                    "Failed to retrieve staff details",
                    "Không thể truy xuất chi tiết nhân viên",
                    ex.Message);
            }
        }

    }
}
