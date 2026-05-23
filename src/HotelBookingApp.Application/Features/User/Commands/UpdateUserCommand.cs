using AutoMapper;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.UserDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;

namespace HotelBookingApp.Application.Features.Users.Commands
{
    public class UpdateUserCommand : IRequest<Response<UserDto>>
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Role { get; set; }
        public bool IsActive { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Response<UserDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public UpdateUserCommandHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context; _mapper = mapper;
        }

        public async Task<Response<UserDto>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FindAsync(new object[] { request.Id }, cancellationToken);
            if (user == null) return new Response<UserDto>("Không tìm thấy người dùng với mã " + request.Id);

            _mapper.Map(request, user);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return new Response<UserDto>(_mapper.Map<UserDto>(user), "Cập nhật thông tin người dùng thành công!");
        }
    }
}