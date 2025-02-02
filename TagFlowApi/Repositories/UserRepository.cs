using TagFlowApi.Models;
using TagFlowApi.Infrastructure;

namespace TagFlowApi.Repositories
{
    public class UserRepository(DataContext context)
    {
        private readonly DataContext _context = context;

        public User? GetUserByEmail(string email)
        {
            return _context.Users.SingleOrDefault(user => user.Email == email);
        }

        public Admin? GetAdminByEmail(string email)
        {
            return _context.Admins.SingleOrDefault(admin => admin.Email == email && !admin.IsDeleted);
        }

        public bool UpdatePasswordHash(string email, string newPasswordHash)
        {
            var admin = _context.Admins.SingleOrDefault(a => a.Email == email);
            if (admin != null)
            {
                if (admin.PasswordHash == newPasswordHash)
                {
                    return false;
                }

                admin.PasswordHash = newPasswordHash ?? "";
                int rowsAffected = _context.SaveChanges();

                if (rowsAffected > 0)
                {
                    _context.Entry(admin).Reload();
                    return true;
                }

                return false;
            }

            var user = _context.Users.SingleOrDefault(u => u.Email == email);
            if (user != null)
            {
                if (user.PasswordHash == newPasswordHash)
                {
                    return false;
                }

                user.PasswordHash = newPasswordHash ?? "";
                int rowsAffected = _context.SaveChanges();

                if (rowsAffected > 0)
                {
                    _context.Entry(user).Reload();
                    return true;
                }

                return false;
            }

            return false;
        }
    }
}
