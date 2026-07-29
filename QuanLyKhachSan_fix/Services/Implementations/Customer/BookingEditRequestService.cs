using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Services.Interfaces.Customer;

namespace QuanLyKhachSan_fix.Services.Implementations.Customer
{
    public class BookingEditRequestService : IBookingEditRequestService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public BookingEditRequestService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        // SUA: kiem tra phong con trong cho khoang ngay MOI (tinh ca doan gia han them)
        // truoc khi cho gui yeu cau, loai tru chinh booking detail dang xet (vi ban than
        // no dang giu phong nay cho khoang ngay cu, khong tinh la "trung").
        private static async Task<bool> IsRoomFreeForExtendAsync(AppDbContext db, BookingDetail detail, DateTime newCheckOutDate)
        {
            bool hasConflict = await db.BookingDetails
                .Where(bd => bd.RoomId == detail.RoomId && bd.Id != detail.Id)
                .Where(bd => bd.Status != "cancelled" && bd.Status != "checked_out")
                .Where(bd => bd.Booking.CheckInDate < newCheckOutDate.Date && bd.Booking.CheckOutDate > detail.Booking.CheckInDate.Date)
                .AnyAsync();

            return !hasConflict;
        }

        // SUA: kiem tra phong MOI muon doi sang co con trong trong dung khoang ngay
        // cua booking hien tai khong.
        private static async Task<bool> IsRoomFreeForChangeAsync(AppDbContext db, int newRoomId, DateTime checkIn, DateTime checkOut)
        {
            bool hasConflict = await db.BookingDetails
                .Where(bd => bd.RoomId == newRoomId)
                .Where(bd => bd.Status != "cancelled" && bd.Status != "checked_out")
                .Where(bd => bd.Booking.CheckInDate < checkOut.Date && bd.Booking.CheckOutDate > checkIn.Date)
                .AnyAsync();

            return !hasConflict;
        }

        public async Task<EditRequestResult> CreateExtendRequestAsync(int bookingDetailId, DateTime newCheckOutDate)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var detail = await db.BookingDetails.Include(bd => bd.Booking).FirstOrDefaultAsync(bd => bd.Id == bookingDetailId);
            if (detail == null)
            {
                return new EditRequestResult { Success = false, ErrorMessage = "Không tìm thấy đặt phòng." };
            }

            if (newCheckOutDate.Date <= detail.Booking.CheckOutDate.Date)
            {
                return new EditRequestResult { Success = false, ErrorMessage = "Ngày gia hạn phải sau ngày trả phòng hiện tại." };
            }

            // SUA: chan gui yeu cau neu phong da co nguoi khac dat trong khoang ngay gia han
            bool isFree = await IsRoomFreeForExtendAsync(db, detail, newCheckOutDate);
            if (!isFree)
            {
                return new EditRequestResult { Success = false, ErrorMessage = "Phòng đã được đặt bởi khách khác trong khoảng thời gian gia hạn, không thể gửi yêu cầu." };
            }

            // SUA: tinh truoc so tien phat sinh (so dem them x gia phong/dem) va luu vao
            // ExtraFee, de khi le tan duyet thi cong thang vao TotalAmount cua booking -
            // truoc day gia han xong ma TotalAmount khong doi, khach o them mien phi.
            int extraNights = (newCheckOutDate.Date - detail.Booking.CheckOutDate.Date).Days;
            decimal extraFee = detail.Price * extraNights;

            var request = new BookingEditRequest
            {
                BookingDetailId = bookingDetailId,
                RequestType = "extend",
                NewCheckOutDate = newCheckOutDate.Date,
                ExtraFee = extraFee,
                Status = "pending",
                RequestedAt = DateTime.Now
            };

            db.BookingEditRequests.Add(request);
            await db.SaveChangesAsync();

            return new EditRequestResult { Success = true, EditRequest = request };
        }

        public async Task<EditRequestResult> CreateChangeRoomRequestAsync(int bookingDetailId, int newRoomId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var detail = await db.BookingDetails.Include(bd => bd.Booking).FirstOrDefaultAsync(bd => bd.Id == bookingDetailId);
            if (detail == null)
            {
                return new EditRequestResult { Success = false, ErrorMessage = "Không tìm thấy đặt phòng." };
            }

            var newRoom = await db.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.Id == newRoomId);
            if (newRoom == null)
            {
                return new EditRequestResult { Success = false, ErrorMessage = "Không tìm thấy phòng muốn đổi." };
            }

            // SUA: chan gui yeu cau neu phong moi khong con trong trong dung khoang ngay
            // cua booking hien tai (truoc day chi kiem tra phong co ton tai hay khong).
            bool isFree = await IsRoomFreeForChangeAsync(db, newRoomId, detail.Booking.CheckInDate, detail.Booking.CheckOutDate);
            if (!isFree)
            {
                return new EditRequestResult { Success = false, ErrorMessage = "Phòng muốn đổi đã có khách khác đặt trong khoảng thời gian này, không thể gửi yêu cầu." };
            }

            // SUA: tinh truoc chenh lech gia (co the am neu doi sang phong re hon) x so
            // dem cua booking, luu vao ExtraFee - truoc day doi phong xong gia phong cu
            // (BookingDetail.Price) van giu nguyen, khong phan anh dung gia phong moi.
            int nights = Math.Max(1, (detail.Booking.CheckOutDate.Date - detail.Booking.CheckInDate.Date).Days);
            decimal newPricePerNight = newRoom.RoomType?.BasePrice ?? 0m;
            decimal extraFee = (newPricePerNight - detail.Price) * nights;

            var request = new BookingEditRequest
            {
                BookingDetailId = bookingDetailId,
                RequestType = "change_room",
                OldRoomId = detail.RoomId,
                NewRoomId = newRoomId,
                ExtraFee = extraFee,
                Status = "pending",
                RequestedAt = DateTime.Now
            };

            db.BookingEditRequests.Add(request);
            await db.SaveChangesAsync();

            return new EditRequestResult { Success = true, EditRequest = request };
        }

        public async Task<List<BookingEditRequest>> GetRequestsByCustomerAsync(int customerId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.BookingEditRequests
                .Include(r => r.BookingDetail).ThenInclude(bd => bd.Booking)
                .Include(r => r.OldRoom)
                .Include(r => r.NewRoom)
                .Where(r => r.BookingDetail.Booking.CustomerId == customerId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<List<BookingEditRequest>> GetPendingRequestsAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.BookingEditRequests
                .Include(r => r.BookingDetail).ThenInclude(bd => bd.Booking)
                .Include(r => r.OldRoom)
                .Include(r => r.NewRoom)
                .Where(r => r.Status == "pending")
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<EditRequestResult> ApproveRequestAsync(int requestId, int handledByUserId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var request = await db.BookingEditRequests
                .Include(r => r.BookingDetail).ThenInclude(bd => bd.Booking)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                return new EditRequestResult { Success = false, ErrorMessage = "Không tìm thấy yêu cầu." };
            }

            if (request.Status != "pending")
            {
                return new EditRequestResult { Success = false, ErrorMessage = "Yêu cầu đã được xử lý." };
            }

            var booking = request.BookingDetail.Booking;

            if (request.RequestType == "extend" && request.NewCheckOutDate.HasValue)
            {
                // SUA: re-check phong con trong ngay tai thoi diem duyet (tinh trang co
                // the da doi so voi luc khach gui yeu cau).
                bool isFree = await IsRoomFreeForExtendAsync(db, request.BookingDetail, request.NewCheckOutDate.Value);
                if (!isFree)
                {
                    return new EditRequestResult { Success = false, ErrorMessage = "Phòng đã bị đặt bởi khách khác trong khoảng thời gian gia hạn, không thể duyệt yêu cầu này." };
                }

                booking.CheckOutDate = request.NewCheckOutDate.Value;
                // SUA: cong tien gia han vao TotalAmount cua booking
                booking.TotalAmount = (booking.TotalAmount ?? 0) + (request.ExtraFee ?? 0);
                booking.UpdatedAt = DateTime.Now;
            }
            else if (request.RequestType == "change_room" && request.NewRoomId.HasValue)
            {
                var newRoom = await db.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.Id == request.NewRoomId.Value);
                if (newRoom == null)
                {
                    return new EditRequestResult { Success = false, ErrorMessage = "Không tìm thấy phòng muốn đổi." };
                }

                // SUA: re-check phong moi con trong tai thoi diem duyet
                bool isFree = await IsRoomFreeForChangeAsync(db, request.NewRoomId.Value, booking.CheckInDate, booking.CheckOutDate);
                if (!isFree)
                {
                    return new EditRequestResult { Success = false, ErrorMessage = "Phòng muốn đổi đã có khách khác đặt, không thể duyệt yêu cầu này." };
                }

                int nights = Math.Max(1, (booking.CheckOutDate.Date - booking.CheckInDate.Date).Days);
                decimal newPricePerNight = newRoom.RoomType?.BasePrice ?? 0m;
                // SUA: tinh lai chenh lech gia bang gia HIEN TAI cua phong moi (phong ngua
                // gia da thay doi ke tu luc khach gui yeu cau), roi cap nhat luon
                // BookingDetail.Price sang gia phong moi va cong chenh lech vao TotalAmount.
                decimal recalculatedExtraFee = (newPricePerNight - request.BookingDetail.Price) * nights;

                request.BookingDetail.RoomId = request.NewRoomId.Value;
                request.BookingDetail.Price = newPricePerNight;
                booking.TotalAmount = (booking.TotalAmount ?? 0) + recalculatedExtraFee;
                booking.UpdatedAt = DateTime.Now;

                request.ExtraFee = recalculatedExtraFee;
            }

            request.Status = "approved";
            request.HandledBy = handledByUserId;
            request.HandledAt = DateTime.Now;

            await db.SaveChangesAsync();

            return new EditRequestResult { Success = true, EditRequest = request };
        }

        public async Task<EditRequestResult> RejectRequestAsync(int requestId, int handledByUserId, string? reason)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var request = await db.BookingEditRequests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null)
            {
                return new EditRequestResult { Success = false, ErrorMessage = "Không tìm thấy yêu cầu." };
            }

            if (request.Status != "pending")
            {
                return new EditRequestResult { Success = false, ErrorMessage = "Yêu cầu đã được xử lý." };
            }

            request.Status = "rejected";
            request.HandledBy = handledByUserId;
            request.HandledAt = DateTime.Now;

            await db.SaveChangesAsync();

            return new EditRequestResult { Success = true, EditRequest = request };
        }
    }
}