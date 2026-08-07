using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Bookings.Queries
{
    public class ExtensionItemInput
    {
        public Guid RoomTypeId { get; set; }
        public int NumRooms { get; set; }
    }

    public class CheckExtensionAvailabilityQuery : IRequest<Response<ExtensionAvailabilityDto>>
    {
        public Guid BookingId { get; set; }
        public Guid ManagerId { get; set; }
        public DateOnly NewCheckOutDate { get; set; }
        public List<ExtensionItemInput> Items { get; set; } = new();
    }

    public class ExtensionAvailabilityDto
    {
        public bool AllAvailable { get; set; }
        public List<RoomAvailabilityItemDto> Items { get; set; } = new();
        public decimal EstimatedTotal { get; set; }
    }

    public class RoomAvailabilityItemDto
    {
        public Guid RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } = null!;
        public int RequiredRooms { get; set; }
        public int AvailableRooms { get; set; }
        public bool IsAvailable { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class CheckExtensionAvailabilityQueryHandler : IRequestHandler<CheckExtensionAvailabilityQuery, Response<ExtensionAvailabilityDto>>
    {
        private readonly IApplicationDbContext _context;

        public CheckExtensionAvailabilityQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Response<ExtensionAvailabilityDto>> Handle(CheckExtensionAvailabilityQuery request, CancellationToken cancellationToken)
        {
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a => a.UserId == request.ManagerId && a.IsActive, cancellationToken);

            if (assignment == null)
                throw new ApiException("Bạn không có quyền quản lý khách sạn nào.");

            var originalBooking = await _context.Bookings
                .Include(b => b.Items)
                    .ThenInclude(i => i.RoomType)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId && b.HotelId == assignment.HotelId, cancellationToken);

            if (originalBooking == null)
                throw new ApiException("Không tìm thấy đơn đặt phòng.");

            if (originalBooking.Status != BookingStatus.Confirmed || originalBooking.PaymentStatus != PaymentStatus.Paid)
                throw new ApiException("Chỉ có thể gia hạn cho đơn đã được xác nhận và thanh toán cọc.");

            var numNights = request.NewCheckOutDate.DayNumber - originalBooking.CheckOutDate.DayNumber;
            if (numNights <= 0)
                throw new ApiException("Ngày trả phòng mới phải sau ngày trả phòng hiện tại.");
            if (numNights > 30)
                throw new ApiException("Chỉ có thể gia hạn tối đa 30 đêm.");

            var result = new ExtensionAvailabilityDto
            {
                AllAvailable = true,
                EstimatedTotal = 0
            };

            foreach (var reqItem in request.Items)
            {
                if (reqItem.NumRooms <= 0) continue;

                var originalItem = originalBooking.Items.FirstOrDefault(i => i.RoomTypeId == reqItem.RoomTypeId);
                if (originalItem == null)
                    throw new ApiException("Loại phòng yêu cầu không nằm trong đơn gốc.");
                if (reqItem.NumRooms > originalItem.NumRooms)
                    throw new ApiException($"Số lượng phòng yêu cầu cho loại phòng '{originalItem.RoomType.Name}' vượt quá số lượng trong đơn gốc.");

                var rt = originalItem.RoomType;

                var bookedRooms = await _context.BookingItems
                    .Where(bi =>
                        bi.RoomTypeId == rt.Id &&
                        (bi.Booking.Status == BookingStatus.Pending || bi.Booking.Status == BookingStatus.Approved || bi.Booking.Status == BookingStatus.Confirmed) &&
                        bi.Booking.CheckInDate < request.NewCheckOutDate &&
                        bi.Booking.CheckOutDate > originalBooking.CheckOutDate)
                    .SumAsync(bi => (int?)bi.NumRooms, cancellationToken) ?? 0;

                var availableRooms = rt.TotalRooms - bookedRooms;
                var isAvailable = availableRooms >= reqItem.NumRooms;
                var subTotal = originalItem.UnitPrice * reqItem.NumRooms * numNights;

                if (!isAvailable)
                {
                    result.AllAvailable = false;
                }

                result.EstimatedTotal += subTotal;

                result.Items.Add(new RoomAvailabilityItemDto
                {
                    RoomTypeId = rt.Id,
                    RoomTypeName = rt.Name,
                    RequiredRooms = reqItem.NumRooms,
                    AvailableRooms = availableRooms,
                    IsAvailable = isAvailable,
                    UnitPrice = originalItem.UnitPrice,
                    SubTotal = subTotal
                });
            }

            if (result.Items.Count == 0)
                throw new ApiException("Vui lòng chọn ít nhất một loại phòng để gia hạn.");

            return new Response<ExtensionAvailabilityDto>(result, "Kiểm tra phòng trống thành công.");
        }
    }
}
