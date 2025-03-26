using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using MTCS.Data;
using MTCS.Data.Models;
using MTCS.Data.Request;
using MTCS.Service.Base;

namespace MTCS.Service.Services
{

    public interface IPriceTableService
    {
        public Task<BusinessResult> GetPriceTables();
        public Task<BusinessResult> GetPriceTableById(string id);
        public Task<BusinessResult> CreatePriceTable(List<CreatePriceTableRequest> priceTable, ClaimsPrincipal claims);
        public Task<BusinessResult> UpdatePriceTable(List<UpdatePriceTableRequest> priceTable, ClaimsPrincipal claims);
        public Task<BusinessResult> DeletePriceTable(string id);
    }
    public class PriceTableService : IPriceTableService
    {
        private readonly UnitOfWork _unitOfWork;

        public PriceTableService(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BusinessResult> UpdatePriceTable(List<UpdatePriceTableRequest> priceTable, ClaimsPrincipal claims)
        {
            try
            {
                var customerId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                                ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                await _unitOfWork.BeginTransactionAsync();
                foreach (var price in priceTable)
                {
                    var existingPrice = _unitOfWork.PriceTableRepository.Get(pt => pt.PriceId == price.PriceId);
                    if (existingPrice == null)
                    {
                        return new BusinessResult(400, "Price Table cannot find");
                    }
                    existingPrice.Status = 0;
                    existingPrice.ModifiedBy = userName;
                    existingPrice.ModifiedDate = DateTime.UtcNow;
                    await _unitOfWork.PriceTableRepository.UpdateAsync(existingPrice);


                    var newPrice = new PriceTable
                    {
                        PriceId = Guid.NewGuid().ToString(),
                        MinKm = price.MinKm,
                        MaxKm = price.MaxKm,
                        ContainerSize = price.ContainerSize,
                        ContainerType = price.ContainerType,
                        MinPricePerKm = price.MinPricePerKm,
                        MaxPricePerKm = price.MaxPricePerKm,
                        Status = 1,
                        CreatedBy = userName,
                        CreatedDate = DateTime.UtcNow
                    };
                    await _unitOfWork.PriceTableRepository.CreateAsync(newPrice);
                }
                return new BusinessResult(200, "Price table created successfully");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BusinessResult(500, ex.Message);
            }
        }

        public async Task<BusinessResult> CreatePriceTable(List<CreatePriceTableRequest> priceTable, ClaimsPrincipal claims)
        {
            try
            {
                var customerId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                                ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                await _unitOfWork.BeginTransactionAsync();
                foreach (var price in priceTable)
                {
                    var newPricetable = new PriceTable
                    {
                        PriceId = Guid.NewGuid().ToString(),
                        MinKm = price.MinKm,
                        MaxKm = price.MaxKm,
                        ContainerSize = price.ContainerSize,
                        ContainerType = price.ContainerType,
                        MinPricePerKm = price.MinPricePerKm,
                        MaxPricePerKm = price.MaxPricePerKm,
                        Status = 1,
                        CreatedBy = userName,
                        CreatedDate = DateTime.UtcNow
                    };
                    await _unitOfWork.PriceTableRepository.CreateAsync(newPricetable);
                }
                return new BusinessResult(200, "Price table created successfully");
            }

            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BusinessResult(500, ex.Message);
            }

        }

        public async Task<BusinessResult> GetPriceTables()
        {
            try
            {
                var priceTables = _unitOfWork.PriceTableRepository.GetAll();
                return new BusinessResult(200, "Get Price Tables successfully", priceTables);
            }
            catch (Exception ex)
            {
                return new BusinessResult(500, ex.Message);
            }
        }

        public async Task<BusinessResult> GetPriceTableById(string id)
        {
            try
            {
                var priceTable = _unitOfWork.PriceTableRepository.Get(pt => pt.PriceId == id);
                return new BusinessResult(200, "Get Price Table successfully", priceTable);
            }
            catch (Exception ex)
            {
                return new BusinessResult(500, ex.Message);
            }
        }

        public Task<BusinessResult> DeletePriceTable(string id)
        {
            try
            {
                var priceTable = _unitOfWork.PriceTableRepository.Get(pt => pt.PriceId == id);
                if (priceTable == null)
                {
                    return Task.FromResult(new BusinessResult(404, "Price Table not found"));
                }
                priceTable.Status = 0;
                _unitOfWork.PriceTableRepository.Update(priceTable);
                return Task.FromResult(new BusinessResult(200, "Price Table deleted successfully"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new BusinessResult(500, ex.Message));
            }
        }
    }
}