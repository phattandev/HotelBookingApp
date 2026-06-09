using AutoMapper;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.WardDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Wards.Commands
{
    public class DeleteWardCommand : IRequest<Response<WardDto>>
    {
        public Guid Id { get; set; }
    }

    public class DeleteWardCommandHandler : IRequestHandler<DeleteWardCommand, Response<WardDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public DeleteWardCommandHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context; _mapper = mapper;
        }

        public async Task<Response<WardDto>> Handle(DeleteWardCommand request, CancellationToken cancellationToken)
        {
            var ward = await _context.Wards.Include(w => w.Province).FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);
            if (ward == null) return new Response<WardDto>("Không tìm thấy phường/xã với mã " + request.Id);

            var dto = _mapper.Map<WardDto>(ward);
            _context.Wards.Remove(ward);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<WardDto>(dto, "Xoá thành công phường/xã:" + dto.Name);
        }
    }
}