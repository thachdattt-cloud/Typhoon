using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Services.Interfaces;
using QuanLyKhachSan_fix.Services.Security;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Services.Security;
using System;
using System.Threading.Tasks;

namespace QuanLyKhachSan_fix.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public AuthService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<AuthResult> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return new AuthResult { Success = false, ErrorMessage = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu." };
            }

            await using var db = await _dbFactory.CreateDbContextAsync();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return new AuthResult { Success = false, ErrorMessage = "Tài khoản không tồn tại." };
            }

            if (!user.IsActive)
            {
                return new AuthResult { Success = false, ErrorMessage = "Tài khoản đã bị khóa." };
            }

            bool isValid = PasswordHasher.VerifyPassword(password, user.PasswordHash);
            if (!isValid)
            {
                return new AuthResult { Success = false, ErrorMessage = "Sai mật khẩu." };
            }

            return new AuthResult { Success = true, User = user };
        }

        public async Task<AuthResult> RegisterAsync(string username, string password, string fullName, string email, string phone)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return new AuthResult { Success = false, ErrorMessage = "Vui lòng nhập đầy đủ thông tin bắt buộc." };
            }

            await using var db = await _dbFactory.CreateDbContextAsync();

            bool usernameExists = await db.Users.AnyAsync(u => u.Username == username);
            if (usernameExists)
            {
                return new AuthResult { Success = false, ErrorMessage = "Tên đăng nhập đã được sử dụng." };
            }

            bool emailExists = await db.Users.AnyAsync(u => u.Email == email);
            if (emailExists)
            {
                return new AuthResult { Success = false, ErrorMessage = "Email này đã được đăng ký." };
            }

            var newUser = new User
            {
                Username = username,
                PasswordHash = PasswordHasher.HashPassword(password),
                FullName = fullName,
                Email = email,
                Phone = phone,
                Role = "customer",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            try
            {
                db.Users.Add(newUser);
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Phong truong hop 2 nguoi bam dang ky cung luc voi cung email/username (race condition)
                return new AuthResult { Success = false, ErrorMessage = "Tên đăng nhập hoặc email đã tồn tại." };
            }

            return new AuthResult { Success = true, User = newUser };
        }
    }
}