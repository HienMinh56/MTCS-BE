using Microsoft.AspNetCore.Mvc;
using MTCS.Data.DTOs;
using MTCS.Data.Response;
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

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<string>>> RegisterCustomer([FromBody] RegisterUserDTO registerDto)
        {
            var result = await _authService.RegisterCustomerAsync(registerDto);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<TokenDTO>>> Login([FromBody] LoginRequestDTO loginDto)
        {
            var result = await _authService.LoginUserAsync(loginDto);
            return Ok(result);
        }

        [HttpPost("create-driver")]
        public async Task<ActionResult<ApiResponse<string>>> CreateDriver([FromBody] CreateDriverDTO createDriverDTO)
        {
            var result = await _driverService.CreateDriver(createDriverDTO);
            return Ok(result);
        }

        [HttpPost("driver-login")]
        public async Task<ActionResult<ApiResponse<TokenDTO>>> LoginDriver([FromBody] LoginRequestDTO loginDto)
        {
            var result = await _authService.LoginDriverAsync(loginDto);
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
        {
            var result = await _tokenService.RefreshTokenAsync(refreshToken);
            return Ok(result);
        }
    }
}
