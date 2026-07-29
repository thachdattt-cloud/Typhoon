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
    // SUA THEO PHUONG AN MOI: "Dat phong truoc, thanh toan sau"
    // - Nhan phong (check-in): le tan thu tien phong da dat (CreateStaffCollectedPaymentAsync)
    //   VA xuat 1 hoa don "tam ung" ngay luc do (khong con doi den luc tra phong).
    // - Trong luc luu tru: neu co yeu cau gia han/doi phong duoc duyet, Booking.TotalAmount
    //   tang len (xem BookingEditRequestService.ApproveRequestAsync).
    // - Tra phong (check-out): neu TotalAmount da tang so voi da thu, phai thanh toan phan
    //   chenh lech (CheckInOutService.CheckOutAsync da chan tra phong neu con no - khong doi),
    //   roi xuat them 1 hoa don "bo sung" cho phan phat sinh do.
    // => 1 booking co the co NHIEU Invoice (truoc day gioi han toi da 1 hoa don/booking).
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

            // SUA: truoc day chi liet ke booking "checked_out" VA chua co hoa don nao ca.
            // Gio can liet ke ca booking "checked_in" (de xuat hoa don tam ung), va van
            // liet ke "checked_out" neu con phan chua xuat hoa don (bo sung) - vi 1 booking
            // co the da co 1 hoa don tam ung roi nhung van con phan phat sinh chua xuat.
            var candidates = await db.Bookings
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails).ThenInclude(bd => bd.Room)
                .Include(b => b.Invoices)
                .Where(b => b.Status == "checked_in" || b.Status == "checked_out")
                .OrderBy(b => b.CheckOutDate)
                .ToListAsync();

            var result = new List<Booking>();
            foreach (var booking in candidates)
            {
                decimal remaining = await CalculateTotalAmountAsync(booking.Id);
                decimal outstanding = await _paymentService.GetOutstandingAmountAsync(booking.Id);

                // Chi hien booking da THU DU TIEN cho phan con lai (outstanding == 0)
                // va con phan CHUA XUAT HOA DON (remaining > 0) - san sang xuat ngay.
                if (remaining > 0 && outstanding == 0)
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

        public async Task<decimal> CalculateTotalAmountAsync(int bookingId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var booking = await db.Bookings
                .Include(b => b.BookingDetails)
                .Include(b => b.Invoices)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return 0;

            decimal bookingTotal = booking.TotalAmount.HasValue && booking.TotalAmount.Value > 0
                ? booking.TotalAmount.Value
                : booking.BookingDetails.Sum(bd => bd.Price);

            // SUA: tra ve phan CHUA duoc xuat hoa don = tong hien tai cua booking - tong
            // cac hoa don da xuat truoc do (tam ung luc check-in, neu co). Truoc day ham
            // nay luon tra ve toan bo TotalAmount, khien lan xuat hoa don thu 2 (bo sung)
            // se tinh trung tien voi lan dau.
            decimal alreadyInvoiced = booking.Invoices.Sum(i => i.TotalAmount ?? 0);
            decimal remaining = bookingTotal - alreadyInvoiced;

            return remaining < 0 ? 0 : remaining;
        }

        public async Task<InvoiceResult> CreateInvoiceAsync(int bookingId, int issuedByUserId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var booking = await db.Bookings
                .Include(b => b.BookingDetails)
                .Include(b => b.Invoices)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return new InvoiceResult { Success = false, ErrorMessage = "Không tìm thấy đơn đặt phòng." };

            // SUA: truoc day chi cho xuat hoa don khi da "checked_out". Gio cho phep xuat
            // ngay tu luc "checked_in" (hoa don tam ung luc nhan phong), va van cho phep
            // luc "checked_out" (hoa don bo sung neu co phat sinh).
            if (booking.Status != "checked_in" && booking.Status != "checked_out")
                return new InvoiceResult { Success = false, ErrorMessage = "Chỉ có thể xuất hóa đơn sau khi khách đã nhận phòng." };

            var issuerExists = await db.Users.AnyAsync(u => u.Id == issuedByUserId);
            if (!issuerExists)
                return new InvoiceResult { Success = false, ErrorMessage = "Mã nhân viên không tồn tại." };

            decimal outstanding = await _paymentService.GetOutstandingAmountAsync(bookingId);
            if (outstanding > 0)
            {
                return new InvoiceResult
                {
                    Success = false,
                    ErrorMessage = $"Khách còn nợ {outstanding:N0} đ. Vui lòng thu tiền (CreateStaffCollectedPaymentAsync / ConfirmCashPaymentAsync) trước khi xuất hóa đơn."
                };
            }

            // SUA: khong con chan "da co hoa don" - 1 booking duoc phep co nhieu hoa don
            // (tam ung + bo sung). Thay vao do chan khi khong con gi de xuat (remaining == 0).
            decimal remaining = await CalculateTotalAmountAsync(bookingId);
            if (remaining <= 0)
            {
                return new InvoiceResult { Success = false, ErrorMessage = "Đơn đặt phòng này không còn khoản nào cần xuất hóa đơn." };
            }

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