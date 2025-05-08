using Microsoft.AspNetCore.Http;
using MTCS.Data;
using MTCS.Data.Models;
using MTCS.Data.Request;
using MTCS.Data.Response;
using MTCS.Service.Base;
using MTCS.Service.Cache;
using OfficeOpenXml;

namespace MTCS.Service.Services
{

    public interface IPriceTableService
    {
        public Task<BusinessResult> GetPriceTableById(string id);

        public Task<BusinessResult> DownloadPriceTableTemplate();
        public Task<BusinessResult> DeletePriceTable(string id);
        Task<ApiResponse<List<PriceTable>>> CreatePriceTable(List<CreatePriceTableRequest> priceTable, string userName);
        Task<ApiResponse<List<PriceTable>>> ImportPriceTable(IFormFile excelFile, string userName);
        Task<ApiResponse<PriceTablesHistoryDTO>> GetPriceTables(int? version = null);
        Task<ApiResponse<List<PriceChangeGroup>>> GetPriceChangesInVersion(int version);
        Task<ApiResponse<PriceTable>> UpdatePriceTable(UpdatePriceTableRequest priceTable, string userName);
        Task<ApiResponse<CalculatedPriceResponse>> CalculatePrice(double distance, int containerType, int containerSize);
    }

    public class PriceTableService : IPriceTableService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly IRedisCacheService _cacheService;
        private const string PRICE_TABLE_PREFIX = "price-table";

        public PriceTableService(UnitOfWork unitOfWork, IRedisCacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
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

                // Invalidate cache after price table update
                await InvalidatePriceTableCache(existingPrice.Version ?? 0);

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
                // Validate input list
                if (priceTable == null || !priceTable.Any())
                {
                    return new ApiResponse<List<PriceTable>>(false, null,
                        "No price table entries provided",
                        "Không có bảng giá nào được cung cấp", null);
                }

                // Dictionary to track km ranges by container type and size
                var rangesByConfig = new Dictionary<string, List<(double Min, double Max)>>();

                // Validate each price table entry
                foreach (var entry in priceTable)
                {
                    // Validate km ranges
                    if (entry.MinKm < 0)
                    {
                        return new ApiResponse<List<PriceTable>>(false, null,
                            "Min Km cannot be negative",
                            "Km tối thiểu không thể âm", null);
                    }

                    if (entry.MinKm >= entry.MaxKm)
                    {
                        return new ApiResponse<List<PriceTable>>(false, null,
                            "Min Km must be less than Max Km",
                            "Km tối thiểu phải nhỏ hơn Km tối đa", null);
                    }

                    // Validate price values
                    if (entry.MinPricePerKm <= 0)
                    {
                        return new ApiResponse<List<PriceTable>>(false, null,
                            "Min Price Per Km must be greater than 0",
                            "Giá tối thiểu mỗi km phải lớn hơn 0", null);
                    }

                    if (entry.MaxPricePerKm < entry.MinPricePerKm)
                    {
                        return new ApiResponse<List<PriceTable>>(false, null,
                            "Max Price Per Km must be greater than or equal to Min Price Per Km",
                            "Giá tối đa mỗi km phải lớn hơn hoặc bằng giá tối thiểu mỗi km", null);
                    }

                    // Check for overlapping ranges
                    string configKey = $"{entry.ContainerSize}-{entry.ContainerType}";
                    if (!rangesByConfig.ContainsKey(configKey))
                    {
                        rangesByConfig[configKey] = new List<(double Min, double Max)>();
                    }

                    foreach (var existingRange in rangesByConfig[configKey])
                    {
                        if ((entry.MinKm >= existingRange.Min && entry.MinKm <= existingRange.Max) ||
                            (entry.MaxKm >= existingRange.Min && entry.MaxKm <= existingRange.Max) ||
                            (entry.MinKm <= existingRange.Min && entry.MaxKm >= existingRange.Max))
                        {
                            return new ApiResponse<List<PriceTable>>(false, null,
                                "Overlapping km ranges detected for the same container configuration",
                                "Phát hiện khoảng km chồng chéo cho cùng một cấu hình container", null);
                        }
                    }

                    rangesByConfig[configKey].Add((entry.MinKm ?? 0, entry.MaxKm ?? 0));
                }

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

                // Invalidate cache after creating new price tables
                await InvalidatePriceTableCache();

                return new ApiResponse<List<PriceTable>>(true, newPriceTables,
                    "Price table created successfully",
                    "Đã tạo bảng giá thành công", null);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ApiResponse<List<PriceTable>>(false, null,
                    "Failed to create price table",
                    "Tạo bảng giá thất bại", ex.Message);
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

                // Invalidate cache for the affected version
                if (priceTable.Version.HasValue)
                {
                    _ = InvalidatePriceTableCache(priceTable.Version.Value);
                }

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
                // Dictionary to track km ranges by container type and size
                var rangesByConfig = new Dictionary<string, List<(double Min, double Max)>>();

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

                                // Validate non-negative values
                                if (minKm < 0)
                                {
                                    return new ApiResponse<List<PriceTable>>(false, null,
                                        $"Min Km cannot be negative in row {row}",
                                        $"Km tối thiểu không thể âm ở dòng {row}", null);
                                }

                                if (minKm >= maxKm)
                                {
                                    return new ApiResponse<List<PriceTable>>(false, null,
                                        $"Min Km must be less than Max Km in row {row}",
                                        $"Km tối thiểu phải nhỏ hơn Km tối đa ở dòng {row}", null);
                                }

                                var rawContainerSize = Convert.ToInt32(worksheet.Cells[row, 3].Value);
                                int containerSize;

                                if (rawContainerSize == 20)
                                    containerSize = 1;
                                else if (rawContainerSize == 40)
                                    containerSize = 2;
                                else
                                    return new ApiResponse<List<PriceTable>>(
                                        false,
                                        null,
                                        $"Invalid container size in row {row}. Only values 20 or 40 are allowed.",
                                        $"Kích thước container không hợp lệ ở dòng {row}. Chỉ chấp nhận giá trị 20 hoặc 40.",
                                        null);

                                var rawContainerType = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                                int containerType;

                                if (string.Equals(rawContainerType, "Khô", StringComparison.OrdinalIgnoreCase))
                                    containerType = 1;
                                else if (string.Equals(rawContainerType, "Lạnh", StringComparison.OrdinalIgnoreCase))
                                    containerType = 2;
                                else
                                    return new ApiResponse<List<PriceTable>>(
                                        false,
                                        null,
                                        $"Invalid container type in row {row}. Only values 'Khô' or 'Lạnh' are allowed.",
                                        $"Loại container không hợp lệ ở dòng {row}. Chỉ chấp nhận giá trị Khô hoặc Lạnh.",
                                        null);

                                // Validate price values
                                var minPricePerKm = Convert.ToDecimal(worksheet.Cells[row, 5].Value);
                                var maxPricePerKm = Convert.ToDecimal(worksheet.Cells[row, 6].Value);

                                if (minPricePerKm <= 0)
                                {
                                    return new ApiResponse<List<PriceTable>>(false, null,
                                        $"Min Price Per Km must be greater than 0 in row {row}",
                                        $"Giá tối thiểu mỗi km phải lớn hơn 0 ở dòng {row}", null);
                                }

                                if (maxPricePerKm < minPricePerKm)
                                {
                                    return new ApiResponse<List<PriceTable>>(false, null,
                                        $"Max Price Per Km must be greater than or equal to Min Price Per Km in row {row}",
                                        $"Giá tối đa mỗi km phải lớn hơn hoặc bằng giá tối thiểu mỗi km ở dòng {row}", null);
                                }

                                // Check for overlapping ranges
                                string configKey = $"{containerSize}-{containerType}";
                                if (!rangesByConfig.ContainsKey(configKey))
                                {
                                    rangesByConfig[configKey] = new List<(double Min, double Max)>();
                                }

                                // Check for overlapping ranges in the current import
                                foreach (var existingRange in rangesByConfig[configKey])
                                {
                                    if ((minKm >= existingRange.Min && minKm <= existingRange.Max) ||
                                        (maxKm >= existingRange.Min && maxKm <= existingRange.Max) ||
                                        (minKm <= existingRange.Min && maxKm >= existingRange.Max))
                                    {
                                        return new ApiResponse<List<PriceTable>>(false, null,
                                            $"Overlapping km range in row {row}. This conflicts with another row for the same container configuration.",
                                            $"Khoảng km chồng chéo ở dòng {row}. Mục này xung đột với một dòng khác cho cùng cấu hình container.", null);
                                    }
                                }

                                // Add the range to our tracking dictionary
                                rangesByConfig[configKey].Add((minKm, maxKm));

                                var priceTable = new CreatePriceTableRequest
                                {
                                    MinKm = minKm,
                                    MaxKm = maxKm,
                                    ContainerSize = containerSize,
                                    ContainerType = containerType,
                                    MinPricePerKm = minPricePerKm,
                                    MaxPricePerKm = maxPricePerKm,
                                };
                                priceTableList.Add(priceTable);
                            }
                            catch (FormatException)
                            {
                                return new ApiResponse<List<PriceTable>>(false, null,
                                    $"Invalid data format in row {row}. Please ensure all cells contain the correct data type.",
                                    $"Định dạng dữ liệu không hợp lệ ở dòng {row}. Vui lòng đảm bảo tất cả các ô chứa đúng kiểu dữ liệu.", null);
                            }
                        }
                    }
                }

                if (priceTableList.Count == 0)
                {
                    return new ApiResponse<List<PriceTable>>(false, null,
                        "No valid price table entries found in the Excel file",
                        "Không tìm thấy bảng giá hợp lệ trong file Excel", null);
                }

                return await CreatePriceTable(priceTableList, userName);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<PriceTable>>(false, null,
                    "Failed to import price table",
                    "Nhập bảng giá thất bại", ex.Message);
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

                    worksheet.Cells[1, 1].Value = "Từ Km";
                    worksheet.Cells[1, 2].Value = "Đến Km";
                    worksheet.Cells[1, 3].Value = "Kích Thước Container (Chỉ nhập 20/40)";
                    worksheet.Cells[1, 4].Value = "Loại Container (Chỉ nhập Khô/Lạnh)";
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
                string cacheKey = BuildPriceTableCacheKey(version);

                return await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
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
                    },
                    TimeSpan.FromHours(24) // Cache for 24 hours
                );
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

        public async Task<ApiResponse<CalculatedPriceResponse>> CalculatePrice(double distance, int containerType, int containerSize)
        {
            var matchedPrice = await _unitOfWork.PriceTableRepository.GetPriceForCalculation(
            distance, containerType, containerSize);

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
                string cacheKey = BuildPriceChangesCacheKey(version);

                return await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        var priceChanges = await _unitOfWork.PriceTableRepository.GetPriceChangesInVersion(version);

                        if (priceChanges == null || !priceChanges.Any())
                        {
                            return new ApiResponse<List<PriceChangeGroup>>(
                                true,
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
                    },
                    TimeSpan.FromHours(24) // Cache for 24 hours
                );
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

        #region Helper Methods
        private string BuildPriceTableCacheKey(int? version)
        {
            return version.HasValue
                ? $"{PRICE_TABLE_PREFIX}:v{version.Value}"
                : $"{PRICE_TABLE_PREFIX}:latest";
        }

        private string BuildPriceChangesCacheKey(int version)
        {
            return $"{PRICE_TABLE_PREFIX}:changes:v{version}";
        }

        private async Task InvalidatePriceTableCache(int specificVersion = 0)
        {
            // Remove general cache
            await _cacheService.RemoveByPrefixAsync(PRICE_TABLE_PREFIX);

            // If a specific version is provided, invalidate just that version
            if (specificVersion > 0)
            {
                await _cacheService.RemoveAsync(BuildPriceTableCacheKey(specificVersion));
                await _cacheService.RemoveAsync(BuildPriceChangesCacheKey(specificVersion));
            }
        }
        #endregion
    }
}
