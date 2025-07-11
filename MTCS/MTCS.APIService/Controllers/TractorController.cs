﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Helpers;
using MTCS.Service.Interfaces;

namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TractorController : ControllerBase
    {
        private readonly ITractorService _tractorService;

        public TractorController(ITractorService tractorService)
        {
            _tractorService = tractorService;
        }

        [HttpPost("create-with-files")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateTractorWithFiles(
    [FromForm] CreateTractorDTO tractorDto,
    [FromForm] List<FileUploadDTO> fileUploads)
        {
            var userId = User.GetUserId();

            var response = await _tractorService.CreateTractorWithFiles(tractorDto, fileUploads, userId);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetTractorsBasicInfo(
             [FromQuery] PaginationParams paginationParams,
             [FromQuery] string? searchKeyword = null,
             [FromQuery] TractorStatus? status = null,
             [FromQuery] bool? maintenanceDueSoon = null,
             [FromQuery] bool? registrationExpiringSoon = null,
             [FromQuery] int? maintenanceDueDays = null,
             [FromQuery] int? registrationExpiringDays = null)
        {
            var response = await _tractorService.GetTractorsBasicInfo(
                paginationParams,
                searchKeyword,
                status,
                maintenanceDueSoon,
                registrationExpiringSoon,
                maintenanceDueDays,
                registrationExpiringDays);
            return Ok(response);
        }

        [HttpGet("{tractorId}")]
        public async Task<IActionResult> GetTractorDetails(string tractorId)
        {
            var response = await _tractorService.GetTractorDetail(tractorId);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{tractorId}")]
        public async Task<IActionResult> UpdateTractorWithFiles(
    string tractorId,
    [FromForm] CreateTractorDTO updateDto,
    [FromForm] List<FileUploadDTO>? newFiles = null,
    [FromForm] List<string>? fileIdsToRemove = null)
        {
            var userId = User.GetUserId();

            var response = await _tractorService.UpdateTractorWithFiles(
                tractorId,
                updateDto,
                newFiles ?? new List<FileUploadDTO>(),
                fileIdsToRemove ?? new List<string>(),
                userId);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPut("files/{fileId}")]
        public async Task<IActionResult> UpdateTractorFileDetails(
            string fileId,
            [FromBody] FileDetailsDTO updateDto)
        {
            var userId = User.GetUserId();

            var response = await _tractorService.UpdateTractorFileDetails(
                fileId,
                updateDto,
                userId);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPut("activate-tractor/{tractorId}")]
        public async Task<IActionResult> ActivateTractor(string tractorId)
        {
            var userId = User.GetUserId();

            var response = await _tractorService.ActivateTractor(tractorId, userId);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPut("deactivate-tractor/{tractorId}")]
        public async Task<IActionResult> DeactivateTractor(string tractorId)
        {
            var userId = User.GetUserId();

            var response = await _tractorService.DeleteTractor(tractorId, userId);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("history/{tractorId}")]
        public async Task<IActionResult> GetTractorUseHistory(string tractorId, [FromQuery] PaginationParams paginationParams)
        {
            var response = await _tractorService.GetTractorUseHistory(tractorId, paginationParams);
            if (!response.Success)
            {
                return NotFound(response);
            }
            return Ok(response);
        }
    }
}
