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
        [HttpPost("add-project")]
        public async Task<IActionResult> AddProject([FromBody] ProjectDto projectCreateDto)
        {
            if (projectCreateDto == null || string.IsNullOrWhiteSpace(projectCreateDto.ProjectName))
            {
                return BadRequest(new { message = "Invalid project data." });
            }
            if (string.IsNullOrWhiteSpace(projectCreateDto.CreatedByAdminEmail))
            {
                return BadRequest(new { message = "Admin email is required." });
            }
            try
            {
                var created = await _adminRepository.AddNewProjectAsync(projectCreateDto, projectCreateDto.CreatedByAdminEmail);
                if (created)
                {
                    return Ok(new { success = true });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Project creation failed." });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("get-all-projects")]
        public async Task<IActionResult> GetAllProjects()
        {
            var projects = await _adminRepository.GetAllProjectsAsync();
            return Ok(new { projects });
        }


        [HttpPut("update-project")]
        public async Task<IActionResult> UpdateProject([FromBody] ProjectUpdateDto dto)
        {
            if (dto == null || dto.ProjectId <= 0 || string.IsNullOrWhiteSpace(dto.ProjectName))
                return BadRequest(new { message = "Invalid project data." });
            try
            {
                var success = await _adminRepository.UpdateProjectAsync(dto);
                if (success)
                    return Ok(new { message = "Project updated successfully." });
                return StatusCode(500, new { message = "Failed to update project." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("delete-project/{projectId}")]
        public async Task<IActionResult> DeleteProject(int projectId)
        {
            try
            {
                var success = await _adminRepository.DeleteProjectAsync(projectId);
                if (success)
                    return Ok(new { message = "Project deleted successfully." });
                return StatusCode(500, new { message = "Failed to delete project." });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("add-patient-type")]
        public async Task<IActionResult> AddPatientType([FromBody] PatientTypeCreateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.CreatedByAdminEmail))
            {
                return BadRequest(new { message = "Invalid patient type data." });
            }
            try
            {
                var success = await _adminRepository.AddNewPatientTypeAsync(dto, dto.CreatedByAdminEmail);
                if (success)
                {
                    return Ok(new { success = true });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Failed to add patient type." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("get-all-patient-types")]
        public async Task<IActionResult> GetAllPatientTypes()
        {
            try
            {
                var types = await _adminRepository.GetAllPatientTypesAsync();
                if (types == null || !types.Any())
                    return Ok(new { patientTypes = new List<object>(), message = "No patient types found." });
                return Ok(new { patientTypes = types });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving patient types.", error = ex.Message });
            }
        }

        [HttpPut("update-patient-type")]
        public async Task<IActionResult> UpdatePatientType([FromBody] PatientTypeUpdateDto dto)
        {
            if (dto == null || dto.PatientTypeId <= 0 || string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.UpdatedBy))
                return BadRequest(new { message = "Invalid patient type data." });
            try
            {
                var success = await _adminRepository.UpdatePatientTypeAsync(dto);
                if (success)
                    return Ok(new { message = "Patient type updated successfully." });
                return StatusCode(500, new { message = "Failed to update patient type." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("delete-patient-type/{patientTypeId}")]
        public async Task<IActionResult> DeletePatientType(int patientTypeId)
        {
            try
            {
                var success = await _adminRepository.DeletePatientTypeAsync(patientTypeId);
                if (success)
                    return Ok(new { message = "Patient type deleted successfully." });
                return StatusCode(500, new { message = "Failed to delete patient type." });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

    }

}

