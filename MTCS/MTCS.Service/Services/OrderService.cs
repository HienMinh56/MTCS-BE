using Google.Api;
using Google.Rpc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MTCS.Common;
using MTCS.Data;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Models;
using MTCS.Data.Repository;
using MTCS.Data.Request;
using MTCS.Service.Base;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Service.Services
{
    public interface IOrderService
    {
        Task<BusinessResult> GetOrders(
                                            string? orderId = null,
                                            string? tripid = null,
                                            string? userId = null,
                                            int? containerType = null,
                                            string? containerNumber = null,
                                            string? trackingCode = null,
                                            string? status = null,
                                            DateOnly? pickUpDate = null,
                                            DateOnly? deliveryDate = null
                                            );
        Task<BusinessResult> CreateOrder(OrderRequest orderRequest, ClaimsPrincipal claims, List<IFormFile> files, List<string> descriptions, List<string> notes);
        Task<BusinessResult> UpdateOrderAsync(string orderId, UpdateOrderRequest model, ClaimsPrincipal claims);
        Task<BusinessResult> GetOrderFiles(string orderId);
        Task<byte[]> ExportOrdersToExcelInternal(IEnumerable<Order> orders);
        Task<byte[]> ExportOrdersToExcelAsync(DateOnly fromDate, DateOnly toDate);

        Task<IEnumerable<Order>> GetAllOrders();

        Task<OrderDto> GetOrderByTrackingCodeAsync(string trackingCode);

        Task<BusinessResult> ToggleIsPayAsync(string orderId, ClaimsPrincipal claims);

        Task<BusinessResult> CancelOrderAsync(string orderId, ClaimsPrincipal claims);
    }

    public class OrderService : IOrderService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly IFirebaseStorageService _firebaseService;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

        public OrderService(UnitOfWork unitOfWork, IFirebaseStorageService firebaseService, IEmailService emailService, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _firebaseService = firebaseService;
            _emailService = emailService;
            _notificationService = notificationService;
            _notificationService = notificationService;
        }

        #region Create Order
        public async Task<BusinessResult> CreateOrder(OrderRequest orderRequest, ClaimsPrincipal claims, List<IFormFile> files, List<string> descriptions, List<string> notes)
        {
            try
            {
                var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Staff";

                // Lấy giới hạn đơn hàng theo ngày giao hàng từ config
                var config = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(7); 
                if (config == null || !int.TryParse(config.ConfigValue, out int dailyLimit))
                {
                    throw new InvalidOperationException("Chưa set cấu hình hệ thống cho đơn hàng.");
                }
                if (orderRequest.DeliveryDate == null)
                {
                    throw new ArgumentException("DeliveryDate không được để trống.");
                }

                // Kiểm tra số lượng đơn hàng đã đặt cho ngày giao hàng
                var deliveryDate = orderRequest.DeliveryDate.Value;
                var countOrdersForDeliveryDate = await _unitOfWork.OrderRepository
                    .CountOrdersByDeliveryDateAsync(deliveryDate);

                if (countOrdersForDeliveryDate >= dailyLimit)
                {
                    throw new InvalidOperationException(
                        $"Số lượng đơn hàng với ngày giao hàng {deliveryDate:dd/MM/yyyy} đã đạt giới hạn ({dailyLimit} đơn). Không thể tạo thêm."
                    );
                }

                if (string.IsNullOrEmpty(orderRequest.CompanyName))
                {
                    throw new ArgumentException("CompanyName không được để trống.");
                }

                var customer = await _unitOfWork.CustomerRepository.GetCustomerByCompanyNameAsync(orderRequest.CompanyName);

                if (customer == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy khách hàng với CompanyName đã nhập.");
                }

                if (orderRequest.ContainerType != (int)ContainerSize.Feet20 && orderRequest.ContainerType != (int)ContainerSize.Feet40)
                {
                    throw new ArgumentException("ContainerSize chỉ được nhập 20f hoặc 40f.");
                }

                if (orderRequest.DeliveryType != 1 && orderRequest.DeliveryType != 2)
                {
                    throw new ArgumentException("DeliveryType chỉ được nhập 1 (Nhập) hoặc 2 (Xuất).");
                }

                if (orderRequest.ContainerType != 1 && orderRequest.ContainerType != 2)
                {
                    throw new ArgumentException("ContainerType chỉ được nhập 1 (Lạnh) hoặc 2 (Khô).");
                }

                var trackingCode = await _unitOfWork.OrderRepository.GetNextCodeAsync();
                var orderId = Guid.NewGuid().ToString();
                var order = new Order
                {
                    OrderId = orderId,
                    TrackingCode = trackingCode,
                    CustomerId = customer.CustomerId,
                    Temperature = orderRequest.Temperature,
                    Weight = orderRequest.Weight,
                    PickUpDate = orderRequest.PickUpDate,
                    DeliveryDate = orderRequest.DeliveryDate,
                    DeliveryLocation = orderRequest.DeliveryLocation,
                    PickUpLocation = orderRequest.PickUpLocation,
                    ConReturnLocation = orderRequest.ConReturnLocation,
                    ContactPerson = orderRequest.ContactPerson,
                    ContactPhone = orderRequest.ContactPhone,
                    ContainerNumber = orderRequest.ContainerNumber,
                    Distance = orderRequest.Distance,
                    Note = orderRequest.Note,
                    Price = orderRequest.Price,
                    Status = "Pending",
                    OrderPlacer = orderRequest.OrderPlace,
                    ContainerType = orderRequest.ContainerType,
                    ContainerSize = orderRequest.ContainerSize,
                    DeliveryType = orderRequest.DeliveryType,
                    CreatedDate = DateTime.Now,
                    CreatedBy = userId,
                    IsPay = 0,
                    CompletionTime = orderRequest.CompletionTime
                };

                await _unitOfWork.OrderRepository.CreateAsync(order);

                var savedFiles = new List<OrderFile>();

                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    var fileUrl = await _firebaseService.UploadImageAsync(file);
                    var fileName = Path.GetFileName(file.FileName);
                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    string fileType = GetFileTypeFromExtension(fileExtension);

                    var orderFile = new OrderFile
                    {
                        FileId = Guid.NewGuid().ToString(),
                        OrderId = orderId,
                        FileName = fileName,
                        FileType = fileType,
                        UploadDate = DateTime.Now,
                        UploadBy = userName,
                        Description = descriptions[i],
                        Note = notes[i],
                        FileUrl = fileUrl,
                        ModifiedDate = DateOnly.FromDateTime(DateTime.Now),
                        ModifiedBy = userName,
                    };

                    await _unitOfWork.OrderFileRepository.CreateAsync(orderFile);
                    savedFiles.Add(orderFile);
                }

                var customerEmail = customer.Email;
                if (!string.IsNullOrEmpty(customerEmail))
                {
                    // Gửi email thông báo
                    await _emailService.SendEmailAsync(
                            customer.Email,                            // to
                            "Thông báo đơn hàng mới",                  // subject
                            customer.CompanyName,                      // companyName
                            order.TrackingCode                         // trackingCode
                        );
                }

                return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, new
                {
                    OrderId = orderId,
                    Files = savedFiles
                });
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.FAIL_CREATE_CODE, ex.Message);
            }
        }
        #endregion

        #region Get Orders
        public async Task<BusinessResult> GetOrders(
        string? orderId = null,
        string? tripId = null,
        string? customerId = null,
        int? containerType = null,
        string? containerNumber = null,
        string? trackingCode = null,
        string? status = null,
        DateOnly? pickUpDate = null,
        DateOnly? deliveryDate = null)
        {
            try
            {
                var orders = await _unitOfWork.OrderRepository.GetOrdersByFiltersAsync(
                    orderId,
                    tripId,
                    customerId,
                    containerType,
                    containerNumber,
                    trackingCode,
                    status,
                    pickUpDate,
                    deliveryDate);

                return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, orders);
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.FAIL_READ_CODE, ex.Message);
            }
        }
        #endregion

        #region Update Order
        public async Task<BusinessResult> UpdateOrderAsync(string orderId, UpdateOrderRequest model, ClaimsPrincipal claims)
        {
            try
            {
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Staff";
                await _unitOfWork.BeginTransactionAsync();
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
                if (order == null)
                    return new BusinessResult(Const.FAIL_READ_CODE, Const.FAIL_READ_MSG);

                if (order.Status != "Pending")
                {
                    return new BusinessResult(Const.FAIL_UPDATE_CODE, "Chỉ có thể cập nhật đơn hàng khi ở trạng thái Pending.");
                }

                order.Temperature = model.Temperature ?? order.Temperature;
                order.Note = model.Note ?? order.Note;
                order.Price = model.Price ?? order.Price;
                order.ModifiedDate = DateTime.Now;
                order.ModifiedBy = userName;
                order.ContactPerson = model.ContactPerson ?? order.ContactPerson;
                order.ContainerNumber = model.ContainerNumber ?? order.ContainerNumber;
                order.ContactPhone = model.ContactPhone ?? order.ContactPhone;
                order.OrderPlacer = model.OrderPlacer ?? order.OrderPlacer;
                order.IsPay = model.IsPay ?? order.IsPay;




                if (model.FileIdsToRemove?.Any() == true)
                {
                    OrderFile? file;
                    foreach (var url in model.FileIdsToRemove)
                    {
                        if ((file = await _unitOfWork.OrderFileRepository.GetImageByUrl(url)) is not null)
                        {
                            await _unitOfWork.OrderFileRepository.RemoveAsync(file);
                        }
                    }
                }

                if (!model.AddedFiles.IsNullOrEmpty() || model.Descriptions.Count != 0 || model.Notes.Count != 0)
                {
                    if (model.AddedFiles.Count != model.Descriptions.Count || model.AddedFiles.Count != model.Notes.Count)
                    {
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Số lượng files, descriptions và notes phải bằng nhau.");
                    }

                    //var savedFiles = new List<OrderFile>();
                    for (int i = 0; i < model.AddedFiles.Count; i++)
                    {
                        var file = model.AddedFiles[i];
                        var fileUrl = await _firebaseService.UploadImageAsync(file);

                        var fileName = Path.GetFileName(file.FileName);
                        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        string fileType = GetFileTypeFromExtension(fileExtension);

                        order.OrderFiles.Add(new OrderFile
                        {
                            FileId = Guid.NewGuid().ToString(),
                            OrderId = order.OrderId,
                            FileName = fileName,
                            FileType = fileType,
                            FileUrl = fileUrl,
                            Description = model.Descriptions[i],
                            Note = model.Notes[i],
                            UploadDate = DateTime.Now,
                            UploadBy = userName,
                            ModifiedBy = userName,
                            ModifiedDate = DateOnly.FromDateTime(DateTime.Now),
                        });
                    }
                }

                await _unitOfWork.OrderRepository.UpdateAsync(order);

                await _unitOfWork.CommitTransactionAsync();

                return new BusinessResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BusinessResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG);
            }
        }
        #endregion

        public async Task<BusinessResult> GetOrderFiles(string orderId)
        {
            try
            {
                var orderFile = _unitOfWork.OrderFileRepository.GetList(x => x.OrderId == orderId);
                return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, orderFile);
            }
            catch
            {
                return new BusinessResult(Const.FAIL_READ_CODE, Const.FAIL_READ_MSG);
            }
        }

        private string GetFileTypeFromExtension(string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".pdf":
                    return "PDF Document";
                case ".doc":
                case ".docx":
                    return "Word Document";
                case ".xls":
                case ".xlsx":
                    return "Excel Spreadsheet";
                case ".ppt":
                case ".pptx":
                    return "PowerPoint Presentation";
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                    return "Image";
                case ".txt":
                    return "Text Document";
                case ".zip":
                case ".rar":
                    return "Archive";
                default:
                    return "Unknown";
            }
        }

        public async Task<byte[]> ExportOrdersToExcelInternal(IEnumerable<Order> orders)
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.Commercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Orders");

                    // Các header cột Excel
                    worksheet.Cells[1, 1].Value = "Ngày";
                    worksheet.Cells[1, 2].Value = "Mã đơn hàng";
                    worksheet.Cells[1, 3].Value = "Khách Hàng";
                    worksheet.Cells[1, 4].Value = "Khối lượng";
                    worksheet.Cells[1, 5].Value = "Nhập/Xuất";
                    worksheet.Cells[1, 6].Value = "Ghi Chú";
                    worksheet.Cells[1, 7].Value = "Số Cont";
                    worksheet.Cells[1, 8].Value = "Kích thước cont";
                    worksheet.Cells[1, 9].Value = "Loại cont";
                    worksheet.Cells[1, 10].Value = "Địa chỉ Lấy Cont";
                    worksheet.Cells[1, 11].Value = "Địa  chỉ giao";
                    worksheet.Cells[1, 12].Value = "Địa chỉ trả cont";
                    worksheet.Cells[1, 13].Value = "Cước vận chuyển";
                    worksheet.Cells[1, 14].Value = "Tình trạng thanh toán";
                    worksheet.Cells[1, 15].Value = "Tài xế";
                    worksheet.Cells[1, 16].Value = "Biển số đầu kéo";
                    worksheet.Cells[1, 17].Value = "Biển số Rơ-mooc";
                    worksheet.Cells[1, 18].Value = "Nhân viên bán hàng";

                    int row = 2;
                    foreach (var order in orders)
                    {
                        // Kiểm tra nếu order có status "complete"
                        if (order.Status != "Completed")
                            continue;

                        var customer = order.Customer;
                        var trips = order.Trips.Where(t => t.Status == "completed");
                        if (!trips.Any())
                            continue;

                        var staff = await _unitOfWork.InternalUserRepository.GetUserByIdAsync(order.CreatedBy);

                        if (staff == null)
                        {
                            throw new Exception("Không tìm thấy người tạo đơn hàng!");
                        }

                        foreach (var trip in trips)
                        {
                            var driver = trip.Driver;
                            var tractor = trip.Tractor;
                            var trailer = trip.Trailer;


                            worksheet.Cells[row, 1, row, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            worksheet.Cells[row, 1].Value = order.DeliveryDate?.ToString("yyyy-MM-dd");
                            worksheet.Cells[row, 2].Value = order.TrackingCode;
                            worksheet.Cells[row, 3].Value = customer.CompanyName;
                            worksheet.Cells[row, 4].Value = order.Weight;
                            worksheet.Cells[row, 5].Value = order.DeliveryType == 1 ? "N" : order.DeliveryType == 2 ? "X" : "";
                            worksheet.Cells[row, 6].Value = order.Note;
                            worksheet.Cells[row, 7].Value = order.ContainerNumber;
                            worksheet.Cells[row, 8].Value = order.ContainerSize + "f";
                            worksheet.Cells[row, 9].Value = order.ContainerType == 1 ? "DC" : order.DeliveryType == 2 ? "RE" : ""; 
                            worksheet.Cells[row, 10].Value = order.PickUpLocation;
                            worksheet.Cells[row, 11].Value = order.DeliveryLocation;
                            worksheet.Cells[row, 12].Value = order.ConReturnLocation;
                            worksheet.Cells[row, 13].Value = order.Price;
                            worksheet.Cells[row, 13].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 14].Value = order.IsPay == 0 ? "Chưa thanh toán" : order.IsPay == 1 ? "Đã thanh toán" : "";
                            worksheet.Cells[row, 15].Value = driver?.FullName ?? ""; 
                            worksheet.Cells[row, 16].Value = tractor?.LicensePlate ?? "";
                            worksheet.Cells[row, 17].Value = trailer?.LicensePlate ?? "";
                            worksheet.Cells[row, 18].Value = staff.FullName;

                            row++;
                        }
                    }

                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    var stream = new MemoryStream();
                    await package.SaveAsAsync(stream);

                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while exporting orders to Excel", ex);
            }
        }

        public async Task<IEnumerable<Order>> GetAllOrders()
        {

            var orders = await _unitOfWork.OrderRepository.GetAllOrdersAsync();
            return orders.ToList();
        }

        public async Task<byte[]> ExportOrdersToExcelAsync(DateOnly fromDate, DateOnly toDate)
        {
            var fromDateTime = fromDate.ToDateTime(TimeOnly.MinValue);
            var toDateTime = toDate.ToDateTime(TimeOnly.MaxValue);

            var orders = await _unitOfWork.OrderRepository.GetQueryable()
                 .Where(o => o.DeliveryDate.HasValue &&
                 o.DeliveryDate.Value >= fromDate &&
                 o.DeliveryDate.Value <= toDate)
                 .Include(o => o.Customer)
                 .Include(o => o.Trips).ThenInclude(t => t.Driver)
                 .Include(o => o.Trips).ThenInclude(t => t.Tractor)
                 .Include(o => o.Trips).ThenInclude(t => t.Trailer)
                 .ToListAsync();

            return await ExportOrdersToExcelInternal(orders);
        }

        #region Get order by tracking code for customer
        public async Task<OrderDto> GetOrderByTrackingCodeAsync(string trackingCode)
        {
            var order = await _unitOfWork.OrderRepository.GetByTrackingCodeAsync(trackingCode);
            if (order == null) return null;

            return new OrderDto
            {
                OrderId = order.OrderId,
                TrackingCode = order.TrackingCode,
                CustomerName = order.Customer?.CompanyName,
                PickUpDate = order.PickUpDate,
                DeliveryDate = order.DeliveryDate,
                Status = order.Status,
                PickUpLocation = order.PickUpLocation,
                DeliveryLocation = order.DeliveryLocation,
                Trips = order.Trips.Where(t => t.Status != "canceled").Select(t => new TripDto
                {
                    TripId = t.TripId,
                    OrderId = t.OrderId,
                    DriverId = t.DriverId,
                    TractorId = t.TractorId,
                    TrailerId = t.TrailerId,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    Status = t.Status,
                    MatchTime = t.MatchTime,
                    Driver = t.Driver == null ? null : new DriverDto
                    {
                        DriverId = t.Driver.DriverId,
                        FullName = t.Driver.FullName,
                        PhoneNumber = t.Driver.PhoneNumber
                    },
                    Tractor = t.Tractor == null ? null : new TractorDto
                    {
                        TractorId = t.Tractor.TractorId,
                        LicensePlate = t.Tractor.LicensePlate
                    },
                    Trailer = t.Trailer == null ? null : new TrailerDto
                    {
                        TrailerId = t.Trailer.TrailerId,
                        LicensePlate = t.Trailer.LicensePlate
                    },
                    TripStatusHistories = t.TripStatusHistories.Select(h => new TripStatusHistoryDto
                    {
                        HistoryId = h.HistoryId,
                        TripId = h.TripId,
                        StatusId = h.StatusId,
                        StatusName = h.Status?.StatusName,
                        StartTime = h.StartTime
                    }).ToList()
                }).ToList()
            };
        }
        #endregion

        public async Task<BusinessResult> ToggleIsPayAsync(string orderId, ClaimsPrincipal claims)
        {
            try
            {
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Staff";

                await _unitOfWork.BeginTransactionAsync();

                var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);

                if (order == null)
                    return new BusinessResult(Const.FAIL_READ_CODE, Const.FAIL_READ_MSG); // Nếu không tìm thấy đơn hàng

                order.IsPay = order.IsPay == 0 ? 1 : order.IsPay;
                order.ModifiedDate = DateTime.Now;
                order.ModifiedBy = userName;


                await _unitOfWork.OrderRepository.UpdateAsync(order);

                await _unitOfWork.CommitTransactionAsync();

                return new BusinessResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(); // Quay lại nếu có lỗi
                return new BusinessResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG);
            }
        }

        public async Task<BusinessResult> CancelOrderAsync(string orderId, ClaimsPrincipal claims)
        {
            var order = await _unitOfWork.OrderRepository.GetOrderWithTripsAsync(orderId);
            var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Staff";

            if (order == null)
            {
                return new BusinessResult { Status = 0, Message = "Không tìm thấy đơn hàng." };
            }

            if (order.Status != "Pending" && order.Status != "Scheduled")
            {
                return new BusinessResult { Status = 0, Message = "Chỉ có thể hủy đơn hàng khi trạng thái là Pending hoặc Scheduled." };
            }

            order.Status = "canceled";
            order.ModifiedDate = DateTime.UtcNow;
            order.ModifiedBy = userName;

            foreach (var trip in order.Trips)
            {
                trip.Status = "canceled";
                trip.Note = "Đơn hàng bị hủy";

                if (!string.IsNullOrEmpty(trip.DriverId) && order.CompletionTime.HasValue)
                {
                    var driverId = trip.DriverId;
                    var completionMinutes = (int)order.CompletionTime.Value.ToTimeSpan().TotalMinutes;

                    var deliveryDate = order.DeliveryDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

                    // Trừ thời gian ngày
                    var dailyRecord = await _unitOfWork.DriverDailyWorkingTimeRepository
                        .GetByDriverIdAndDateAsync(driverId, deliveryDate);

                    if (dailyRecord != null)
                    {
                        dailyRecord.TotalTime = Math.Max(0, (dailyRecord.TotalTime ?? 0) - completionMinutes);
                        dailyRecord.ModifiedDate = DateTime.UtcNow;
                        dailyRecord.ModifiedBy = userName;
                        await _unitOfWork.DriverDailyWorkingTimeRepository.UpdateAsync(dailyRecord);
                    }

                    // Tính tuần (bắt đầu từ thứ 2)
                    var deliveryDateTime = deliveryDate.ToDateTime(TimeOnly.MinValue);
                    var startOfWeek = DateOnly.FromDateTime(deliveryDateTime.AddDays(-(int)deliveryDateTime.DayOfWeek + 1));
                    var endOfWeek = startOfWeek.AddDays(6);

                    // Trừ thời gian tuần
                    var weeklyRecord = await _unitOfWork.DriverWeeklySummaryRepository
                        .GetByDriverIdAndWeekAsync(driverId, startOfWeek, endOfWeek);

                    if (weeklyRecord != null)
                    {
                        weeklyRecord.TotalHours = Math.Max(0, (weeklyRecord.TotalHours ?? 0) - completionMinutes);
                        await _unitOfWork.DriverWeeklySummaryRepository.UpdateAsync(weeklyRecord);
                    }
                }
            }

            await _unitOfWork.OrderRepository.UpdateAsync(order);

            // Gửi thông báo cho tài xế
            foreach (var trip in order.Trips)
            {
                if (!string.IsNullOrEmpty(trip.DriverId))
                {
                    await _notificationService.SendNotificationAsync(
                        trip.DriverId,
                        "Đơn hàng đã bị hủy",
                        $"Chuyến {trip.TripId} đã bị hủy vì đơn hàng {order.OrderId} bị huỷ.",
                        "Hệ thống"
                    );
                }
            }

            // Gửi email cho khách hàng
            var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(order.CustomerId);
            if (customer != null && !string.IsNullOrEmpty(customer.Email))
            {
                await _emailService.SendEmailCancelAsync(
                    customer.Email,
                    "Đơn hàng của bạn đã bị huỷ",
                    customer.CompanyName,
                    order.TrackingCode
                );
            }

            return new BusinessResult
            {
                Status = 1,
                Message = "Đơn hàng đã được hủy thành công và thời gian làm việc của tài xế đã được cập nhật."
            };
        }
    }
}

