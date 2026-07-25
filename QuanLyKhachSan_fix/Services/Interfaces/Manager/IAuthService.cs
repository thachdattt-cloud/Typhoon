using QuanLyKhachSan_fix.Models;
using System.Threading.Tasks;

namespace QuanLyKhachSan_fix.Services.Interfaces.Manager
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public User? User { get; set; }
    }

    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string username, string password);
        Task<AuthResult> RegisterAsync(string username, string password, string fullName, string email, string phone);
    }
}