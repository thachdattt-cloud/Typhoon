using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Services;
using QuanLyKhachSan_fix.Services.Implementations.Customer;
using QuanLyKhachSan_fix.Services.Implementations.Manager;
using QuanLyKhachSan_fix.Services.Implementations.Receptionist;
using QuanLyKhachSan_fix.Services.Interfaces.Customer;
using QuanLyKhachSan_fix.Services.Interfaces.Manager;
using QuanLyKhachSan_fix.Services.Interfaces.Receptionist;

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

            const string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=QuanLyKhachSan;Integrated Security=True";

            builder.Services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IRoomService, RoomService>();
            builder.Services.AddScoped<IEmployeeService, EmployeeService>();
            builder.Services.AddScoped<ICustomerAccountService, CustomerAccountService>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddSingleton<QuanLyKhachSan_fix.Services.CurrentUserState>();

            // TODO: 1 thanh vien con lai them dong dang ky Service cua minh vao day
            builder.Services.AddScoped<IBookingService, BookingService>();       // Thanh vien 1
            builder.Services.AddScoped<IPaymentService, PaymentService>();       // Thanh vien 1
            builder.Services.AddScoped<IBookingEditRequestService, BookingEditRequestService>(); // Thanh vien 1
            builder.Services.AddScoped<ICheckInOutService, CheckInOutService>(); // Thanh vien 2
            builder.Services.AddScoped<IInvoiceService, InvoiceService>();       // Thanh vien 2

            // ---- Hết phần thêm ----

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}