using Microsoft.AspNetCore.Components;

namespace QuanLyKhachSan_fix.Services
{
    public static class AccessGuard
    {
        public static bool Ensure(CurrentUserState userState, NavigationManager navigation, params string[] allowedRoles)
        {
            if (!userState.IsLoggedIn)
            {
                navigation.NavigateTo("/login");
                return false;
            }

            if (allowedRoles != null && allowedRoles.Length > 0 && !userState.HasAnyRole(allowedRoles))
            {
                navigation.NavigateTo("/unauthorized");
                return false;
            }

            return true;
        }
    }
}