using Microsoft.AspNetCore.Http;
using MTCS.Data;
using MTCS.Data.Models;
using MTCS.Data.Request;
using MTCS.Data.Response;
using MTCS.Service.Base;
using OfficeOpenXml;

namespace MTCS.Service.Services
{

    public interface IPriceTableService
    {
        public Task<BusinessResult> GetPriceTableById(string id);

        public Task<BusinessResult> DownloadPriceTableTemplate();
        public Task<BusinessResult> DeletePriceTable(string id);
        Task<ApiResponse<CalculatedPriceResponse>> CalculatePrice(double distance, int containerType, int containerSize, int deliveryType);
        Task<ApiResponse<List<PriceTable>>> CreatePriceTable(List<CreatePriceTableRequest> priceTable, string userName);
        Task<ApiResponse<List<PriceTable>>> ImportPriceTable(IFormFile excelFile, string userName);
        Task<ApiResponse<PriceTablesHistoryDTO>> GetPriceTables(int? version = null);
        Task<ApiResponse<List<PriceChangeGroup>>> GetPriceChangesInVersion(int version);
        Task<ApiResponse<PriceTable>> UpdatePriceTable(UpdatePriceTableRequest priceTable, string userName);
    }
    public class PriceTableService : IPriceTableService
    {
        private readonly UnitOfWork _unitOfWork;

        public PriceTableService(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<PriceTable>> UpdatePriceTable(UpdatePriceTableRequest priceTable, string userName)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var existingPrice = _unitOfWork.PriceTableRepository.Get(pt => pt.PriceId == priceTable.PriceId);
                if (existingPrice == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return new ApiResponse<PriceTable>(
                        false,
                        null,
                        $"Price Table with ID {priceTable.PriceId} not found",
                        $"Không tìm thấy bảng giá có ID {priceTable.PriceId}",
                        null);
                }

                if (existingPrice.Status == 0)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return new ApiResponse<PriceTable>(
                        false,
                        null,
                        $"Can not update inactive price",
                        $"Không thể cập nhật giá không hiện hành",
                        null);
                }

                existingPrice.Status = 0;
                existingPrice.ModifiedBy = userName;
                existingPrice.ModifiedDate = DateTime.Now;
                await _unitOfWork.PriceTableRepository.UpdateAsync(existingPrice);

                var newPrice = new PriceTable
                {
                    PriceId = Guid.NewGuid().ToString(),
                    MinKm = existingPrice.MinKm,
                    MaxKm = existingPrice.MaxKm,
                    ContainerSize = existingPrice.ContainerSize,
                    ContainerType = existingPrice.ContainerType,
                    DeliveryType = existingPrice.DeliveryType,
                    Version = existingPrice.Version,

                    MinPricePerKm = priceTable.MinPricePerKm,
                    MaxPricePerKm = priceTable.MaxPricePerKm,

                    Status = 1,
                    CreatedBy = userName,
                    CreatedDate = DateTime.Now
                };

                await _unitOfWork.PriceTableRepository.CreateAsync(newPrice);
                await _unitOfWork.CommitTransactionAsync();

                return new ApiResponse<PriceTable>(
                    true,
                    newPrice,
                    "Successfully updated price record",
                    "Đã cập nhật thành công bản ghi giá",
                    null);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ApiResponse<PriceTable>(
                    false,
                    null,
                    "Failed to update price table",
                    "Cập nhật bảng giá thất bại",
                    ex.Message);
            }
        }

        public async Task<ApiResponse<List<PriceTable>>> CreatePriceTable(List<CreatePriceTableRequest> priceTable, string userName)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var currentMaxVersion = await _unitOfWork.PriceTableRepository.GetMaxVersion() ?? 0;
                if (currentMaxVersion > 0)
                {
                    var currentVersionTables = await _unitOfWork.PriceTableRepository.GetPriceTables(currentMaxVersion);
                    foreach (var item in currentVersionTables)
                    {
                        item.Status = 0;
                        item.ModifiedBy = userName;
                        item.ModifiedDate = DateTime.Now;
                        await _unitOfWork.PriceTableRepository.UpdateAsync(item);
                    }
                }

                int newVersion = currentMaxVersion + 1;
                var newPriceTables = priceTable.Select(price => new PriceTable
                {
                    PriceId = Guid.NewGuid().ToString(),
                    MinKm = price.MinKm,
                    MaxKm = price.MaxKm,
                    ContainerSize = price.ContainerSize,
                    ContainerType = price.ContainerType,
                    MinPricePerKm = price.MinPricePerKm,
                    MaxPricePerKm = price.MaxPricePerKm,
                    Status = 1,
                    Version = newVersion,
                    CreatedBy = userName,
                    CreatedDate = DateTime.Now
                }).ToList();

                foreach (var newPriceTable in newPriceTables)
                {
                    await _unitOfWork.PriceTableRepository.CreateAsync(newPriceTable);
                }
                await _unitOfWork.CommitTransactionAsync();
                return new ApiResponse<List<PriceTable>>(true, newPriceTables, "Price table created successfully", "Đã tạo bảng giá thành công", null);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ApiResponse<List<PriceTable>>(false, null, "Failed to create price table", "Tạo bảng giá thất bại", ex.Message);
            }
        }

        #region GetPriceTableById
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
        #endregion

        #region DeletePriceTable
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
        #endregion

        #region ImportPriceTable
        public async Task<ApiResponse<List<PriceTable>>> ImportPriceTable(IFormFile excelFile, string userName)
        {
            try
            {
                if (!excelFile.FileName.EndsWith(".xlsx") && !excelFile.FileName.EndsWith(".xls"))
                {
                    return new ApiResponse<List<PriceTable>>(false, null, "Only Excel files are allowed", "Chỉ chấp nhận file Excel", null);
                }

                var priceTableList = new List<CreatePriceTableRequest>();
                using (var stream = new MemoryStream())
                {
                    await excelFile.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];

                        if (worksheet.Dimension == null || worksheet.Dimension.Rows <= 1)
                        {
                            return new ApiResponse<List<PriceTable>>(false, null, "Excel file is empty or has no data rows", "File Excel trống hoặc không có dữ liệu", null);
                        }

                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                if (worksheet.Cells[row, 1].Value == null || worksheet.Cells[row, 2].Value == null)
                                {
                                    return new ApiResponse<List<PriceTable>>(false, null, $"Missing required data in row {row}", $"Thiếu dữ liệu cần thiết ở dòng {row}", null);
                                }

                                var minKm = Convert.ToDouble(worksheet.Cells[row, 1].Value);
                                var maxKm = Convert.ToDouble(worksheet.Cells[row, 2].Value);

                                if (minKm >= maxKm)
                                {
                                    return new ApiResponse<List<PriceTable>>(false, null, $"Min Km must be less than Max Km in row {row}", $"Km tối thiểu phải nhỏ hơn Km tối đa ở dòng {row}", null);
                                }

                                var priceTable = new CreatePriceTableRequest
                                {
                                    MinKm = minKm,
                                    MaxKm = maxKm,
                                    ContainerSize = Convert.ToInt32(worksheet.Cells[row, 3].Value),
                                    ContainerType = Convert.ToInt32(worksheet.Cells[row, 4].Value),
                                    MinPricePerKm = Convert.ToDecimal(worksheet.Cells[row, 5].Value),
                                    MaxPricePerKm = Convert.ToDecimal(worksheet.Cells[row, 6].Value),
                                };
                                priceTableList.Add(priceTable);
                            }
                            catch (FormatException)
                            {
                                return new ApiResponse<List<PriceTable>>(false, null, $"Invalid data format in row {row}. Please ensure all cells contain the correct data type.", $"Định dạng dữ liệu không hợp lệ ở dòng {row}. Vui lòng đảm bảo tất cả các ô chứa đúng kiểu dữ liệu.", null);
                            }
                        }
                    }
                }

                if (priceTableList.Count == 0)
                {
                    return new ApiResponse<List<PriceTable>>(false, null, "No valid price table entries found in the Excel file", "Không tìm thấy bảng giá hợp lệ trong file Excel", null);
                }
                return await CreatePriceTable(priceTableList, userName);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<PriceTable>>(false, null, "Failed to import price table", "Nhập bảng giá thất bại", ex.Message);
            }
        }
        #endregion

        #region DownloadPriceTableTemplate
        public async Task<BusinessResult> DownloadPriceTableTemplate()
        {
            try
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("PriceTableTemplate");

                    // Add headers
                    worksheet.Cells[1, 1].Value = "Từ Km";
                    worksheet.Cells[1, 2].Value = "Đến Km";
                    worksheet.Cells[1, 3].Value = "Kích Thước Container(1 20feet / 2 40 feet)";
                    worksheet.Cells[1, 4].Value = "Loại Container (1 Khô/ 2 Lạnh)";
                    worksheet.Cells[1, 5].Value = "Giá nhỏ nhất mỗi km";
                    worksheet.Cells[1, 6].Value = "Giá lớn nhất mỗi km";

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
        #endregion

        public async Task<ApiResponse<PriceTablesHistoryDTO>> GetPriceTables(int? version = null)
        {
            try
            {
                var (versionsInfo, activeVersion) = await _unitOfWork.PriceTableRepository.GetPriceTableVersions();

                var priceTables = await _unitOfWork.PriceTableRepository.GetPriceTables(version);

                if (priceTables == null || !priceTables.Any())
                {
                    string message = version.HasValue
                        ? $"No price tables found for version {version}"
                        : "No price tables found in the latest version";

                    return new ApiResponse<PriceTablesHistoryDTO>(
                        false,
                        new PriceTablesHistoryDTO
                        {
                            PriceTables = new List<PriceTable>(),
                            VersionsInfo = versionsInfo,
                            CurrentVersion = version ?? versionsInfo.FirstOrDefault()?.Version ?? 0,
                            ActiveVersion = activeVersion,
                            TotalCount = 0
                        },
                        message,
                        "Không tìm thấy bảng giá",
                        null);
                }

                var currentVersion = version ?? priceTables.FirstOrDefault()?.Version ?? 0;

                var activePrices = priceTables.Where(p => p.Status == 1).ToList();
                var inactivePrices = priceTables.Where(p => p.Status == 0).ToList();

                var response = new PriceTablesHistoryDTO
                {
                    PriceTables = priceTables,
                    VersionsInfo = versionsInfo,
                    ActiveVersion = activeVersion,
                    CurrentVersion = currentVersion,
                    TotalCount = priceTables.Count
                };

                string successMessage = version.HasValue
                    ? $"Retrieved {priceTables.Count} price tables for version {version}"
                    : $"Retrieved {priceTables.Count} price tables from the latest version";

                return new ApiResponse<PriceTablesHistoryDTO>(
                    true,
                    response,
                    successMessage,
                    "Lấy bảng giá thành công",
                    null);
            }
            catch (Exception ex)
            {
                return new ApiResponse<PriceTablesHistoryDTO>(
                    false,
                    null,
                    "Failed to retrieve price tables",
                    "Lấy bảng giá thất bại",
                    ex.Message);
            }
        }

        public async Task<ApiResponse<CalculatedPriceResponse>> CalculatePrice(double distance, int containerType, int containerSize, int deliveryType)
        {
            var matchedPrice = await _unitOfWork.PriceTableRepository.GetPriceForCalculation(
            distance, containerType, containerSize, deliveryType);

            if (matchedPrice == null)
            {
                return new ApiResponse<CalculatedPriceResponse>(false, null, "No price found ", "Không tìm thấy giá", null);
            }
            decimal decimalDistance = (decimal)distance;
            decimal minPrice = matchedPrice.MinPricePerKm ?? 0;
            decimal maxPrice = matchedPrice.MaxPricePerKm ?? 0;

            decimal basePrice = minPrice * decimalDistance;
            decimal averagePrice = ((minPrice + maxPrice) / 2) * decimalDistance;
            decimal highestPrice = maxPrice * decimalDistance;

            var calculatedPrice = new CalculatedPriceResponse
            {
                BasePrice = basePrice,
                AveragePrice = averagePrice,
                HighestPrice = highestPrice
            };

            return new ApiResponse<CalculatedPriceResponse>(true, calculatedPrice, "Price calculated successfully", "Tính giá thành công", null);
        }

        public async Task<ApiResponse<List<PriceChangeGroup>>> GetPriceChangesInVersion(int version)
        {
            try
            {
                var priceChanges = await _unitOfWork.PriceTableRepository.GetPriceChangesInVersion(version);

                if (priceChanges == null || !priceChanges.Any())
                {
                    return new ApiResponse<List<PriceChangeGroup>>(
                        false,
                        new List<PriceChangeGroup>(),
                        $"No price changes found in version {version}",
                        $"Không tìm thấy thay đổi giá trong phiên bản {version}",
                        null);
                }

                return new ApiResponse<List<PriceChangeGroup>>(
                    true,
                    priceChanges,
                    $"Retrieved {priceChanges.Count} price change groups for version {version}",
                    $"Đã lấy {priceChanges.Count} thay đổi giá cho phiên bản {version}",
                    null);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<PriceChangeGroup>>(
                    false,
                    null,
                    "Failed to retrieve price changes",
                    "Lấy thay đổi giá thất bại",
                    ex.Message);
            }
        }
    }
}