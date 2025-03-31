using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.IdentityModel.Tokens;
using MTCS.Common;
using MTCS.Data;
using MTCS.Data.Models;
using MTCS.Data.Request;
using MTCS.Service.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IdentityModel.Tokens.Jwt;
using System.IO.Compression;
using System.Security.Claims;

namespace MTCS.Service.Services
{
    public interface IContractService
    {
        Task<BusinessResult> GetContractFiles(string contractId);
        Task<BusinessResult> GetContract();
        Task<BusinessResult> GetContract(string contractId);
        Task<BusinessResult> CreateContract(ContractRequest contractRequest, List<IFormFile> files, List<string> descriptions, List<string> notes, ClaimsPrincipal claims);
        Task<BusinessResult> SendSignedContract(string contractId, List<string> descriptions, List<string> notes, List<IFormFile> files, ClaimsPrincipal claims);
        Task<BusinessResult> UpdateContractAsync(UpdateContractRequest model, ClaimsPrincipal claims);
        Task<BusinessResult> DownloadSelectedFilesAsZip(List<string> fileIds);
        Task<BusinessResult> DeleteContract(string contractId, ClaimsPrincipal claims);
    }

    public class ContractService : IContractService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly IFirebaseStorageService _firebaseService;

        public ContractService(UnitOfWork repository, IFirebaseStorageService firebaseService)
        {
            _unitOfWork = repository;
            _firebaseService = firebaseService;
        }



        #region Create Contract
        /// <summary>
        /// Create Contract
        /// </summary>
        /// <param name="contractRequest"></param>
        /// <param name="files"></param>
        /// <param name="descriptions"></param>
        /// <param name="notes"></param>
        /// <param name="claims"></param>
        /// <returns></returns>
        public async Task<BusinessResult> CreateContract(ContractRequest contractRequest, List<IFormFile> files, List<string> descriptions, List<string> notes, ClaimsPrincipal claims)
        {
            try
            {
                var customerId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                await _unitOfWork.BeginTransactionAsync();
                var contractId = await _unitOfWork.ContractRepository.GetNextContractIdAsync();

                var contract = new Data.Models.Contract
                {
                    ContractId = contractId,
                    CustomerId = customerId,
                    Summary = contractRequest.Summary,
                    SignedBy = contractRequest.SignedBy,
                    SignedTime = contractRequest.SignedTime,
                    StartDate = contractRequest.StartDate,
                    EndDate = contractRequest.EndDate,
                    Status = contractRequest.Status,
                    CreatedDate = DateTime.Now,
                    CreatedBy = userName
                };

                await _unitOfWork.ContractRepository.CreateAsync(contract);
                var savedFiles = new List<ContractFile>();

                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    var fileUrl = await _firebaseService.UploadImageAsync(file);
                    var fileName = Path.GetFileName(file.FileName);
                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    string fileType = GetFileTypeFromExtension(fileExtension);

                    var contractFile = new ContractFile
                    {
                        FileId = Guid.NewGuid().ToString(),
                        ContractId = contractId,
                        FileName = fileName,
                        FileType = fileType,
                        UploadDate = DateTime.UtcNow,
                        UploadBy = userName,
                        Description = descriptions[i], // Lấy từ danh sách descriptions
                        Note = notes[i],               // Lấy từ danh sách notes
                        FileUrl = fileUrl,
                        ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        ModifiedBy = userName,
                    };

                    await _unitOfWork.ContractFileRepository.CreateAsync(contractFile);
                    savedFiles.Add(contractFile);
                }

                return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, new
                {
                    ContractId = contractId,
                    Files = savedFiles
                });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
        #endregion

        #region Delete Contract
        /// <summary>
        /// Change status of contract
        /// </summary>
        /// <param name="contractId"></param>
        /// <param name="claims"></param>
        /// <returns></returns>
        public async Task<BusinessResult> DeleteContract(string contractId, ClaimsPrincipal claims)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                var existingContract = _unitOfWork.ContractRepository.GetById(contractId);
                if (existingContract == null)
                {
                    new BusinessResult(Const.FAIL_READ_CODE, Const.FAIL_READ_MSG, null);
                }
                existingContract.Status = 2;
                var result = await _unitOfWork.ContractRepository.UpdateAsync(existingContract);
                return new BusinessResult(Const.SUCCESS_DELETE_CODE, Const.SUCCESS_DELETE_MSG);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BusinessResult(Const.SUCCESS_DELETE_CODE, Const.SUCCESS_DELETE_MSG);
            }


        }
        #endregion


        #region Get contract and contract file
        /// <summary>
        /// Get all contract
        /// </summary>
        /// <returns></returns>
        public async Task<BusinessResult> GetContract()
        {
            try
            {
                var contracts = await _unitOfWork.ContractRepository.GetContractsAsync();
                return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, contracts);
            }
            catch
            {
                return new BusinessResult(Const.FAIL_READ_CODE, Const.FAIL_READ_MSG);
            }
        }
        /// <summary>
        /// Get contract by contractId
        /// </summary>
        /// <param name="contractId"></param>
        /// <returns></returns>
        public Task<BusinessResult> GetContract(string contractId)
        {
            try
            {
                var contract = _unitOfWork.ContractRepository.GetById(contractId);
                return Task.FromResult(new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, contract));
            }
            catch
            {
                return Task.FromResult(new BusinessResult(Const.FAIL_READ_CODE, Const.FAIL_READ_MSG));
            }
        }
        /// <summary>
        /// Get contractFile by contractId
        /// </summary>
        /// <param name="contractId"></param>
        /// <returns></returns>
        public async Task<BusinessResult> GetContractFiles(string contractId)
        {
            try
            {
                var contractFiles = _unitOfWork.ContractFileRepository.GetList(x => x.ContractId == contractId);
                return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, contractFiles);
            }
            catch
            {
                return new BusinessResult(Const.FAIL_READ_CODE, Const.FAIL_READ_MSG);
            }
        }
        #endregion
        #region Send Signed Contract
        /// <summary>
        /// add file to contract
        /// </summary>
        /// <param name="contractId"></param>
        /// <param name="descriptions"></param>
        /// <param name="notes"></param>
        /// <param name="files"></param>
        /// <param name="claims"></param>
        /// <returns></returns>
        public async Task<BusinessResult> SendSignedContract(string contractId, List<string> descriptions, List<string> notes, List<IFormFile> files, ClaimsPrincipal claims)
        {
            try
            {
                var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";


                // Kiểm tra số lượng descriptions, notes và files có khớp nhau không
                if (files.Count != descriptions.Count || files.Count != notes.Count)
                {
                    return new BusinessResult(Const.FAIL_CREATE_CODE, "Số lượng files, descriptions và notes phải bằng nhau.");
                }

                // Bắt đầu transaction
                await _unitOfWork.BeginTransactionAsync();
                var savedFiles = new List<ContractFile>();

                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    var fileUrl = await _firebaseService.UploadImageAsync(file);

                    var fileName = Path.GetFileName(file.FileName);
                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    string fileType = GetFileTypeFromExtension(fileExtension);


                    var contractFile = new ContractFile
                    {
                        FileId = Guid.NewGuid().ToString(),
                        ContractId = contractId,
                        FileName = fileName,
                        FileType = fileType,
                        UploadDate = DateTime.UtcNow,
                        UploadBy = userName,
                        Description = descriptions[i], // Gán mô tả tương ứng
                        Note = notes[i],               // Gán ghi chú tương ứng
                        FileUrl = fileUrl,
                        ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        ModifiedBy = userName
                    };

                    // Lưu vào database
                    await _unitOfWork.ContractFileRepository.CreateAsync(contractFile);
                    savedFiles.Add(contractFile);
                }

                // Commit transaction
                //await _repository.CommitTransactionAsync();

                return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, savedFiles);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
            }
        }
        #endregion

        #region Update Contract
        /// <summary>
        /// Update not check logic yet
        /// </summary>
        /// <param name="model"></param>
        /// <param name="claims"></param>
        /// <returns></returns>
        public async Task<BusinessResult> UpdateContractAsync(UpdateContractRequest model, ClaimsPrincipal claims)
        {
            try
            {
                var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                await _unitOfWork.BeginTransactionAsync();

                var contract = await _unitOfWork.ContractRepository.GetContractAsync(model.ContractId);
                if (contract == null)
                    return new BusinessResult(Const.FAIL_READ_CODE, Const.FAIL_READ_MSG);


                contract.StartDate = model.StartDate ?? contract.StartDate;
                contract.EndDate = model.EndDate ?? contract.EndDate;
                contract.Status = model.Status ?? contract.Status;

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

                        contract.ContractFiles.Add(new ContractFile
                        {
                            FileId = Guid.NewGuid().ToString(),
                            ContractId = contract.ContractId,
                            FileName = fileName,
                            FileType = fileType,
                            FileUrl = fileUrl,
                            Description = model.Descriptions[i],
                            Note = model.Notes[i],
                            UploadDate = DateTime.UtcNow,
                            UploadBy = userName,
                            ModifiedBy = userName,
                            ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        });
                    }

                    await _unitOfWork.ContractRepository.UpdateAsync(contract);
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

        public async Task<BusinessResult> DownloadSelectedFilesAsZip(List<string> fileIds)
        {
            try
            {
                if (fileIds == null || !fileIds.Any())
                {
                    return new BusinessResult(Const.FAIL_READ_CODE, "No file IDs provided.");
                }

                var contractFiles = await _unitOfWork.ContractFileRepository.GetFilesByIdsAsync(fileIds);
                if (contractFiles == null || !contractFiles.Any())
                {
                    return new BusinessResult(Const.FAIL_READ_CODE, "No matching files found.");
                }

                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var file in contractFiles)
                        {
                            var fileData = await _firebaseService.DownloadFileAsync(file.FileUrl);
                            if (fileData == null || fileData.Length == 0)
                                continue;

                            var zipEntry = archive.CreateEntry(file.FileName, CompressionLevel.Fastest);
                            using (var entryStream = zipEntry.Open())
                            {
                                await entryStream.WriteAsync(fileData, 0, fileData.Length);
                            }
                        }
                    }

                    memoryStream.Position = 0;
                    var zipBytes = memoryStream.ToArray();

                    return new BusinessResult(Const.SUCCESS_READ_CODE, "Files downloaded successfully.", Convert.ToBase64String(zipBytes));
                }
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.FAIL_READ_CODE, $"Error downloading selected files: {ex.Message}");
            }
        }




        #region Support Read extension file
        /// <summary>
        /// Get extension file to assign into field File Type
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
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
        #endregion
    }
}