using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.UserDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Users.Queries
{
    public class GetMyProfileQuery : IRequest<Response<UserProfileDto>>
    {
        public Guid UserId { get; set; }
        public GetMyProfileQuery(Guid userId) => UserId = userId;
    }

    public class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, Response<UserProfileDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetMyProfileQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Response<UserProfileDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.OwnedBusinesses) // Nạp thông tin doanh nghiệp sở hữu nếu có
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null) throw new ApiException("Không tìm thấy thông tin tài khoản.");

            var profileDto = new UserProfileDto
            {
                Id = user.Id.ToString(),
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                Role = user.Role.Name
            };

            // Nếu là Partner, lấy thông tin doanh nghiệp đầu tiên mà họ sở hữu
            if (user.Role.Name == "partner" && user.OwnedBusinesses.Any())
            {
                var biz = user.OwnedBusinesses.First();
                profileDto.BusinessInfo = new BusinessProfileDto
                {
                    BusinessId = biz.Id.ToString(),
                    BusinessName = biz.BusinessName,
                    TaxCode = biz.TaxCode,
                    BusinessAddress = biz.BusinessAddress,
                    RepresentativeName = biz.RepresentativeName,
                    Position = biz.Position,
                    VerificationStatus = biz.VerificationStatus.ToString()
                };
            }

            return new Response<UserProfileDto>(profileDto, "Lấy thông tin hồ sơ thành công.");
        }
    }
}
