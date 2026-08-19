using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
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
        public Guid PartnerId { get; set; } // Gán từ Controller qua JWT Token
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Password { get; set; } = null!;
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

            // Luôn gán role "staff" cho nhân viên mới — Partner sẽ phân công sau
            var staffRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == "staff", cancellationToken);
            if (staffRole == null) throw new ApiException("Role 'staff' chưa được cấu hình. Vui lòng liên hệ Admin.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                Phone = request.Phone,
                PasswordHash = _passwordHasher.HashPasswordEnhanced(request.Password),
                RoleId = staffRole.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            var businessStaff = new Domain.Models.BusinessStaff
            {
                Id = Guid.NewGuid(),
                BusinessId = business.Id,
                UserId = user.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.BusinessStaff.Add(businessStaff);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<string> { Succeeded = true, Data = "Success", Message = $"Đã thêm nhân viên '{request.FullName}' thành công. Vui lòng phân công tại mục Phân Công." };
        }
    }
}
