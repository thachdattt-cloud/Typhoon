using System;
using QuanLyKhachSan_fix.Models;

namespace QuanLyKhachSan_fix.Services
{
    public class CurrentUserState
    {
        public User? CurrentUser { get; private set; }
        public bool IsLoggedIn => CurrentUser != null;

        // Su kien de cac component (vi du NavMenu) tu ve lai khi user thay doi
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
    }
}