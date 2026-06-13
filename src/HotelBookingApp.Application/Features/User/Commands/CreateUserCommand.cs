using AutoMapper;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.UserDto;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;

namespace HotelBookingApp.Application.Features.Users.Commands
{
    public class CreateUserCommand : IRequest<Response<UserDto>>
    {
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; } = "customer";
        public bool IsActive { get; set; } = true;
        public string? AvatarUrl { get; set; }
    }

    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Response<UserDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CreateUserCommandHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context; _mapper = mapper;
        }

        public async Task<Response<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            bool emailExists = _context.Users.Any(u => u.Email == request.Email);
            if (emailExists) return new Response<UserDto>("Email đã tồn tại!");

            var user = _mapper.Map<User>(request);
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<UserDto>(_mapper.Map<UserDto>(user), "Tạo tài khoản thành công!");
        }
    }
}