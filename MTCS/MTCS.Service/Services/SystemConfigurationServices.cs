using MTCS.Common;
using MTCS.Data;
using MTCS.Data.Models;
using MTCS.Data.Request;
using MTCS.Service.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static Google.Cloud.Firestore.V1.StructuredQuery.Types;

namespace MTCS.Service.Services
{
    public interface ISystemConfigurationServices {
        Task<BusinessResult> CreateSystemConfigurationAsync(CreateSystemConfigurationRequestModel request, ClaimsPrincipal claims);
        Task<BusinessResult> UpdateSystemConfigurationAsync(int configId, string configValue, string updatedBy);
        Task<BusinessResult> GetAllSystemConfigurationsAsync();
    }
    public class SystemConfigurationServices : ISystemConfigurationServices
    {
        private readonly UnitOfWork _unitOfWork;
        public SystemConfigurationServices(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

        }

        private async Task<int> GenerateNewConfigIdAsync()
        {
            var configs = await _unitOfWork.SystemConfigurationRepository.GetAllAsync();
            var lastId = configs.Any() ? configs.Max(c => c.ConfigId) : 0;

            return lastId + 1;
        }
        public async Task<BusinessResult> CreateSystemConfigurationAsync(CreateSystemConfigurationRequestModel model, ClaimsPrincipal claims)
        {
            var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
            var newId = await GenerateNewConfigIdAsync();

            var config = new SystemConfiguration
            {
                ConfigId = newId,
                ConfigKey = model.ConfigKey,
                ConfigValue = model.ConfigValue,
                CreatedDate = DateTime.Now,
                CreatedBy = userName
            };

            try
            {
                await _unitOfWork.SystemConfigurationRepository.CreateAsync(config);
                return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, config);
            }
            catch (Exception ex)
            {
                var error = ex.InnerException?.Message ?? ex.Message;
                return new BusinessResult(Const.FAIL_CREATE_CODE, $"Lỗi khi tạo cấu hình hệ thống: {error}");
            }
        }

        public async Task<BusinessResult> UpdateSystemConfigurationAsync(int configId, string configValue, string updatedBy)
        {
            var config = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(configId);
            if (config == null)
            {
                return new BusinessResult(-1, "Không tìm thấy cấu hình!");
            }

            config.ConfigValue = configValue;
            config.UpdatedDate = DateTime.Now;
            config.UpdatedBy = updatedBy;

            try
            {
                await _unitOfWork.SystemConfigurationRepository.UpdateAsync(config);
                return new BusinessResult(1, "Cập nhật cấu hình thành công!", config);
            }
            catch (Exception ex)
            {
                var error = ex.InnerException?.Message ?? ex.Message;
                return new BusinessResult(-2, $"Lỗi khi cập nhật: {error}");
            }
        }

        public async Task<BusinessResult> GetAllSystemConfigurationsAsync()
        {
            var configs = await _unitOfWork.SystemConfigurationRepository.GetAllAsync();
            return new BusinessResult(1, "Lấy danh sách cấu hình thành công!", configs);
        }

    }
}
