using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MTCS.Data;
using MTCS.Data.Models;
using MTCS.Data.Request;
using MTCS.Service.Base;
using OfficeOpenXml;

namespace MTCS.Service.Services
{

    public interface IPriceTableService
    {
        public Task<BusinessResult> GetPriceTables();
        public Task<BusinessResult> GetPriceTableById(string id);
        public Task<BusinessResult> CreatePriceTable(List<CreatePriceTableRequest> priceTable, string userName);
        public Task<BusinessResult> UpdatePriceTable(List<UpdatePriceTableRequest> priceTable, ClaimsPrincipal claims);
        public Task<BusinessResult> ImportPriceTable(IFormFile excelFile, string userName);
        public Task<BusinessResult> DownloadPriceTableTemplate();
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
                    existingPrice.ModifiedDate = DateTime.Now;
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
                        CreatedDate = DateTime.Now
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

        public async Task<BusinessResult> CreatePriceTable(List<CreatePriceTableRequest> priceTable, string userName)
        {
            try
            {
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
                        DeliveryType = price.DeliveryType,
                        Status = 1,
                        CreatedBy = userName,
                        CreatedDate = DateTime.Now
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

        public async Task<BusinessResult> ImportPriceTable(IFormFile excelFile, string userName)
        {
            try
            {
                var priceTableList = new List<CreatePriceTableRequest>();

                // Update status of all existing price tables to 0
                var existingPriceTables = _unitOfWork.PriceTableRepository.GetAll().ToList();
                foreach (var priceTable in existingPriceTables)
                {
                    priceTable.Status = 0;
                    priceTable.ModifiedBy = userName;
                    priceTable.ModifiedDate = DateTime.Now;
                    await _unitOfWork.PriceTableRepository.UpdateAsync(priceTable);
                }

                using (var stream = new MemoryStream())
                {
                    await excelFile.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var priceTable = new CreatePriceTableRequest
                            {
                                MinKm = Convert.ToDouble(worksheet.Cells[row, 1].Value),
                                MaxKm = Convert.ToDouble(worksheet.Cells[row, 2].Value),
                                ContainerSize = Convert.ToInt32(worksheet.Cells[row, 3].Value),
                                ContainerType = Convert.ToInt32(worksheet.Cells[row, 4].Value),
                                MinPricePerKm = Convert.ToDecimal(worksheet.Cells[row, 5].Value),
                                MaxPricePerKm = Convert.ToDecimal(worksheet.Cells[row, 6].Value),
                                DeliveryType = Convert.ToInt32(worksheet.Cells[row, 7].Value)
                            };
                            priceTableList.Add(priceTable);
                        }
                    }
                }

                return await CreatePriceTable(priceTableList, userName);
            }
            catch (Exception ex)
            {
                return new BusinessResult(500, ex.Message);
            }
        }
        public async Task<BusinessResult> DownloadPriceTableTemplate()
        {
            try
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("PriceTableTemplate");

                    // Add headers
                    worksheet.Cells[1, 1].Value = "MinKm";
                    worksheet.Cells[1, 2].Value = "MaxKm";
                    worksheet.Cells[1, 3].Value = "ContainerSize";
                    worksheet.Cells[1, 4].Value = "ContainerType";
                    worksheet.Cells[1, 5].Value = "MinPricePerKm";
                    worksheet.Cells[1, 6].Value = "MaxPricePerKm";
                    worksheet.Cells[1, 7].Value = "DeliveryType";

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    var result = new BusinessResult(200, "Template generated successfully", stream.ToArray());
                    return await Task.FromResult(result);
                }
            }
            catch (Exception ex)
            {
                return new BusinessResult(500, ex.Message);
            }
        }

    }
}