using System;
using System.Linq;
using QuanLyKhachSan_fix.Models;

namespace QuanLyKhachSan_fix.Services
{
    public class CurrentUserState
    {
        public User? CurrentUser { get; private set; }
        public bool IsLoggedIn => CurrentUser != null;

        public event Action? OnChange;

        public void SetUser(User user)
        {
            CurrentUser = user;
            OnChange?.Invoke();
        }

        public void ClearUser()
        {
            CurrentUser = null;
            OnChange?.Invoke();
        }

        public bool HasRole(string role)
        {
            if (CurrentUser?.Role == null || string.IsNullOrWhiteSpace(role))
                return false;
            return string.Equals(CurrentUser.Role, role.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public bool HasAnyRole(params string[] roles)
        {
            if (!IsLoggedIn || roles == null || roles.Length == 0)
                return false;
            return roles.Any(HasRole);
        }

        public string GetHomePath()
        {
            if (CurrentUser?.Role == null)
                return "/login";

            return CurrentUser.Role.ToLowerInvariant() switch
            {
                "manager" => "/manager/dashboard",
                "receptionist" => "/receptionist/dashboard",
                "customer" => "/customer/home",
                _ => "/"
            };
        }
    }
}