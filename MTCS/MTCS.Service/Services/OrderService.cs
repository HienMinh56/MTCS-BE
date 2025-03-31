using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MTCS.Common;
using MTCS.Data;
using MTCS.Data.Enums;
using MTCS.Data.Models;
using MTCS.Data.Repository;
using MTCS.Data.Request;
using MTCS.Service.Base;
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
        public async Task<BusinessResult> UpdateOrderAsync(UpdateOrderRequest model, ClaimsPrincipal claims)
        {
            try
            {
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                await _unitOfWork.BeginTransactionAsync();

                var order = await _unitOfWork.OrderRepository.GetByIdAsync(model.OrderId);
                if (order == null)
                    return new BusinessResult(Const.FAIL_READ_CODE, Const.FAIL_READ_MSG);

                order.Price = model.Price ?? order.Price;
                order.Status = model.Status ?? order.Status;
                order.ModifiedDate = DateTime.Now;
                order.ModifiedBy = userName;

                await _unitOfWork.OrderRepository.UpdateAsync(order);

                return new BusinessResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, order);
            }
            catch (Exception ex)
            {
                //await _unitOfWork.RollbackTransactionAsync();
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
    }
}
