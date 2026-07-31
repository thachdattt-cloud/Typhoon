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

        public List<InvoicePreviewRoom> Rooms { get; set; } = new();

        // Cac yeu cau gia han / doi phong DA DUOC DUYET cho booking nay
        public List<InvoicePreviewService> ExtraServices { get; set; } = new();

        // So tien cua yeu cau DAT PHONG BAN DAU (truoc khi co yeu cau gia han/doi phong nao)
        // = Booking.TotalAmount hien tai - tong ExtraFee cac yeu cau da duyet
        public decimal OriginalAmount { get; set; }

        // Tong phu phi tu cac yeu cau da duyet (= tong ExtraFee)
        public decimal ExtraFeeAmount { get; set; }

        // = OriginalAmount + ExtraFeeAmount = Booking.TotalAmount hien tai
        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
    }

    public interface IInvoiceService
    {
        Task<List<Booking>> GetBookingsReadyForInvoiceAsync();
        Task<List<Invoice>> GetAllInvoicesAsync();
        Task<decimal> CalculateExtraFeeAsync(int bookingId);
        Task<InvoicePreview> GetInvoicePreviewAsync(int bookingId);
        Task<InvoiceResult> CreateCheckInInvoiceAsync(int bookingId, int issuedByUserId);
        Task<InvoiceResult> CreateInvoiceAsync(int bookingId, int issuedByUserId);
    }
}
