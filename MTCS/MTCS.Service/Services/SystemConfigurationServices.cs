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

namespace MTCS.Service.Services
{
    public interface ISystemConfigurationServices {
        Task<BusinessResult> CreateSystemConfigurationAsync(CreateSystemConfigurationRequestModel request, ClaimsPrincipal claims);
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

    }
}
