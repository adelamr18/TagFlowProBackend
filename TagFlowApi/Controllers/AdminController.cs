using Microsoft.AspNetCore.Mvc;
using TagFlowApi.Dtos;
using TagFlowApi.Repositories;

namespace TagFlowApi.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly AdminRepository _adminRepository;

        public AdminController(AdminRepository adminRepository)
        {
            _adminRepository = adminRepository;
        }

        [HttpGet("get-all-roles")]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _adminRepository.GetAllRolesWithAdminDetails();

            if (roles == null || !roles.Any())
            {
                return NotFound(new { message = "No roles found." });
            }

            return Ok(new { roles });
        }

        [HttpPut("update-role")]
        public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleNameDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.NewRoleName))
            {
                return BadRequest("Invalid input data.");
            }

            var success = await _adminRepository.UpdateRoleNameAsync(dto.RoleId, dto.NewRoleName, dto.UpdatedBy);

            if (!success)
            {
                return StatusCode(500, "An error occurred while updating the role name.");
            }

            return Ok(new { message = "Role name updated successfully." });
        }

        [HttpGet("get-all-tags")]
        public async Task<IActionResult> GetAllTags()
        {
            var tags = await _adminRepository.GetAllTagsWithDetailsAsync();

            if (!tags.Any())
            {
                return NotFound(new { message = "No tags found." });
            }

            return Ok(new { tags });
        }

        [HttpGet("get-all-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _adminRepository.GetAllUsers();

            if (!users.Any())
            {
                return NotFound(new { message = "No users found." });
            }

            return Ok(new { users });
        }

        [HttpPost("create-tag")]
        public async Task<IActionResult> CreateTag([FromBody] TagCreateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.TagName))
            {
                return BadRequest("Invalid input data.");
            }

            if (string.IsNullOrWhiteSpace(dto.AdminUsername))
            {
                return Unauthorized("Admin authentication is required.");
            }

            var success = await _adminRepository.CreateTagAsync(dto, dto.AdminUsername);

            if (!success)
            {
                return StatusCode(500, "An error occurred while creating the tag.");
            }

            return Ok(new { message = "Tag created successfully." });
        }

        [HttpPut("update-tag")]
        public async Task<IActionResult> UpdateTag([FromBody] TagUpdateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.TagName))
            {
                return BadRequest("Invalid input data.");
            }

            var success = await _adminRepository.UpdateTagAsync(dto);

            if (!success)
            {
                return StatusCode(500, "An error occurred while updating the tag.");
            }

            return Ok(new { message = "Tag updated successfully." });
        }

        [HttpDelete("delete-tag/{tagId}")]
        public async Task<IActionResult> DeleteTag(int tagId)
        {
            var success = await _adminRepository.DeleteTagAsync(tagId);

            if (!success)
            {
                return StatusCode(500, "An error occurred while deleting the tag.");
            }

            return Ok(new { message = "Tag deleted successfully." });
        }

        [HttpPut("update-user/{userId}")]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UserUpdateDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Invalid input data.");
            }

            var roleExists = await _adminRepository.RoleExists(dto.RoleId);
            if (!roleExists)
            {
                return BadRequest(new { message = "The specified RoleId does not exist." });
            }

            var success = await _adminRepository.UpdateUserAsync(userId, dto);

            if (!success)
            {
                return StatusCode(500, "An error occurred while updating the user.");
            }

            return Ok(new { message = "User updated successfully." });
        }

        [HttpDelete("delete-user/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                var success = await _adminRepository.DeleteUserAsync(userId);

                if (!success)
                {
                    return StatusCode(500, "An error occurred while deleting the user.");
                }

                return Ok(new { message = "User deleted successfully." });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("add-user")]
        public async Task<IActionResult> AddUser([FromBody] UserCreateDto userCreateDto)
        {
            try
            {
                var created = await _adminRepository.AddNewUserAsync(userCreateDto, userCreateDto.CreatedByAdminEmail);
                if (created)
                {
                    return Ok(new { success = true });
                }

                return BadRequest(new { success = false, message = "Failed to create user." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("get-all-admins")]
        public async Task<IActionResult> GetAllAdmins()
        {
            var admins = await _adminRepository.GetAllAdmins();

            if (!admins.Any())
            {
                return NotFound(new { message = "No admins found." });
            }

            return Ok(new { admins });
        }

        [HttpPost("add-admin")]
        public async Task<IActionResult> AddAdmin([FromBody] CreateAdminDto adminCreatedDto)
        {
            try
            {
                var created = await _adminRepository.AddNewAdminAsync(adminCreatedDto, adminCreatedDto.CreatedBy);
                if (created)
                {
                    return Ok(new { success = true });
                }

                return BadRequest(new { success = false, message = "Failed to create admin." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


        [HttpPut("update-admin/{adminId}")]
        public async Task<IActionResult> UpdateAdmin(int adminId, [FromBody] UpdateAdminDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Invalid input data.");
            }

            var success = await _adminRepository.UpdateAdminAsync(adminId, dto, dto.UpdatedBy);

            if (!success)
            {
                return StatusCode(500, "An error occurred while updating the admin.");
            }

            return Ok(new { message = "Admin updated successfully." });
        }

        [HttpDelete("delete-admin/{adminId}")]
        public async Task<IActionResult> DeleteAdmin(int adminId)
        {
            try
            {
                var success = await _adminRepository.DeleteAdminAsync(adminId);

                if (!success)
                {
                    return StatusCode(500, "An error occurred while deleting the admin.");
                }

                return Ok(new { message = "Admin deleted successfully." });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
