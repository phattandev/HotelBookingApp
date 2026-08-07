using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Bookings.Commands
{
    public class BookingItemInput
    {
        public Guid RoomTypeId { get; set; }
        public int NumRooms { get; set; }
    }

    /// <summary>
    /// Command tạo đơn đặt phòng mới (có thể chứa nhiều loại phòng) từ phía khách hàng.
    /// </summary>
    public class CreateBookingCommand : IRequest<Response<Guid>>
    {
        public Guid CustomerId { get; set; }    // Set từ JWT token
        public Guid HotelId { get; set; }
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public int NumAdults { get; set; } = 1;
        public int NumChildren { get; set; } = 0;
        public string GuestName { get; set; } = null!;
        public string GuestPhone { get; set; } = null!;
        public string GuestEmail { get; set; } = null!;
        public string? SpecialRequests { get; set; }
        
        public List<BookingItemInput> Items { get; set; } = new();
    }

    public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Response<Guid>>
    {
        private readonly IApplicationDbContext _context;
        public CreateBookingCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<Guid>> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
        {
            var hotel = await _context.Hotels
                .Include(h => h.Business)
                .FirstOrDefaultAsync(h => h.Id == request.HotelId && h.IsActive, cancellationToken);

            if (hotel == null)
                throw new ApiException("Khách sạn không tồn tại hoặc đã ngừng kinh doanh.");

            if (hotel.Business != null && hotel.Business.OwnerId == request.CustomerId)
                throw new ApiException("Bạn không thể tự đặt phòng tại khách sạn của chính mình.");

            var roomTypeIds = request.Items.Select(i => i.RoomTypeId).Distinct().ToList();
            var roomTypes = await _context.RoomTypes
                .Where(rt => roomTypeIds.Contains(rt.Id) && rt.HotelId == request.HotelId && rt.IsActive)
                .ToDictionaryAsync(rt => rt.Id, cancellationToken);

            if (roomTypes.Count != roomTypeIds.Count)
                throw new ApiException("Một hoặc nhiều loại phòng không tồn tại, không thuộc khách sạn này hoặc đã ngừng kinh doanh.");

            var numNights = request.CheckOutDate.DayNumber - request.CheckInDate.DayNumber;
            Guid newBookingId = Guid.Empty;

            await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    decimal totalPrice = 0;
                    var bookingItems = new List<BookingItem>();
                    int totalAdultCapacity = 0;
                    int totalChildCapacity = 0;

                    foreach (var inputItem in request.Items)
                    {
                        var rt = roomTypes[inputItem.RoomTypeId];

                        var bookedRooms = await _context.BookingItems
                            .Where(bi =>
                                bi.RoomTypeId == rt.Id &&
                                (bi.Booking.Status == BookingStatus.Pending || bi.Booking.Status == BookingStatus.Approved || bi.Booking.Status == BookingStatus.Confirmed) &&
                                bi.Booking.CheckInDate < request.CheckOutDate &&
                                bi.Booking.CheckOutDate > request.CheckInDate)
                            .SumAsync(bi => (int?)bi.NumRooms, cancellationToken) ?? 0;

                        var availableRooms = rt.TotalRooms - bookedRooms;
                        if (availableRooms < inputItem.NumRooms)
                            throw new ApiException($"Loại phòng '{rt.Name}' chỉ còn {availableRooms} phòng trống.");

                        totalAdultCapacity += rt.MaxAdults * inputItem.NumRooms;
                        totalChildCapacity += rt.MaxChildren * inputItem.NumRooms;

                        var subTotal = rt.BasePrice * inputItem.NumRooms * numNights;
                        totalPrice += subTotal;

                        bookingItems.Add(new BookingItem
                        {
                            Id = Guid.NewGuid(),
                            RoomTypeId = rt.Id,
                            NumRooms = inputItem.NumRooms,
                            UnitPrice = rt.BasePrice,
                            SubTotal = subTotal
                        });
                    }

                    if (request.NumAdults > totalAdultCapacity || request.NumChildren > totalChildCapacity)
                        throw new ApiException($"Sức chứa tổng cộng của các phòng đã chọn không đủ cho {request.NumAdults} người lớn và {request.NumChildren} trẻ em.");

                    var cancelPolicy = await _context.HotelCancellationPolicies
                        .FirstOrDefaultAsync(p => p.HotelId == request.HotelId && p.IsActive, cancellationToken);

                    var depositPolicy = await _context.HotelDepositPolicies
                        .FirstOrDefaultAsync(p => p.HotelId == request.HotelId && p.IsActive, cancellationToken);
                    var depositPercentage = depositPolicy?.DepositPercentage ?? 50m;

                    var depositAmount = Math.Round(totalPrice * (depositPercentage / 100m), 0);
                    var now = DateTime.UtcNow;

                    var booking = new Booking
                    {
                        Id = Guid.NewGuid(),
                        HotelId = request.HotelId,
                        CustomerId = request.CustomerId,
                        CheckInDate = request.CheckInDate,
                        CheckOutDate = request.CheckOutDate,
                        NumAdults = request.NumAdults,
                        NumChildren = request.NumChildren,
                        TotalPrice = totalPrice,
                        Status = BookingStatus.Pending,
                        GuestName = request.GuestName,
                        GuestPhone = request.GuestPhone,
                        GuestEmail = request.GuestEmail,
                        SpecialRequests = request.SpecialRequests,
                        CancellationPolicyId = cancelPolicy?.Id,
                        PenaltyPercentageSnapshot = cancelPolicy?.PenaltyPercentage,
                        DepositPolicyId = depositPolicy?.Id,
                        DepositPercentageSnapshot = depositPercentage,
                        PaymentStatus = PaymentStatus.Unpaid,
                        DepositAmount = depositAmount,
                        CreatedAt = now,
                        UpdatedAt = now,
                        Items = bookingItems
                    };

                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    newBookingId = booking.Id;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            return new Response<Guid>(newBookingId, "Đặt phòng thành công! Vui lòng chờ khách sạn xác nhận.");
        }
    }

    public class BookingItemInputValidator : AbstractValidator<BookingItemInput>
    {
        public BookingItemInputValidator()
        {
            RuleFor(x => x.RoomTypeId).NotEmpty();
            RuleFor(x => x.NumRooms).GreaterThanOrEqualTo(1).WithMessage("Số phòng ít nhất là 1.");
        }
    }

    public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
    {
        public CreateBookingCommandValidator()
        {
            RuleFor(x => x.HotelId).NotEmpty();
            RuleFor(x => x.Items).NotEmpty().WithMessage("Vui lòng chọn ít nhất 1 loại phòng.");
            RuleForEach(x => x.Items).SetValidator(new BookingItemInputValidator());

            RuleFor(x => x.CheckInDate).NotEmpty()
                .Must(checkIn =>
                {
                    var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");
                    var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
                    var today = DateOnly.FromDateTime(nowVn);
                    return checkIn >= today;
                }).WithMessage("Ngày nhận phòng không được là ngày trong quá khứ.");

            RuleFor(x => x.CheckOutDate).NotEmpty()
                .GreaterThan(x => x.CheckInDate).WithMessage("Ngày trả phòng phải sau ngày nhận phòng.")
                .Must((req, checkOut) => (checkOut.DayNumber - req.CheckInDate.DayNumber) <= 30)
                .WithMessage("Hệ thống chỉ hỗ trợ đặt phòng tối đa 30 đêm.");

            RuleFor(x => x.NumAdults).GreaterThanOrEqualTo(1).WithMessage("Số người lớn ít nhất là 1.");
            RuleFor(x => x.NumChildren).GreaterThanOrEqualTo(0).WithMessage("Số trẻ em không được âm.");

            RuleFor(x => x.GuestName).NotEmpty().WithMessage("Vui lòng nhập tên người đặt.")
                .Matches(@"^[\p{L}\s]+$").WithMessage("Tên người đặt không được chứa số hoặc ký tự đặc biệt.");

            RuleFor(x => x.GuestPhone).NotEmpty().WithMessage("Vui lòng nhập số điện thoại.")
                .Matches(@"^(0[3|5|7|8|9])[0-9]{8}$").WithMessage("Số điện thoại không hợp lệ.");

            RuleFor(x => x.GuestEmail).NotEmpty().WithMessage("Vui lòng nhập email.")
                .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$").WithMessage("Email không hợp lệ.");
                
            RuleFor(x => x.SpecialRequests).MaximumLength(500).WithMessage("Yêu cầu đặc biệt tối đa 500 ký tự.");
        }
    }
}
