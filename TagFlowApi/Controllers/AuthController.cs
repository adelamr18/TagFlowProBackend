using Microsoft.AspNetCore.Mvc;
using TagFlowApi.Dtos;
using TagFlowApi.Repositories;
using TagFlowApi.Infrastructure;
using TagFlowApi.Utils;

namespace TagFlowApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(UserRepository userRepository, JwtService jwtService) : ControllerBase
    {
        private readonly UserRepository _userRepository = userRepository;
        private readonly JwtService _jwtService = jwtService;

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Email and password are required" });
            }

            var admin = _userRepository.GetAdminByEmail(request.Email);
            if (admin != null)
            {
                if (admin.IsDeleted)
                {
                    return Unauthorized(new { message = "Admin account is deleted" });
                }

                if (!admin.CheckPassword(request.Password))
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                var token = _jwtService.GenerateToken(admin.AdminId);

                return Ok(new
                {
                    message = "Login successful",
                    token,
                    userType = "Admin",
                    userName = admin.Username,
                    roleId = admin.RoleId
                });
            }

            var user = _userRepository.GetUserByEmail(request.Email);
            if (user != null)
            {
                if (!user.CheckPassword(request.Password))
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                var token = _jwtService.GenerateToken(user.UserId);

                return Ok(new
                {
                    message = "Login successful",
                    token,
                    userType = "User",
                    userName = user.Username,
                    roleId = user.RoleId
                });
            }

            return Unauthorized(new { message = "Invalid email or password" });
        }


        [HttpPost("forget-password")]
        public IActionResult ForgetPassword([FromBody] ForgetPasswordDto request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest(new { message = "Email and new password are required" });
            }

            string hashedPassword = Helpers.HashPassword(request.NewPassword);

            bool updateSuccessful = _userRepository.UpdatePasswordHash(request.Email, hashedPassword);

            if (!updateSuccessful)
            {
                return Unauthorized(new { message = "You cannot use an already existing password. Please try a different one." });
            }

            return Ok(new { message = "Password updated successfully. Please use your new password to log in." });
        }
    }
}
