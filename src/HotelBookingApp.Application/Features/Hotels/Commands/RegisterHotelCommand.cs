using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Hotels.Commands
{
    public class RegisterHotelCommand : IRequest<Response<Guid>>
    {
        public Guid PartnerId { get; set; }
        public string Name { get; set; } = null!;
        public string AddressLine { get; set; } = null!;
        public string TaxCode { get; set; } = null!;
        public Guid WardId { get; set; }
        public string? Description { get; set; }
        public int? StarRating { get; set; }
    }

    public class RegisterHotelCommandHandler : IRequestHandler<RegisterHotelCommand, Response<Guid>>
    {
        private readonly IApplicationDbContext _context;
        public RegisterHotelCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<Guid>> Handle(RegisterHotelCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra doanh nghiệp tồn tại và đã được duyệt
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.OwnerId == request.PartnerId, cancellationToken);

            if (business == null)
                throw new ApiException("Không tìm thấy hồ sơ doanh nghiệp hợp lệ.");

            if (business.VerificationStatus != BusinessVerificationStatus.Approved)
                throw new ApiException("Doanh nghiệp của bạn chưa được Admin phê duyệt. Không thể đăng ký khách sạn mới.");

            // 2. Kiểm tra ward tồn tại
            var wardExists = await _context.Wards.AnyAsync(w => w.Id == request.WardId, cancellationToken);
            if (!wardExists)
                throw new ApiException("Địa chỉ phường/xã không hợp lệ.");

            var hotel = new Hotel
            {
                Id = Guid.NewGuid(),
                BusinessId = business.Id,
                Name = request.Name,
                AddressLine = request.AddressLine,
                WardId = request.WardId,
                TaxCode = request.TaxCode,
                Description = request.Description,
                StarRating = request.StarRating,
                ApprovalStatus = HotelApprovalStatus.Pending,
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Hotels.Add(hotel);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<Guid>(hotel.Id, "Đăng ký khách sạn thành công, vui lòng chờ Admin phê duyệt.");
        }
    }

    public class RegisterHotelCommandValidator : AbstractValidator<RegisterHotelCommand>
    {
        public RegisterHotelCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên khách sạn không được để trống.")
                .MaximumLength(200).WithMessage("Tên khách sạn tối đa 200 ký tự.");

            RuleFor(x => x.AddressLine)
                .NotEmpty().WithMessage("Địa chỉ khách sạn không được để trống.")
                .MaximumLength(255).WithMessage("Địa chỉ tối đa 255 ký tự.");

            RuleFor(x => x.TaxCode)
                .NotEmpty().WithMessage("Mã số thuế khách sạn không được để trống.")
                .MaximumLength(50).WithMessage("Mã số thuế tối đa 50 ký tự.");

            RuleFor(x => x.WardId)
                .NotEmpty().WithMessage("Vui lòng chọn phường/xã cho địa chỉ khách sạn.");

            RuleFor(x => x.StarRating)
                .InclusiveBetween(1, 5).WithMessage("Số sao phải từ 1 đến 5.")
                .When(x => x.StarRating.HasValue);
        }
    }
}
