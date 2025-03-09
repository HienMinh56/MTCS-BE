using Microsoft.AspNetCore.Http;
using MTCS.Common;
using MTCS.Data;
using MTCS.Data.Models;
using MTCS.Data.Request;
using MTCS.Service.Base;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MTCS.Service.Services
{
    public interface IContractService
    {
        Task<BusinessResult> CreateContract(ContractRequest contractRequest, IFormFile file, ClaimsPrincipal claims);
        Task<BusinessResult> SendSignedContract(string contractId, string description, string note, IFormFile file, ClaimsPrincipal claims);
    }

    public class ContractService : IContractService
    {
        private readonly UnitOfWork _repository;
        private readonly IFirebaseStorageService _firebaseService;

        public ContractService(UnitOfWork repository, IFirebaseStorageService firebaseService)
        {
            _repository = repository;
            _firebaseService = firebaseService;
        }

        public async Task<BusinessResult> CreateContract(ContractRequest contractRequest, IFormFile file, ClaimsPrincipal claims)
        {
            try
            {
                var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
             ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                // Begin transaction
                await _repository.BeginTransactionAsync();

                var nextContractId = await _repository.ContractRepository.GetNextContractNumberAsync();
                var contractId = $"CTR{nextContractId:D6}";
                // 1. Create Contract entity
                var contract = new Contract
                {
                    ContractId = contractId,
                    UserId = userId,
                    StartDate = contractRequest.StartDate,
                    EndDate = contractRequest.EndDate,
                    Status = contractRequest.Status,
                    CreatedDate = DateTime.Now,
                    CreatedBy = userName
                    // Set other contract properties as needed
                };

                // 2. Add contract to database
                await _repository.ContractRepository.CreateAsync(contract);

                // 3. Upload file to Firebase and get URL
                var fileUrl = await _firebaseService.UploadImageAsync(file);

                // 4. Extract file information
                var fileName = Path.GetFileName(file.FileName);
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                // Determine file type based on extension
                string fileType = GetFileTypeFromExtension(fileExtension);



                var nextFileID = await _repository.ContractFileRepository.GetNextFileNumberAsync();
                var fileId = $"FIL{nextFileID:D6}";
                // 5. Create ContractFile entity
                var contractFile = new ContractFile
                {
                    FileId = fileId,
                    ContractId = contractId,
                    FileName = fileName,
                    FileType = fileType,
                    UploadDate = DateTime.Now,
                    UploadBy = userName,
                    Description = contractRequest.FileDescription,
                    Note = contractRequest.FileNote,
                    FileUrl = fileUrl,
                    ModifiedDate = DateOnly.FromDateTime(DateTime.Now),
                    ModifiedBy = userName
                };

                // 6. Add contract file to database
                await _repository.ContractFileRepository.CreateAsync(contractFile);


                // 7. Commit transaction
                await _repository.CommitTransactionAsync();

                return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, new
                {
                    ContractId = contractId,
                    FileId = fileId
                });
            }
            catch (Exception ex)
            {
                // Rollback transaction on error
                await _repository.RollbackTransactionAsync();
                throw;
            }
        }


        public async Task<BusinessResult> SendSignedContract(string contractId, string description, string note, IFormFile file, ClaimsPrincipal claims)
        {
            try
            {
                var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
             ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                // Begin transaction
                await _repository.BeginTransactionAsync();

                var fileUrl = await _firebaseService.UploadImageAsync(file);

                var fileName = Path.GetFileName(file.FileName);
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                string fileType = GetFileTypeFromExtension(fileExtension);



                var nextFileID = await _repository.ContractFileRepository.GetNextFileNumberAsync();
                var fileId = $"FIL{nextFileID:D6}";
                var contractFile = new ContractFile
                {
                    FileId = fileId,
                    ContractId = contractId,
                    FileName = fileName,
                    FileType = fileType,
                    UploadDate = DateTime.Now,
                    UploadBy = userName,
                    Description = description,
                    Note = note,
                    FileUrl = fileUrl,
                    ModifiedDate = DateOnly.FromDateTime(DateTime.Now),
                    ModifiedBy = userName
                };

                // 6. Add contract file to database
                var result = await _repository.ContractFileRepository.CreateAsync(contractFile);

                // 7. Commit transaction
                await _repository.CommitTransactionAsync();

                return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, result);

            }
            catch (Exception ex)
            {
                // Rollback transaction on error
                await _repository.RollbackTransactionAsync();
                throw;
            }
        }

        // Helper method to determine file type from extension
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