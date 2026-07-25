using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyKhachSan_fix.Models;

namespace QuanLyKhachSan_fix.Services.Interfaces.Customer
{
    public class EditRequestResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public BookingEditRequest? EditRequest { get; set; }
    }

    public interface IBookingEditRequestService
    {
        // Khach gui yeu cau gia han ngay tra phong
        Task<EditRequestResult> CreateExtendRequestAsync(int bookingDetailId, DateTime newCheckOutDate);

        // Khach gui yeu cau doi sang phong khac
        Task<EditRequestResult> CreateChangeRoomRequestAsync(int bookingDetailId, int newRoomId);

        // Danh sach yeu cau cua 1 khach hang (dung cho FormEditRequest / FormMyBookings)
        Task<List<BookingEditRequest>> GetRequestsByCustomerAsync(int customerId);

        // Danh sach yeu cau dang cho duyet (Thanh vien 2 dung trong FormApproveRequest)
        Task<List<BookingEditRequest>> GetPendingRequestsAsync();

        // Le tan duyet / tu choi yeu cau (Thanh vien 2 trien khai phan goi)
        Task<EditRequestResult> ApproveRequestAsync(int requestId, int handledByUserId);
        Task<EditRequestResult> RejectRequestAsync(int requestId, int handledByUserId, string? reason);
    }
}
