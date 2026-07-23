using QuanLyKhachSan_fix.Models;

namespace QuanLyKhachSan_fix.Services
{
    // Luu tam user dang dang nhap, dung chung cho toan bo app (vi MAUI Blazor chi co 1 scope duy nhat)
    public class CurrentUserState
    {
        public User? CurrentUser { get; private set; }

        public void SetUser(User user)
        {
            CurrentUser = user;
        }

        public void ClearUser()
        {
            CurrentUser = null;
        }

        public bool IsLoggedIn => CurrentUser != null;
    }
}