using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Manager.Commands
{
    // ─────────────────────────────────────────────────────────────────────
    // DTO dùng chung để trả về thông tin chính sách đặt cọc
    // ─────────────────────────────────────────────────────────────────────
    public class DepositPolicyDto
    {
        public Guid Id { get; set; }
        public Guid HotelId { get; set; }
        public int HoursBeforeCheckIn { get; set; }
        public decimal DepositPercentage { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────
    // CREATE: Manager tạo chính sách đặt cọc
    // ─────────────────────────────────────────────────────────────────────
    public class CreateDepositPolicyCommand : IRequest<Response<DepositPolicyDto>>
    {
        public Guid ManagerId { get; set; }
        public int HoursBeforeCheckIn { get; set; }
        public decimal DepositPercentage { get; set; }
    }

    public class CreateDepositPolicyCommandHandler
        : IRequestHandler<CreateDepositPolicyCommand, Response<DepositPolicyDto>>
    {
        private readonly IApplicationDbContext _context;
        public CreateDepositPolicyCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<DepositPolicyDto>> Handle(
            CreateDepositPolicyCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a =>
                    a.UserId == request.ManagerId &&
                    a.RoleInHotel == "manager" &&
                    a.IsActive,
                    cancellationToken);

            if (assignment == null)
                throw new ApiException("Bạn không được phân công quản lý khách sạn nào.");

            var existing = await _context.HotelDepositPolicies
                .FirstOrDefaultAsync(p => p.HotelId == assignment.HotelId, cancellationToken);

            if (existing != null)
                throw new ApiException("Khách sạn này đã có chính sách đặt cọc. Vui lòng sử dụng chức năng cập nhật.");

            var policy = new HotelDepositPolicy
            {
                Id = Guid.NewGuid(),
                HotelId = assignment.HotelId,
                HoursBeforeCheckIn = request.HoursBeforeCheckIn,
                DepositPercentage = request.DepositPercentage,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.HotelDepositPolicies.Add(policy);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<DepositPolicyDto>(DepositPolicyMapper.ToDto(policy),
                "Đã tạo chính sách đặt cọc thành công.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────────────────────────────
    public class UpdateDepositPolicyCommand : IRequest<Response<DepositPolicyDto>>
    {
        public Guid ManagerId { get; set; }
        public int HoursBeforeCheckIn { get; set; }
        public decimal DepositPercentage { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateDepositPolicyCommandHandler
        : IRequestHandler<UpdateDepositPolicyCommand, Response<DepositPolicyDto>>
    {
        private readonly IApplicationDbContext _context;
        public UpdateDepositPolicyCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<DepositPolicyDto>> Handle(
            UpdateDepositPolicyCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a =>
                    a.UserId == request.ManagerId &&
                    a.RoleInHotel == "manager" &&
                    a.IsActive,
                    cancellationToken);

            if (assignment == null)
                throw new ApiException("Bạn không được phân công quản lý khách sạn nào.");

            var policy = await _context.HotelDepositPolicies
                .FirstOrDefaultAsync(p => p.HotelId == assignment.HotelId, cancellationToken);

            if (policy == null)
                throw new ApiException("Chính sách đặt cọc chưa được tạo. Vui lòng tạo mới.");

            policy.HoursBeforeCheckIn = request.HoursBeforeCheckIn;
            policy.DepositPercentage = request.DepositPercentage;
            policy.IsActive = request.IsActive;
            policy.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return new Response<DepositPolicyDto>(DepositPolicyMapper.ToDto(policy),
                "Đã cập nhật chính sách đặt cọc thành công.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Validators
    // ─────────────────────────────────────────────────────────────────────
    public class CreateDepositPolicyCommandValidator : AbstractValidator<CreateDepositPolicyCommand>
    {
        public CreateDepositPolicyCommandValidator()
        {
            RuleFor(x => x.HoursBeforeCheckIn)
                .GreaterThan(0).WithMessage("Số giờ phải lớn hơn 0.");

            RuleFor(x => x.DepositPercentage)
                .InclusiveBetween(1, 100).WithMessage("Tỉ lệ cọc phải từ 1% đến 100%.");
        }
    }

    public class UpdateDepositPolicyCommandValidator : AbstractValidator<UpdateDepositPolicyCommand>
    {
        public UpdateDepositPolicyCommandValidator()
        {
            RuleFor(x => x.HoursBeforeCheckIn)
                .GreaterThan(0).WithMessage("Số giờ phải lớn hơn 0.");

            RuleFor(x => x.DepositPercentage)
                .InclusiveBetween(1, 100).WithMessage("Tỉ lệ cọc phải từ 1% đến 100%.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Mapper
    // ─────────────────────────────────────────────────────────────────────
    public static class DepositPolicyMapper
    {
        public static DepositPolicyDto ToDto(HotelDepositPolicy p) => new()
        {
            Id = p.Id,
            HotelId = p.HotelId,
            HoursBeforeCheckIn = p.HoursBeforeCheckIn,
            DepositPercentage = p.DepositPercentage,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }
}
