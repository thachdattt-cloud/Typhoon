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
    // SUA (lan 2): phat hien loi tinh "tien phong" bang cach cong thang BookingDetail.Price -
    // cot nay CHI luu gia 1 DEM (xem BookingService.CreateBookingAsync), khong nhan so dem o,
    // nen don nhieu dem bi hien thieu tien. Booking.TotalAmount moi la nguon dung: da tinh
    // dung dem x gia luc tao don, va duoc cong dung phu phi khi duyet gia han/doi phong
    // (BookingEditRequestService.ApproveRequestAsync). Tu day ve sau dung truc tiep
    // Booking.TotalAmount + tong ExtraFee cac yeu cau da duyet, KHONG tu tinh lai tu Price.
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

            var candidates = await db.Bookings
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails).ThenInclude(bd => bd.Room)
                .Include(b => b.Invoices)
                .Where(b => b.Status == "checked_out")
                .OrderBy(b => b.CheckOutDate)
                .ToListAsync();

            var result = new List<Booking>();
            foreach (var booking in candidates)
            {
                decimal remaining = await CalculateExtraFeeAsync(booking.Id);
                if (remaining > 0)
                {
                    result.Add(booking);
                }
            }

            return result;
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

        // So tien CHUA duoc xuat hoa don (tinh theo so sach: TotalAmount hien tai - tong cac
        // Invoice da xuat). Dung cach nay (thay vi tu suy ra "phu phi") vi no LUON dung bat ke
        // booking da co bao nhieu lan gia han/doi phong hay hoa don truoc do da xuat dung/sai.
        public async Task<decimal> CalculateExtraFeeAsync(int bookingId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == null) return 0;

            decimal totalAmount = booking.TotalAmount ?? 0;

            decimal alreadyInvoiced = await db.Invoices
                .Where(i => i.BookingId == bookingId)
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;

            decimal remaining = totalAmount - alreadyInvoiced;
            return remaining < 0 ? 0 : remaining;
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

        public async Task<InvoiceResult> CreateCheckInInvoiceAsync(int bookingId, int issuedByUserId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var booking = await db.Bookings
                .Include(b => b.Invoices)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return new InvoiceResult { Success = false, ErrorMessage = "Không tìm thấy đơn đặt phòng." };

            // Da co hoa don nhan phong roi (vi du: phong thu 2 trong cung 1 booking moi check-in)
            // => khong tao trung, coi nhu thanh cong (khong can bao loi ra UI).
            if (booking.Invoices.Any())
                return new InvoiceResult { Success = true, Invoice = null };

            var issuerExists = await db.Users.AnyAsync(u => u.Id == issuedByUserId);
            if (!issuerExists)
                return new InvoiceResult { Success = false, ErrorMessage = "Mã nhân viên không tồn tại." };

            // SUA: dung thang Booking.TotalAmount (da tinh dung dem x gia), KHONG cong
            // BookingDetail.Price (chi la gia 1 dem, thieu tien voi don nhieu dem).
            decimal roomAmount = booking.TotalAmount ?? 0;
            if (roomAmount <= 0)
                return new InvoiceResult { Success = false, ErrorMessage = "Không xác định được tiền phòng để xuất hóa đơn." };

            var invoice = new Invoice
            {
                BookingId = booking.Id,
                TotalAmount = roomAmount,
                IssuedBy = issuedByUserId,
                IssuedAt = DateTime.Now
            };

            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            return new InvoiceResult { Success = true, Invoice = invoice };
        }

        public async Task<InvoiceResult> CreateInvoiceAsync(int bookingId, int issuedByUserId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return new InvoiceResult { Success = false, ErrorMessage = "Không tìm thấy đơn đặt phòng." };

            if (booking.Status != "checked_out")
                return new InvoiceResult { Success = false, ErrorMessage = "Chỉ có thể xuất hóa đơn phụ phí sau khi khách đã trả phòng." };

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

            decimal remaining = await CalculateExtraFeeAsync(bookingId);
            if (remaining <= 0)
                return new InvoiceResult { Success = false, ErrorMessage = "Không có phụ phí phát sinh, không cần xuất thêm hóa đơn." };

            var invoice = new Invoice
            {
                BookingId = booking.Id,
                TotalAmount = remaining,
                IssuedBy = issuedByUserId,
                IssuedAt = DateTime.Now
            };

            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            return new InvoiceResult { Success = true, Invoice = invoice };
        }
    }
}
