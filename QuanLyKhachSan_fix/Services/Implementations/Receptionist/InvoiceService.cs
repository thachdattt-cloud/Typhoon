using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Services.Interfaces.Customer;
using QuanLyKhachSan_fix.Services.Interfaces.Receptionist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_fix.Services.Implementations.Receptionist
{
    // SUA (lan 3): quay ve mo hinh MOT booking = MOT hoa don duy nhat, xuat luc tra phong,
    // gom day du tien phong + tat ca dich vu/phu phi da duyet - thay vi tach hoa don nhan
    // phong rieng va hoa don phu phi rieng nhu truoc (cach cu gay loi: don khong co phu phi
    // thi khong the xuat hoa don gi ca, vi CalculateExtraFeeAsync tra ve 0).
    // Quy tac dat coc toi thieu 50% luc nhan phong VAN GIU (xem CheckIn.razor), nhung chi
    // tao Payment, KHONG tao Invoice o buoc do nua.
    public class InvoiceService : IInvoiceService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly IPaymentService _paymentService;

        public InvoiceService(IDbContextFactory<AppDbContext> dbFactory, IPaymentService paymentService)
        {
            _dbFactory = dbFactory;
            _paymentService = paymentService;
        }

        public async Task<List<Booking>> GetBookingsReadyForInvoiceAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.Bookings
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails).ThenInclude(bd => bd.Room)
                .Include(b => b.Invoices)
                .Where(b => b.Status == "checked_out")
                .Where(b => !b.Invoices.Any())
                .OrderBy(b => b.CheckOutDate)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.Invoices
                .Include(i => i.Booking).ThenInclude(b => b.Customer)
                .Include(i => i.IssuedByUser)
                .OrderByDescending(i => i.IssuedAt)
                .ToListAsync();
        }

        public async Task<InvoicePreview> GetInvoicePreviewAsync(int bookingId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var booking = await db.Bookings
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails).ThenInclude(bd => bd.Room).ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            var preview = new InvoicePreview { BookingId = bookingId };
            if (booking == null) return preview;

            preview.BookingCode = booking.BookingCode;
            preview.CustomerFullName = booking.Customer?.FullName;
            preview.CustomerUsername = booking.Customer?.Username;
            preview.CustomerPhone = booking.Customer?.Phone;
            preview.CustomerEmail = booking.Customer?.Email;

            preview.CheckInDate = booking.CheckInDate;
            preview.CheckOutDate = booking.CheckOutDate;
            preview.Nights = Math.Max(1, (booking.CheckOutDate.Date - booking.CheckInDate.Date).Days);

            preview.Rooms = booking.BookingDetails.Select(bd => new InvoicePreviewRoom
            {
                RoomNumber = bd.Room.RoomNumber,
                RoomTypeName = bd.Room.RoomType.Name,
                Price = bd.Price,
                GuestCount = bd.GuestCount
            }).ToList();

            var editRequests = await db.BookingEditRequests
                .Include(r => r.OldRoom)
                .Include(r => r.NewRoom)
                .Where(r => r.BookingDetail.BookingId == bookingId && r.Status == "approved")
                .OrderBy(r => r.HandledAt)
                .ToListAsync();

            preview.ExtraServices = editRequests.Select(r => new InvoicePreviewService
            {
                RequestType = r.RequestType ?? "",
                Description = r.RequestType == "extend"
                    ? $"Gia hạn trả phòng đến {r.NewCheckOutDate?.ToString("dd/MM/yyyy")}"
                    : $"Đổi phòng {r.OldRoom?.RoomNumber ?? "?"} → {r.NewRoom?.RoomNumber ?? "?"}",
                ExtraFee = r.ExtraFee ?? 0,
                HandledAt = r.HandledAt
            }).ToList();

            preview.TotalAmount = booking.TotalAmount ?? 0;
            preview.ExtraFeeAmount = preview.ExtraServices.Sum(s => s.ExtraFee);
            preview.OriginalAmount = preview.TotalAmount - preview.ExtraFeeAmount;
            if (preview.OriginalAmount < 0) preview.OriginalAmount = 0;

            preview.PaidAmount = await _paymentService.GetPaidAmountAsync(bookingId);
            preview.OutstandingAmount = preview.TotalAmount - preview.PaidAmount;
            if (preview.OutstandingAmount < 0) preview.OutstandingAmount = 0;

            return preview;
        }

        public async Task<InvoiceResult> CreateInvoiceAsync(int bookingId, int issuedByUserId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var booking = await db.Bookings
                .Include(b => b.Invoices)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return new InvoiceResult { Success = false, ErrorMessage = "Không tìm thấy đơn đặt phòng." };

            if (booking.Status != "checked_out")
                return new InvoiceResult { Success = false, ErrorMessage = "Chỉ có thể xuất hóa đơn sau khi khách đã trả phòng." };

            if (booking.Invoices.Any())
                return new InvoiceResult { Success = false, ErrorMessage = "Đơn đặt phòng này đã có hóa đơn rồi." };

            var issuerExists = await db.Users.AnyAsync(u => u.Id == issuedByUserId);
            if (!issuerExists)
                return new InvoiceResult { Success = false, ErrorMessage = "Mã nhân viên không tồn tại." };

            decimal outstanding = await _paymentService.GetOutstandingAmountAsync(bookingId);
            if (outstanding > 0)
            {
                return new InvoiceResult
                {
                    Success = false,
                    ErrorMessage = $"Khách còn nợ {outstanding:N0} đ. Vui lòng thu tiền trước khi xuất hóa đơn."
                };
            }

            decimal totalAmount = booking.TotalAmount ?? 0;
            if (totalAmount <= 0)
                return new InvoiceResult { Success = false, ErrorMessage = "Không xác định được số tiền để xuất hóa đơn." };

            var invoice = new Invoice
            {
                BookingId = booking.Id,
                TotalAmount = totalAmount,
                IssuedBy = issuedByUserId,
                IssuedAt = DateTime.Now
            };

            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            return new InvoiceResult { Success = true, Invoice = invoice };
        }
    }
}
