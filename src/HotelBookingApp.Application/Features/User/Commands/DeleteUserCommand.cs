using AutoMapper;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.UserDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;

namespace HotelBookingApp.Application.Features.Users.Commands
{
    public class DeleteUserCommand : IRequest<Response<UserDto>>
    {
        public int Id { get; set; }
    }

    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Response<UserDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public DeleteUserCommandHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context; _mapper = mapper;
        }

        public async Task<Response<UserDto>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FindAsync(new object[] { request.Id }, cancellationToken);
            if (user == null) return new Response<UserDto>("Không tìm thấy người dùng với mã " + request.Id);

            var dto = _mapper.Map<UserDto>(user);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<UserDto>(dto, "Đã xoá người dùng " + dto.Name);
        }
    }
}