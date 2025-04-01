using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MTCS.Common;
using MTCS.Data;
using MTCS.Data.Enums;
using MTCS.Data.Models;
using MTCS.Data.Repository;
using MTCS.Data.Request;
using MTCS.Service.Base;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
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
        Task<BusinessResult> UpdateOrderAsync(UpdateOrderRequest model, ClaimsPrincipal claims);
        Task<BusinessResult> GetOrderFiles(string orderId);
        Task<byte[]> ExportOrdersToExcelAsync(IEnumerable<Order> orders);
        Task<IEnumerable<Order>> GetAllOrders();

    }

    public class OrderService : IOrderService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly IFirebaseStorageService _firebaseService;

        public OrderService(UnitOfWork unitOfWork, IFirebaseStorageService firebaseService)
        {
            _unitOfWork = unitOfWork;
            _firebaseService = firebaseService;

        }

        #region Create Order
        public async Task<BusinessResult> CreateOrder(OrderRequest orderRequest, ClaimsPrincipal claims, List<IFormFile> files, List<string> descriptions, List<string> notes)
        {
            try
            {
                var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";



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
                    throw new ArgumentException("ContainerType chỉ được nhập 20 hoặc 40.");
                }

                if (orderRequest.DeliveryType != 1 && orderRequest.DeliveryType != 2)
                {
                    throw new ArgumentException("DeliveryType chỉ được nhập 1 (Nhập) hoặc 2 (Xuất).");
                }

                await _unitOfWork.BeginTransactionAsync();
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
                    OrderPlacer = userName,
                    ContainerType = orderRequest.ContainerType,
                    DeliveryType = orderRequest.DeliveryType,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = userId,
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
                        UploadDate = DateTime.UtcNow,
                        UploadBy = userName,
                        Description = descriptions[i],
                        Note = notes[i],
                        FileUrl = fileUrl,
                        ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        ModifiedBy = userName,
                    };

                    await _unitOfWork.OrderFileRepository.CreateAsync(orderFile);
                    savedFiles.Add(orderFile);
                }

                return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, new
                {
                    OrderId = orderId,
                    Files = savedFiles
                });
            }
            catch (Exception ex)
            {
                //await _unitOfWork.RollbackTransactionAsync();
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

                var query = _unitOfWork.OrderRepository.GetQueryable();

                if (!string.IsNullOrEmpty(orderId))
                    query = query.Where(o => o.OrderId == orderId);
                if (!string.IsNullOrEmpty(tripId))
                    query = query.Include(o => o.Trips).Where(o => o.Trips.Any(o => o.TripId == tripId));
                if (!string.IsNullOrEmpty(customerId))
                    query = query.Where(o => o.CustomerId == customerId);

                if (containerType.HasValue)
                    query = query.Where(o => o.ContainerType == containerType.Value);

                if (!string.IsNullOrEmpty(containerNumber))
                    query = query.Where(o => o.ContainerNumber == containerNumber);

                if (!string.IsNullOrEmpty(trackingCode))
                    query = query.Where(o => o.TrackingCode == trackingCode);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(o => o.Status == status);

                if (pickUpDate.HasValue)
                    query = query.Where(o => o.PickUpDate == DateOnly.FromDateTime(pickUpDate.Value.ToDateTime(TimeOnly.MinValue)));

                if (deliveryDate.HasValue)
                    query = query.Where(o => o.DeliveryDate == DateOnly.FromDateTime(deliveryDate.Value.ToDateTime(TimeOnly.MinValue)));


                query = query.OrderBy(o => o.CreatedDate);

                var orders = await query.ToListAsync();

                return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, orders);
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.FAIL_READ_CODE, ex.Message);
            }
        }
        #endregion

        #region Update Order
        public async Task<BusinessResult> UpdateOrderAsync(UpdateOrderRequest model, ClaimsPrincipal claims)
        {
            try
            {
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                await _unitOfWork.BeginTransactionAsync();

                var order = await _unitOfWork.OrderRepository.GetByIdAsync(model.OrderId);
                if (order == null)
                    return new BusinessResult(Const.FAIL_READ_CODE, Const.FAIL_READ_MSG);


                if (order.Status != "Pending")
                    return new BusinessResult(Const.FAIL_UPDATE_CODE, "Không thể update khi đơn hàng đã bắt đầu.");

                order.Note = string.IsNullOrEmpty(model.Note) ? order.Note : model.Note;
                order.ContainerType = model.ContainerType ?? order.ContainerType;
                order.PickUpLocation = string.IsNullOrEmpty(model.PickUpLocation) ? order.PickUpLocation : model.PickUpLocation;
                order.DeliveryLocation = string.IsNullOrEmpty(model.DeliveryLocation) ? order.DeliveryLocation : model.DeliveryLocation;
                order.ConReturnLocation = string.IsNullOrEmpty(model.ConReturnLocation) ? order.ConReturnLocation : model.ConReturnLocation;
                order.DeliveryType = model.DeliveryType ?? order.DeliveryType;
                order.Price = model.Price ?? order.Price;
                order.ContainerNumber = string.IsNullOrEmpty(model.ContainerNumber) ? order.ContainerNumber : model.ContainerNumber;
                order.ContactPerson = string.IsNullOrEmpty(model.ContactPerson) ? order.ContactPerson : model.ContactPerson;
                order.ContactPhone = string.IsNullOrEmpty(model.ContactPhone) ? order.ContactPhone : model.ContactPhone;
                order.OrderPlacer = string.IsNullOrEmpty(model.OrderPlacer) ? order.OrderPlacer : model.OrderPlacer;
                order.Distance = model.Distance ?? order.Distance;
                order.ModifiedDate = DateTime.Now;
                order.ModifiedBy = userName;

                await _unitOfWork.OrderRepository.UpdateAsync(order);
                await _unitOfWork.CommitTransactionAsync();

                return new BusinessResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, order);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BusinessResult(Const.FAIL_UPDATE_CODE, ex.Message);
            }
        }
        #endregion

        public async Task<BusinessResult> GetOrderFiles(string orderId)
        {
            try
            {
                var contractFiles = _unitOfWork.OrderFileRepository.GetList(x => x.OrderId == orderId);
                return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, contractFiles);
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



        public async Task<byte[]> ExportOrdersToExcelAsync(IEnumerable<Order> orders)
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.Commercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Orders");

                    // Đặt tiêu đề cột
                    worksheet.Cells[1, 1].Value = "OrderId";
                    worksheet.Cells[1, 2].Value = "TrackingCode";
                    worksheet.Cells[1, 3].Value = "CustomerId";
                    worksheet.Cells[1, 4].Value = "Temperature";
                    worksheet.Cells[1, 5].Value = "Weight";
                    worksheet.Cells[1, 6].Value = "PickUpDate";
                    worksheet.Cells[1, 7].Value = "DeliveryDate";
                    worksheet.Cells[1, 8].Value = "Status";
                    worksheet.Cells[1, 9].Value = "Note";
                    worksheet.Cells[1, 10].Value = "CreatedDate";
                    worksheet.Cells[1, 11].Value = "CreatedBy";
                    worksheet.Cells[1, 12].Value = "ModifiedDate";
                    worksheet.Cells[1, 13].Value = "ModifiedBy";

                    // Điền dữ liệu vào các dòng
                    int row = 2;
                    foreach (var order in orders)
                    {
                        worksheet.Cells[row, 1].Value = order.OrderId;
                        worksheet.Cells[row, 2].Value = order.TrackingCode;
                        worksheet.Cells[row, 3].Value = order.CustomerId;
                        worksheet.Cells[row, 4].Value = order.Temperature;
                        worksheet.Cells[row, 5].Value = order.Weight;
                        worksheet.Cells[row, 6].Value = order.PickUpDate?.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 7].Value = order.DeliveryDate?.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 8].Value = order.Status;
                        worksheet.Cells[row, 9].Value = order.Note;
                        worksheet.Cells[row, 10].Value = order.CreatedDate?.ToString("yyyy-MM-dd HH:mm:ss");
                        worksheet.Cells[row, 11].Value = order.CreatedBy;
                        worksheet.Cells[row, 12].Value = order.ModifiedDate?.ToString("yyyy-MM-dd HH:mm:ss");
                        worksheet.Cells[row, 13].Value = order.ModifiedBy;

                        row++;
                    }

                    // Thiết lập AutoFit cho các cột
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    // Lưu file Excel vào bộ nhớ
                    var stream = new MemoryStream();
                    await package.SaveAsAsync(stream);

                    // Trả về dữ liệu Excel dưới dạng mảng byte
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                // Ném ra exception nếu có lỗi
                throw new Exception("An error occurred while exporting orders to Excel", ex);
            }
        }

        public async Task<IEnumerable<Order>> GetAllOrders()
        {
            try
            {
                var orders = await _unitOfWork.OrderRepository.GetQueryable().ToListAsync();
                return orders;  // Trả về danh sách đơn hàng
            }
            catch (Exception ex)
            {
                // Log exception nếu cần
                return Enumerable.Empty<Order>(); // Trả về một danh sách rỗng nếu có lỗi
            }
        }
    }
}
