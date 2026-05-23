using AutoMapper;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.UserDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Users.Queries
{
    public class GetUsersQuery : IRequest<Response<IEnumerable<UserDto>>> { }

    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Response<IEnumerable<UserDto>>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetUsersQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Response<IEnumerable<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _context.Users.ToListAsync(cancellationToken);
            var dto = _mapper.Map<IEnumerable<UserDto>>(users);
            return new Response<IEnumerable<UserDto>>(dto, "Lấy thông tin người dùng thành công!");
        }
    }
}