using QuanLyKhachSan_fix.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuanLyKhachSan_fix.Services.Interfaces.Receptionist
{
    public class InvoiceResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Invoice? Invoice { get; set; }
    }

    public class InvoicePreviewRoom
    {
        public string RoomNumber { get; set; } = "";
        public string RoomTypeName { get; set; } = "";
        public decimal Price { get; set; }
        public int? GuestCount { get; set; }
    }

    public class InvoicePreviewService
    {
        public string RequestType { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal ExtraFee { get; set; }
        public DateTime? HandledAt { get; set; }
    }

    public class InvoicePreview
    {
        public int BookingId { get; set; }
        public string? BookingCode { get; set; }
        public string? CustomerFullName { get; set; }
        public string? CustomerUsername { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }

        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int Nights { get; set; }

        // Phong da o
        public List<InvoicePreviewRoom> Rooms { get; set; } = new();

        // Tat ca dich vu / yeu cau gia han - doi phong DA DUOC DUYET trong ky luu tru
        public List<InvoicePreviewService> ExtraServices { get; set; } = new();

        // Tien phong ban dau (chua gom dich vu phat sinh) = TotalAmount - tong ExtraFee
        public decimal OriginalAmount { get; set; }

        // Tong phi dich vu phat sinh (gia han / doi phong da duyet)
        public decimal ExtraFeeAmount { get; set; }

        // = OriginalAmount + ExtraFeeAmount = Booking.TotalAmount hien tai. Day la so tien
        // hoa don DUY NHAT cua booking se the hien (mot booking = mot hoa don, xuat luc tra phong).
        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
    }

    public interface IInvoiceService
    {
        Task<List<Booking>> GetBookingsReadyForInvoiceAsync();
        Task<List<Invoice>> GetAllInvoicesAsync();
        Task<InvoicePreview> GetInvoicePreviewAsync(int bookingId);
        Task<InvoiceResult> CreateInvoiceAsync(int bookingId, int issuedByUserId);
    }
}
