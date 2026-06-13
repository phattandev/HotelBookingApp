using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.BusinessStaff.Commands
{
    public class CreateEmployeeCommand : IRequest<Response<string>>
    {
        public Guid PartnerId { get; set; } // Sẽ được gán từ Controller qua Token
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string RoleName { get; set; } = null!; // "manager" hoặc "employee"
    }

    public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public CreateEmployeeCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<Response<string>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email, cancellationToken);
            if (emailExists) throw new ApiException("Email này đã tồn tại trong hệ thống.");

            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.OwnerId == request.PartnerId, cancellationToken);
            if (business == null) throw new ApiException("Hồ sơ doanh nghiệp không tồn tại.");

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.RoleName.ToLower(), cancellationToken);
            if (role == null) throw new ApiException("Quyền hạn không hợp lệ.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                Phone = request.Phone,
                PasswordHash = _passwordHasher.HashPasswordEnhanced(request.Password), // Dùng chung hàm Hash để đăng nhập được
                RoleId = role.Id,
                BusinessId = business.Id, // Liên kết nhân viên vào đúng doanh nghiệp
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<string>($"Đã thêm tài khoản {request.RoleName} thành công.");
        }
    }
}
