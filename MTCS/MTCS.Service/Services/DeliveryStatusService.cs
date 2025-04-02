using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using MTCS.Data;
using MTCS.Data.Models;
using MTCS.Data.Repository;
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

        public async Task<BusinessResult> CreateDeliveryStatus(List<CreateDeliveryStatusRequest> deliveryStatus, ClaimsPrincipal claims)
        {
            try
            {
                var customerId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                                ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                await _unitOfWork.BeginTransactionAsync();

                foreach (var status in deliveryStatus)
                {
                    var existingStatus = _unitOfWork.DeliveryStatusRepository.Get(ds => ds.StatusId == status.StatusId);
                    if (existingStatus != null)
                    {
                        existingStatus.StatusName = status.StatusName;
                        existingStatus.StatusIndex = status.StatusIndex;
                        existingStatus.IsActive = status.IsActive;
                        existingStatus.ModifiedBy = userName;
                        existingStatus.ModifiedDate = DateTime.Now;
                        _unitOfWork.DeliveryStatusRepository.Update(existingStatus);
                    }
                    else
                    {
                        var newStatus = new DeliveryStatus
                        {
                            StatusId = status.StatusId,
                            StatusName = status.StatusName,
                            StatusIndex = status.StatusIndex,
                            IsActive = status.IsActive,
                            CreatedBy = userName,
                            CreatedDate = DateTime.Now
                        };

                        await _unitOfWork.DeliveryStatusRepository.CreateAsync(newStatus);
                    }
                }

                return new BusinessResult(200, "Create delivery status success");

            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BusinessResult(500, ex.Message);
            }
        }
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
    }
}
