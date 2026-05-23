using AutoMapper;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.UserDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;

namespace HotelBookingApp.Application.Features.Users.Queries
{
    public class GetUserByIdQuery : IRequest<Response<UserDto>>
    {
        public int Id { get; set; }
    }

    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Response<UserDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetUserByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context; _mapper = mapper;
        }

        public async Task<Response<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FindAsync(new object[] { request.Id }, cancellationToken);
            if (user == null) return new Response<UserDto>("Không tìm thấy người dùng với mã " + request.Id);
            return new Response<UserDto>(_mapper.Map<UserDto>(user), "Lấy thành công thông tin người dùng " + user.Name);
        }
    }
}