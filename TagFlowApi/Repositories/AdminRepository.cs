using Microsoft.EntityFrameworkCore;
using TagFlowApi.Dtos;
using TagFlowApi.Infrastructure;
using TagFlowApi.Models;

namespace TagFlowApi.Repositories
{
    public class AdminRepository(DataContext context)
    {
        private readonly DataContext _context = context;

        public async Task<List<RoleDto>> GetAllRolesWithAdminDetails()
        {
            var rolesWithAdmins = await _context.Roles
                .Include(r => r.CreatedByAdmin)
                .Select(r => new RoleDto
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName,
                    CreatedAt = r.CreatedAt,
                    UpdatedBy = r.UpdatedBy,
                    CreatedBy = r.CreatedByAdmin != null ? r.CreatedByAdmin.Email : null
                })
                .ToListAsync();

            return rolesWithAdmins;
        }


        public async Task<bool> UpdateRoleNameAsync(int roleId, string newRoleName, string updatedBy)
        {
            if (string.IsNullOrWhiteSpace(newRoleName))
            {
                throw new ArgumentException("Role name cannot be empty or null.", nameof(newRoleName));
            }

            if (string.IsNullOrWhiteSpace(updatedBy))
            {
                throw new ArgumentException("UpdatedBy cannot be empty or null.", nameof(updatedBy));
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId)
                ?? throw new Exception($"Role with ID {roleId} not found.");

            role.RoleName = newRoleName;
            role.UpdatedBy = updatedBy;

            try
            {
                _context.Roles.Update(role);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating role name: {ex.Message}");
                return false;
            }
        }

        public async Task<List<UserDto>> GetAllUsers()
        {
            var users = await _context.Users
                .Include(u => u.CreatedByAdmin)
                .Include(u => u.Role)
                .Include(u => u.UserTagPermissions)
                    .ThenInclude(utp => utp.Tag)
                    .Where(u => u.CreatedByAdmin == null || !u.CreatedByAdmin.IsDeleted)
                .ToListAsync();

            var userDtos = users.Select(u => new UserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                CreatedByAdminEmail = u.CreatedByAdmin != null ? u.CreatedByAdmin.Email : "",
                CreatedByAdminName = u.CreatedByAdmin != null ? u.CreatedByAdmin.Username : "",
                RoleName = u.Role.RoleName,
                RoleId = u.RoleId,
                AssignedTags = u.UserTagPermissions?.Select(utp => utp.Tag.TagName ?? "").ToList() ?? [],
                UpdatedBy = u.UpdatedBy
            })
            .ToList();

            return userDtos;
        }

        public async Task<IEnumerable<TagDto>> GetAllTagsWithDetailsAsync()
        {
            var tags = await _context.Tags
                .Include(t => t.TagValues)
                .Include(t => t.UserTagPermissions)
                    .ThenInclude(tu => tu.User)
                .Include(t => t.CreatedByAdmin)
                .AsSplitQuery()
                .Where(t => t.CreatedByAdmin == null || !t.CreatedByAdmin.IsDeleted)
                .Select(t => new TagDto
                {
                    TagId = t.TagId,
                    TagName = t.TagName,
                    TagValues = t.TagValues.Select(tv => tv.Value).ToList(),
                    TagValuesIds = t.TagValues.Select(tv => tv.TagValueId).ToList(),
                    AssignedUsers = t.UserTagPermissions.Select(tu => tu.User.Username).ToList(),
                    AssignedUserIds = t.UserTagPermissions.Select(tu => tu.User.UserId).ToList(),
                    CreatedByEmail = t.CreatedByAdmin != null ? t.CreatedByAdmin.Email : "",
                    CreatedByUserName = t.CreatedByAdmin != null ? t.CreatedByAdmin.Username : "",
                    UpdatedBy = t.UpdatedBy,
                })
                .ToListAsync();

            return tags;
        }


        public async Task<bool> UpdateTagAsync(TagUpdateDto tagUpdateDto)
        {
            var tag = await _context.Tags
                .Include(t => t.TagValues)
                .Include(t => t.UserTagPermissions)
                    .ThenInclude(tu => tu.User)
                .AsSplitQuery()
                .FirstOrDefaultAsync(t => t.TagId == tagUpdateDto.TagId);

            if (tag == null)
            {
                throw new Exception($"Tag with ID {tagUpdateDto.TagId} not found.");
            }

            tag.TagName = tagUpdateDto.TagName;
            tag.Description = tagUpdateDto.Description ?? tag.Description;
            tag.UpdatedBy = tagUpdateDto.UpdatedBy;

            if (tagUpdateDto.TagValues != null && tagUpdateDto.TagValues.Any())
            {
                var valuesToRemove = tag.TagValues
                    .Where(tv => !tagUpdateDto.TagValues.Contains(tv.Value))
                    .ToList();
                _context.TagValues.RemoveRange(valuesToRemove);

                var existingValues = tag.TagValues.Select(tv => tv.Value).ToHashSet();
                var valuesToAdd = tagUpdateDto.TagValues
                    .Where(v => !existingValues.Contains(v))
                    .Select(v => new TagValue
                    {
                        Value = v,
                        TagId = tag.TagId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = tag.CreatedBy
                    })
                    .ToList();

                _context.TagValues.AddRange(valuesToAdd);
            }

            if (tagUpdateDto.AssignedUsers != null)
            {
                var currentUsernames = tag.UserTagPermissions
                    .Where(utp => utp.User != null)
                    .Select(utp => utp.User.Username)
                    .ToHashSet();

                var usersToAdd = tagUpdateDto.AssignedUsers.Except(currentUsernames).ToList();
                var usersToRemove = currentUsernames.Except(tagUpdateDto.AssignedUsers).ToList();

                var userTagPermissionsToRemove = tag.UserTagPermissions
                    .Where(utp => utp.User != null && usersToRemove.Contains(utp.User.Username))
                    .ToList();
                _context.UserTagPermissions.RemoveRange(userTagPermissionsToRemove);

                foreach (var username in usersToAdd)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                    if (user != null)
                    {
                        tag.UserTagPermissions.Add(new UserTagPermission
                        {
                            TagId = tag.TagId,
                            UserId = user.UserId,
                        });
                    }
                }
            }

            try
            {
                _context.Tags.Update(tag);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating tag: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteTagAsync(int tagId)
        {
            var tag = await _context.Tags
                .Include(t => t.TagValues)
                .Include(t => t.UserTagPermissions)
                .AsSplitQuery()
                .FirstOrDefaultAsync(t => t.TagId == tagId);

            if (tag == null)
            {
                throw new Exception($"Tag with ID {tagId} not found.");
            }

            _context.TagValues.RemoveRange(tag.TagValues);

            _context.UserTagPermissions.RemoveRange(tag.UserTagPermissions);

            _context.Tags.Remove(tag);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting tag: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CreateTagAsync(TagCreateDto tagCreateDto, string createdByAdminUsername)
        {
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == createdByAdminUsername);
            if (admin == null)
            {
                throw new Exception("Admin not found.");
            }

            var newTag = new Tag
            {
                TagName = tagCreateDto.TagName,
                CreatedBy = admin.AdminId,
                CreatedAt = DateTime.UtcNow
            };

            if (tagCreateDto.TagValues != null && tagCreateDto.TagValues.Any())
            {
                newTag.TagValues = tagCreateDto.TagValues.Select(value => new TagValue
                {
                    Value = value,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = admin.AdminId
                }).ToList();
            }

            if (tagCreateDto.AssignedUsers != null && tagCreateDto.AssignedUsers.Any())
            {
                newTag.UserTagPermissions = new List<UserTagPermission>();

                foreach (var username in tagCreateDto.AssignedUsers)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                    if (user != null)
                    {
                        newTag.UserTagPermissions.Add(new UserTagPermission
                        {
                            UserId = user.UserId
                        });
                    }
                }
            }

            try
            {
                await _context.Tags.AddAsync(newTag);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating tag: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AddNewUserAsync(UserCreateDto userCreateDto, string createdByAdminEmail)
        {
            if (userCreateDto == null)
            {
                throw new ArgumentException("UserCreateDto cannot be null.");
            }

            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Email == createdByAdminEmail);
            if (admin == null)
            {
                throw new Exception("Admin not found.");
            }

            if (await _context.Users.AnyAsync(u => u.Email == userCreateDto.Email))
            {
                throw new Exception("User with the given email already exists.");
            }

            if (await _context.Users.AnyAsync(u => u.Username == userCreateDto.Username))
            {
                throw new Exception("User with the given username already exists.");
            }

            if (!await _context.Roles.AnyAsync(r => r.RoleId == userCreateDto.RoleId))
            {
                throw new Exception($"Role with ID {userCreateDto.RoleId} not found.");
            }

            var hashedPassword = Utils.Helpers.HashPassword(userCreateDto.Password);

            var newUser = new User
            {
                Username = userCreateDto.Username,
                Email = userCreateDto.Email,
                PasswordHash = hashedPassword,
                RoleId = userCreateDto.RoleId,
                CreatedBy = admin.AdminId,
                CreatedAt = DateTime.UtcNow
            };

            if (userCreateDto.AssignedTagIds != null && userCreateDto.AssignedTagIds.Any())
            {
                newUser.UserTagPermissions = userCreateDto.AssignedTagIds.Select(tagId => new UserTagPermission
                {
                    TagId = tagId
                }).ToList();
            }

            try
            {
                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating user: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserTagPermissions)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                throw new Exception($"User with ID {userId} not found.");
            }

            _context.UserTagPermissions.RemoveRange(user.UserTagPermissions);
            _context.Users.Remove(user);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting user: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RoleExists(int? roleId)
        {
            return await _context.Roles.AnyAsync(r => r.RoleId == roleId);
        }

        public async Task<bool> UpdateUserAsync(int userId, UserUpdateDto userUpdateDto)
        {
            if (userUpdateDto == null)
            {
                throw new ArgumentException("UserUpdateDto cannot be null.");
            }

            var user = await _context.Users
                .Include(u => u.UserTagPermissions)
                .ThenInclude(utp => utp.Tag)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                throw new Exception($"User with ID {userId} not found.");
            }

            if (!string.IsNullOrWhiteSpace(userUpdateDto.Username))
            {
                user.Username = userUpdateDto.Username;
            }

            if (userUpdateDto.RoleId.HasValue)
            {
                user.RoleId = userUpdateDto.RoleId.Value;
            }

            if (userUpdateDto.AssignedTagIds != null)
            {
                var currentTagIds = user.UserTagPermissions.Select(utp => utp.TagId).ToList();

                var tagsToAdd = userUpdateDto.AssignedTagIds
                    .Except(currentTagIds)
                    .Select(tagId => new UserTagPermission { UserId = userId, TagId = tagId });

                var tagsToRemove = user.UserTagPermissions
                    .Where(utp => !userUpdateDto.AssignedTagIds.Contains(utp.TagId))
                    .ToList();

                await _context.UserTagPermissions.AddRangeAsync(tagsToAdd);

                _context.UserTagPermissions.RemoveRange(tagsToRemove);
            }

            user.UpdatedBy = userUpdateDto.UpdatedBy;

            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user: {ex.Message}");
                return false;
            }
        }

        public async Task<List<AdminDto>> GetAllAdmins()
        {
            var admins = await _context.Admins
                .Where(a => !a.IsDeleted)
                .ToListAsync();

            var adminDtos = admins.Select(a => new AdminDto
            {
                AdminId = a.AdminId,
                Username = a.Username,
                Email = a.Email,
                CreatedByAdminEmail = a.CreatedBy != null ? a.CreatedBy : "",
                UpdatedBy = a.UpdatedBy,
                RoleId = a.RoleId
            })
            .ToList();

            return adminDtos;
        }

        public async Task<bool> AddNewAdminAsync(CreateAdminDto adminCreateDto, string CreatedBy)
        {
            if (adminCreateDto == null)
            {
                throw new ArgumentException("adminCreateDto cannot be null.");
            }

            if (await _context.Admins.AnyAsync(a => a.Email == adminCreateDto.Email))
            {
                throw new Exception("Admin with the given email already exists.");
            }

            if (await _context.Admins.AnyAsync(a => a.Username == adminCreateDto.Username))
            {
                throw new Exception("Admin with the given username already exists.");
            }

            var creatorAdmin = await _context.Admins
                               .FirstOrDefaultAsync(a => a.Email.Trim() == adminCreateDto.CreatedBy.Trim());
            if (creatorAdmin == null)
            {
                throw new Exception("Admin performing the creation not found.");
            }

            var hashedPassword = Utils.Helpers.HashPassword(adminCreateDto.Password);

            var newAdmin = new Admin
            {
                Username = adminCreateDto.Username,
                Email = adminCreateDto.Email,
                PasswordHash = hashedPassword,
                CreatedBy = adminCreateDto.CreatedBy,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _context.Admins.AddAsync(newAdmin);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating admin: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAdminAsync(int adminId, UpdateAdminDto updateAdminDto, string updatedBy)
        {
            if (updateAdminDto == null)
            {
                throw new ArgumentException("UpdateAdminDto cannot be null.");
            }

            var updatingAdmin = await _context.Admins
                                .FirstOrDefaultAsync(a => a.Email.Trim() == updateAdminDto.UpdatedBy.Trim());

            if (updatingAdmin == null)
            {
                throw new Exception("Admin performing the update not found.");
            }

            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.AdminId == adminId);

            if (admin == null)
            {
                throw new Exception($"Admin with ID {adminId} not found.");
            }

            if (!string.IsNullOrWhiteSpace(updateAdminDto.Username))
            {
                admin.Username = updateAdminDto.Username;
            }

            if (!string.IsNullOrWhiteSpace(updateAdminDto.UpdatedBy))
            {
                admin.UpdatedBy = updateAdminDto.UpdatedBy;
            }

            if (!string.IsNullOrWhiteSpace(updateAdminDto.Email))
            {
                var isEmailTaken = await _context.Admins.AnyAsync(a => a.Email == updateAdminDto.Email && a.AdminId != adminId);
                if (isEmailTaken)
                {
                    throw new Exception($"Email {updateAdminDto.Email} is already in use.");
                }
                admin.Email = updateAdminDto.Email;
            }

            try
            {
                _context.Admins.Update(admin);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating admin: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteAdminAsync(int adminId)
        {
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.AdminId == adminId);

            if (admin == null)
            {
                throw new Exception($"Admin with ID {adminId} not found.");
            }

            admin.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting admin: {ex.Message}");
                return false;
            }
        }
    }
}
