﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTCS.Data.DTOs;
using MTCS.Data.Helpers;
using MTCS.Data.Models;
using MTCS.Data.Response;
using MTCS.Service.Base;
using MTCS.Service.Interfaces;

namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;
        private readonly IDriverService _driverService;

        public AuthenController(IAuthService authService, ITokenService tokenService, IDriverService driverService)
        {
            _authService = authService;
            _tokenService = tokenService;
            _driverService = driverService;
        }

        [HttpPost("register-staff")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> RegisterStaff([FromBody] RegisterUserDTO registerDto)
        {
            var result = await _authService.RegisterStaff(registerDto);
            return Ok(result);
        }

        [HttpPost("register-admin")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> RegisterAdmin([FromBody] RegisterUserDTO registerDto)
        {
            var result = await _authService.RegisterAdmin(registerDto);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<TokenDTO>>> Login([FromBody] LoginRequestDTO loginDto)
        {
            var result = await _authService.LoginInternalUser(loginDto);
            return Ok(result);
        }

        [HttpPost("create-driver")]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> CreateDriverWithFiles(
   [FromForm] CreateDriverDTO driverDto,
   [FromForm] List<FileUploadDTO> fileUploads)
        {
            var userName = User.GetUserName();

            var response = await _driverService.CreateDriverWithFiles(driverDto, fileUploads, userName);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPost("driver-login")]
        public async Task<ActionResult<ApiResponse<TokenDTO>>> LoginDriver([FromBody] LoginRequestDTO loginDto)
        {
            var result = await _authService.LoginDriver(loginDto);
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponse<TokenDTO>>> RefreshToken([FromBody] string refreshToken)
        {
            var result = await _tokenService.RefreshToken(refreshToken);
            return Ok(result);
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<ProfileResponseDTO>>> GetProfile()
        {
            var userId = User.GetUserId();
            var result = await _authService.GetUserProfile(userId);

            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<ProfileResponseDTO>>> UpdateProfile([FromBody] ProfileDTO profileDto)
        {
            var userId = User.GetUserId();

            var result = await _authService.UpdateInternalUserProfile(userId, profileDto);

            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpGet("users")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedList<InternalUser>>>> GetUsers(
    [FromQuery] PaginationParams paginationParams,
    [FromQuery] string? keyword = null,
    [FromQuery] int? role = null)
        {
            var result = await _authService.GetInternalUserWithFilter(
                paginationParams,
                keyword,
                role);

            return Ok(result);
        }

        [HttpGet("staff")]
        public async Task<IActionResult> GetStaffForDriver()
        {
            var result = await _authService.GetStaffForDriver();
            return Ok(result);
        }

        [HttpPut("users/{userId}/status")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<ApiResponse<string>>> ChangeUserStatus(
            string userId,
            [FromBody] int newStatus)
        {
            var modifierId = User.GetUserId();

            var result = await _authService.ChangeUserActivationStatus(
                userId,
                newStatus,
                modifierId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPut("users/{userId}/admin-update")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<ApiResponse<ProfileResponseDTO>>> AdminUpdateUser(
     string userId,
     [FromBody] AdminUpdateUserDTO updateDto)
        {
            var modifierId = User.GetUserId();

            var result = await _authService.UpdateUserInformation(userId, updateDto, modifierId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

    }
}
