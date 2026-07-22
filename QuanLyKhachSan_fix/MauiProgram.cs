using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Services.Implementations;
using QuanLyKhachSan_fix.Services.Interfaces;
using QuanLyKhachSan_fix;
using QuanLyKhachSan_fix.Data;

namespace QuanLyKhachSan_fix
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            // ---- Thêm phần này ----

            // Connection string tạm hardcode (vi appsettings.json khong tu doc duoc trong MAUI)
            const string connectionString = "Server=localhost;Database=QuanLyKhachSan;Trusted_Connection=True;TrustServerCertificate=True;";

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));
            //builder.Services.AddScoped<IAuthService, AuthService>();
            //builder.Services.AddScoped<IRoomService, RoomService>();
            // TODO: 3 thanh vien them dong dang ky Service cua minh vao day
            // builder.Services.AddScoped<IBookingService, BookingService>();      // Thanh vien 1
            // builder.Services.AddScoped<ICheckInOutService, CheckInOutService>();// Thanh vien 2
            // builder.Services.AddScoped<IEmployeeService, EmployeeService>();    // Thanh vien 3

            // ---- Hết phần thêm ----

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}