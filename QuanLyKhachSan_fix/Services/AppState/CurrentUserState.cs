using System;
using QuanLyKhachSan_fix.Models;

namespace QuanLyKhachSan_fix.Services.AppState
{
    // GHI CHU: chua co co che luu "nguoi dang dang nhap" trong project (Login.razor
    // van con la stub trong). Thanh vien 1 tao class nay de 4 trang con lai co the
    // hoat dong. Nguoi lam Login.razor can goi CurrentUserState.SetUser(result.User)
    // ngay sau khi dang nhap/dang ky thanh cong, va SetUser(null) khi dang xuat.
    // Bao nhom truoc khi doi ten class/phuong thuc nay vi cac trang khac se phu thuoc vao no.
    public class CurrentUserState
    {
        public User? CurrentUser { get; private set; }

        public event Action? OnChange;

        public void SetUser(User? user)
        {
            CurrentUser = user;
            OnChange?.Invoke();
        }
    }
}
