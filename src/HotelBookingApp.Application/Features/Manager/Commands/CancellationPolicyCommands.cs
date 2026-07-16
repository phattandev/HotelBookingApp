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
    // DTO dùng chung để trả về thông tin policy
    // ─────────────────────────────────────────────────────────────────────
    public class CancellationPolicyDto
    {
        public Guid Id { get; set; }
        public Guid HotelId { get; set; }
        public string PolicyName { get; set; } = null!;
        public int HoursBeforeCheckIn { get; set; }
        public decimal PenaltyPercentage { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────
    // CREATE: Manager tạo chính sách hủy cho khách sạn mình quản lý
    // ─────────────────────────────────────────────────────────────────────
    public class CreateCancellationPolicyCommand : IRequest<Response<CancellationPolicyDto>>
    {
        public Guid ManagerId { get; set; }     // Lấy từ JWT
        public string PolicyName { get; set; } = null!;
        public int HoursBeforeCheckIn { get; set; }
        public decimal PenaltyPercentage { get; set; }
    }

    public class CreateCancellationPolicyCommandHandler
        : IRequestHandler<CreateCancellationPolicyCommand, Response<CancellationPolicyDto>>
    {
        private readonly IApplicationDbContext _context;
        public CreateCancellationPolicyCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<CancellationPolicyDto>> Handle(
            CreateCancellationPolicyCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm assignment để biết Manager quản lý hotel nào
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a =>
                    a.UserId == request.ManagerId &&
                    a.RoleInHotel == "manager" &&
                    a.IsActive,
                    cancellationToken);

            if (assignment == null)
                throw new ApiException("Bạn không được phân công quản lý khách sạn nào.");

            // 2. Kiểm tra hotel này đã có policy chưa (1 hotel = 1 policy)
            var existing = await _context.HotelCancellationPolicies
                .FirstOrDefaultAsync(p => p.HotelId == assignment.HotelId, cancellationToken);

            if (existing != null)
                throw new ApiException("Khách sạn này đã có chính sách hủy phòng. Vui lòng sử dụng chức năng cập nhật.");

            var policy = new HotelCancellationPolicy
            {
                Id = Guid.NewGuid(),
                HotelId = assignment.HotelId,
                PolicyName = request.PolicyName,
                HoursBeforeCheckIn = request.HoursBeforeCheckIn,
                PenaltyPercentage = request.PenaltyPercentage,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.HotelCancellationPolicies.Add(policy);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<CancellationPolicyDto>(CancellationPolicyMapper.ToDto(policy),
                "Đã tạo chính sách hủy phòng thành công.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // UPDATE: Manager cập nhật chính sách hiện có
    // ─────────────────────────────────────────────────────────────────────
    public class UpdateCancellationPolicyCommand : IRequest<Response<CancellationPolicyDto>>
    {
        public Guid ManagerId { get; set; }
        public string PolicyName { get; set; } = null!;
        public int HoursBeforeCheckIn { get; set; }
        public decimal PenaltyPercentage { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateCancellationPolicyCommandHandler
        : IRequestHandler<UpdateCancellationPolicyCommand, Response<CancellationPolicyDto>>
    {
        private readonly IApplicationDbContext _context;
        public UpdateCancellationPolicyCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<CancellationPolicyDto>> Handle(
            UpdateCancellationPolicyCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a =>
                    a.UserId == request.ManagerId &&
                    a.RoleInHotel == "manager" &&
                    a.IsActive,
                    cancellationToken);

            if (assignment == null)
                throw new ApiException("Bạn không được phân công quản lý khách sạn nào.");

            var policy = await _context.HotelCancellationPolicies
                .FirstOrDefaultAsync(p => p.HotelId == assignment.HotelId, cancellationToken);

            if (policy == null)
                throw new ApiException("Khách sạn chưa có chính sách hủy phòng. Vui lòng tạo mới.");

            policy.PolicyName = request.PolicyName;
            policy.HoursBeforeCheckIn = request.HoursBeforeCheckIn;
            policy.PenaltyPercentage = request.PenaltyPercentage;
            policy.IsActive = request.IsActive;
            policy.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return new Response<CancellationPolicyDto>(CancellationPolicyMapper.ToDto(policy),
                "Đã cập nhật chính sách hủy phòng thành công.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Validators
    // ─────────────────────────────────────────────────────────────────────
    public class CreateCancellationPolicyCommandValidator : AbstractValidator<CreateCancellationPolicyCommand>
    {
        public CreateCancellationPolicyCommandValidator()
        {
            RuleFor(x => x.PolicyName)
                .NotEmpty().WithMessage("Tên chính sách không được để trống.")
                .MaximumLength(100).WithMessage("Tên chính sách tối đa 100 ký tự.");

            RuleFor(x => x.HoursBeforeCheckIn)
                .GreaterThan(0).WithMessage("Mốc giờ hủy phải lớn hơn 0.");

            RuleFor(x => x.PenaltyPercentage)
                .InclusiveBetween(0, 100).WithMessage("Tỉ lệ phạt phải nằm trong khoảng 0-100%.");
        }
    }

    public class UpdateCancellationPolicyCommandValidator : AbstractValidator<UpdateCancellationPolicyCommand>
    {
        public UpdateCancellationPolicyCommandValidator()
        {
            RuleFor(x => x.PolicyName)
                .NotEmpty().WithMessage("Tên chính sách không được để trống.")
                .MaximumLength(100).WithMessage("Tên chính sách tối đa 100 ký tự.");

            RuleFor(x => x.HoursBeforeCheckIn)
                .GreaterThan(0).WithMessage("Mốc giờ hủy phải lớn hơn 0.");

            RuleFor(x => x.PenaltyPercentage)
                .InclusiveBetween(0, 100).WithMessage("Tỉ lệ phạt phải nằm trong khoảng 0-100%.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helper mapper
    // ─────────────────────────────────────────────────────────────────────
    internal static class CancellationPolicyMapper
    {
        public static CancellationPolicyDto ToDto(HotelCancellationPolicy p) => new()
        {
            Id = p.Id,
            HotelId = p.HotelId,
            PolicyName = p.PolicyName,
            HoursBeforeCheckIn = p.HoursBeforeCheckIn,
            PenaltyPercentage = p.PenaltyPercentage,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }
}
