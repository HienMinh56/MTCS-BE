using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MTCS.Common;
using MTCS.Data;
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

                order.Temperature = model.Temperature ?? order.Temperature;  
                order.Note = model.Note ?? order.Note;  
                order.Price = model.Price ?? order.Price; 
                order.Status = model.Status ?? order.Status;  
                order.ModifiedDate = DateTime.Now;
                order.ModifiedBy = userName;
                order.ContactPerson = model.ContactPerson ?? order.ContactPerson; 
                order.ContainerNumber = model.ContainerNumber ?? order.ContainerNumber;  
                order.ContactPhone = model.ContactPhone ?? order.ContactPhone; 
                order.OrderPlacer = model.OrderPlacer ?? order.OrderPlacer; 
                order.IsPay = model.IsPay ?? order.IsPay;


                if (model.FileIdsToRemove?.Any() == true)
                {
                    foreach (var fileId in model.FileIdsToRemove)
                    {
                        var file = _unitOfWork.ContractFileRepository.GetById(fileId);
                        if (file != null)
                        {
                            await _unitOfWork.ContractFileRepository.RemoveAsync(file);
                        }
                    }
                }

                if (!model.AddedFiles.IsNullOrEmpty() || model.Descriptions.Count != 0 || model.Notes.Count != 0)
                {
                    if (model.AddedFiles.Count != model.Descriptions.Count || model.AddedFiles.Count != model.Notes.Count)
                    {
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Số lượng files, descriptions và notes phải bằng nhau.");
                    }
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

                    await _unitOfWork.OrderRepository.UpdateAsync(order);
                }


                //await _repository.CommitTransactionAsync();
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

               
                    int row = 2;
                    foreach (var order in orders)
                    {

                        var customer = order.Customer;
                        var priceCell = worksheet.Cells[row, 13];
                        worksheet.Cells[row, 1, row, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        worksheet.Cells[row, 1].Value = order.DeliveryDate?.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 2].Value = order.TrackingCode;
                        worksheet.Cells[row, 3].Value = customer.CompanyName;
                        worksheet.Cells[row, 4].Value = order.Weight;
                        worksheet.Cells[row, 5].Value = order.DeliveryType == 1 ? "N" : order.DeliveryType == 2 ? "X" : "";
                        worksheet.Cells[row, 6].Value = order.Note;
                        worksheet.Cells[row, 7].Value = order.ContainerNumber;
                        worksheet.Cells[row, 8].Value = order.ContainerSize +"f";
                        worksheet.Cells[row, 9].Value = order.ContainerType == 1 ? "DC" : order.DeliveryType == 2 ? "RE" : ""; //DC - Dry container, RE (Reefer)
                        worksheet.Cells[row, 10].Value = order.PickUpLocation;
                        worksheet.Cells[row, 11].Value = order.DeliveryLocation;
                        worksheet.Cells[row, 12].Value = order.ConReturnLocation;
                        priceCell.Value = order.Price;
                        priceCell.Style.Numberformat.Format = "#,##0";

                        row++;
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
            var orders = await _unitOfWork.OrderRepository.GetOrdersByDateRangeAsync(fromDate, toDate);
            return await ExportOrdersToExcelInternal(orders);
        }

    }
}
