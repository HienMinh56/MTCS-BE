using MTCS.Common;
using MTCS.Data.Models;
using MTCS.Data;
using MTCS.Service.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using MTCS.Data.Request;
using Microsoft.AspNetCore.Http;
using MTCS.Data.Enums;
using MTCS.Data.Response;
using Microsoft.IdentityModel.Tokens;

namespace MTCS.Service.Services
{
    public interface IOrderDetailService
    {
        Task<BusinessResult> CreateOrderDetailAsync(OrderDetailRequest request, ClaimsPrincipal claims, List<IFormFile> files, List<string> descriptions, List<string> notes);
        Task<List<OrderDetailResponse>> GetOrderDetailsAsync(
        string? orderId,
        string? containerNumber,
        DateOnly? pickUpDate,
        DateOnly? deliveryDate,
        string? driverId,
        string? tripId
        );
        Task<BusinessResult> UpdateOrderDetailAsync(string orderDetailId, UpdateOrderDetailRequest model, ClaimsPrincipal claims);
    }

    public class OrderDetailService : IOrderDetailService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly IFirebaseStorageService _firebaseStorageService;


        public OrderDetailService(UnitOfWork unitOfWork, IFirebaseStorageService firebaseStorageService)
        {
            _unitOfWork = unitOfWork;
            _firebaseStorageService = firebaseStorageService;
        }

        public async Task<BusinessResult> CreateOrderDetailAsync(OrderDetailRequest orderDetailrequest, ClaimsPrincipal claims, List<IFormFile> files, List<string> descriptions, List<string> notes)
        {
            try
            {
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Staff";

                // Kiểm tra đơn hàng có tồn tại không
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderDetailrequest.OrderId);
                if (order == null)
                {
                    return new BusinessResult(Const.FAIL_CREATE_CODE, "Không tìm thấy đơn hàng.");
                }

                if (orderDetailrequest.ContainerType != (int)ContainerSize.Feet20 && orderDetailrequest.ContainerType != (int)ContainerSize.Feet40)
                {
                    throw new ArgumentException("ContainerSize chỉ được nhập 20f hoặc 40f.");
                }

                if (orderDetailrequest.ContainerType != 1 && orderDetailrequest.ContainerType != 2)
                {
                    throw new ArgumentException("ContainerType chỉ được nhập 1 (Lạnh) hoặc 2 (Khô).");
                }


                var trackingCode = order.TrackingCode;
                var trimmedTrackingCode = trackingCode.Substring(3);

                var currentCount = await _unitOfWork.OrderDetailRepository
                    .CountAsync(od => od.OrderId == order.OrderId);

                var detailSequence = currentCount + 1;

                var orderDetailId = $"DET{trimmedTrackingCode}{detailSequence}";

                var orderDetail = new OrderDetail
                {
                    OrderDetailId = orderDetailId,
                    OrderId = orderDetailrequest.OrderId,
                    ContainerNumber = orderDetailrequest.ContainerNumber,
                    ContainerType = orderDetailrequest.ContainerType,
                    ContainerSize = orderDetailrequest.ContainerSize,
                    Weight = orderDetailrequest.Weight,
                    Temperature = orderDetailrequest.Temperature,
                    PickUpLocation = orderDetailrequest.PickUpLocation,
                    DeliveryLocation = orderDetailrequest.DeliveryLocation,
                    ConReturnLocation = orderDetailrequest.ConReturnLocation,
                    CompletionTime = orderDetailrequest.CompletionTime,
                    Distance = orderDetailrequest.Distance,
                    PickUpDate = orderDetailrequest.PickUpDate,
                    DeliveryDate = orderDetailrequest.DeliveryDate,
                    Status = "Pending",
                };

                await _unitOfWork.OrderDetailRepository.CreateAsync(orderDetail);

                var savedFiles = new List<OrderDetailFile>();

                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    var fileUrl = await _firebaseStorageService.UploadImageAsync(file);
                    var fileName = Path.GetFileName(file.FileName);
                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    string fileType = GetFileTypeFromExtension(fileExtension);

                    var orderDetailFile = new OrderDetailFile
                    {
                        FileId = Guid.NewGuid().ToString(),
                        OrderDetailId = orderDetail.OrderDetailId,
                        Description = descriptions[i],
                        Note = notes[i],
                        FileName = fileName,
                        FileType = fileType,
                        UploadDate = DateTime.Now,
                        UploadBy = userName,
                        FileUrl = fileUrl,
                        ModifiedDate = DateOnly.FromDateTime(DateTime.Now),
                        ModifiedBy = userName,
                    };


                    await _unitOfWork.OrderDetailFileRepository.CreateAsync(orderDetailFile);
                    savedFiles.Add(orderDetailFile);
                }

                order.Quantity = (order.Quantity ?? 0) + 1;
                await _unitOfWork.OrderRepository.UpdateAsync(order);

                return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, new
                {
                    OrderDetailId = orderDetailId,
                    Files = savedFiles
                });
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.FAIL_CREATE_CODE, ex.Message);
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

        public async Task<List<OrderDetailResponse>> GetOrderDetailsAsync(
         string? orderId,
         string? containerNumber,
         DateOnly? pickUpDate,
         DateOnly? deliveryDate,
         string? driverId,
         string? tripId)
        {
            var orderDetails = await _unitOfWork.OrderDetailRepository
                                                .GetOrderDetailsByFiltersAsync(orderId, containerNumber, pickUpDate, deliveryDate, driverId, tripId);

            return orderDetails.Select(od => new OrderDetailResponse
            {
                OrderDetailId = od.OrderDetailId,
                OrderId = od.OrderId,
                ContainerNumber = od.ContainerNumber,
                ContainerType = (int)od.ContainerType,
                ContainerSize = (int)od.ContainerSize,
                Weight = (double)od.Weight,
                Temperature = od.Temperature.HasValue ? (int)od.Temperature : null,
                PickUpDate = (DateOnly)od.PickUpDate,
                PickUpLocation = od.PickUpLocation,
                DeliveryLocation = od.DeliveryLocation,
                ConReturnLocation = od.ConReturnLocation,
                Distance = od.Distance,
                CompletionTime = od.CompletionTime,
                DeliveryDate = (DateOnly)od.DeliveryDate,
                Status = od.Status,
                Note = od.Order.Note,
                ContactPerson = od.Order.ContactPerson,
                ContactPhone = od.Order.ContactPhone,
                OrderPlacer = od.Order.OrderPlacer,
                Files = od.OrderDetailFiles?.Select(f => new OrderDetailFileData
                {
                    FileId = f.FileId,
                    FileName = f.FileName,
                    FileUrl = f.FileUrl,
                    FileType = f.FileType,
                    Description = f.Description,
                    Note = f.Note,
                    UploadDate = (DateTime)f.UploadDate,
                    UploadBy = f.UploadBy
                }).ToList()
            }).ToList();
        }

        #region Update OrderDetail
        public async Task<BusinessResult> UpdateOrderDetailAsync(string orderDetailId, UpdateOrderDetailRequest model, ClaimsPrincipal claims)
        {
            try
            {
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Staff";
                await _unitOfWork.BeginTransactionAsync();
                var orderDetail = await _unitOfWork.OrderDetailRepository.GetByIdAsync(orderDetailId);
                if (orderDetail == null)
                    return new BusinessResult(Const.FAIL_READ_CODE, Const.FAIL_READ_MSG);

                if (orderDetail.Status != "Pending")
                {
                    return new BusinessResult(Const.FAIL_UPDATE_CODE, "Chỉ có thể cập nhật đơn hàng khi ở trạng thái Pending.");
                }

                orderDetail.Temperature = model.Temperature ?? orderDetail.Temperature;
                orderDetail.ContainerNumber = model.ContainerNumber ?? orderDetail.ContainerNumber;
                orderDetail.ContainerType = model.ContainerType ?? orderDetail.ContainerType;
                orderDetail.ContainerSize = model.ContainerSize ?? orderDetail.ContainerSize;
                orderDetail.Weight = model.Weight ?? orderDetail.Weight;
                orderDetail.PickUpLocation = model.PickUpLocation ?? orderDetail.PickUpLocation;
                orderDetail.DeliveryLocation = model.DeliveryLocation ?? orderDetail.DeliveryLocation;
                orderDetail.ConReturnLocation = model.ConReturnLocation ?? orderDetail.ConReturnLocation;
                orderDetail.CompletionTime = model.CompletionTime ?? orderDetail.CompletionTime;
                orderDetail.Distance = model.Distance ?? orderDetail.Distance;

                if (model.FileIdsToRemove?.Any() == true)
                {
                    OrderDetailFile? file;
                    foreach (var url in model.FileIdsToRemove)
                    {
                        if ((file = await _unitOfWork.OrderDetailFileRepository.GetImageByUrl(url)) is not null)
                        {
                            await _unitOfWork.OrderDetailFileRepository.RemoveAsync(file);
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
                        var fileUrl = await _firebaseStorageService.UploadImageAsync(file);

                        var fileName = Path.GetFileName(file.FileName);
                        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        string fileType = GetFileTypeFromExtension(fileExtension);

                        orderDetail.OrderDetailFiles.Add(new OrderDetailFile
                        {
                            FileId = Guid.NewGuid().ToString(),
                            OrderDetailId = orderDetail.OrderDetailId,
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

                await _unitOfWork.OrderDetailRepository.UpdateAsync(orderDetail);
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
    }
}