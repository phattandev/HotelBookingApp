using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using MediatR;

namespace HotelBookingApp.Application.Features.Amenities.Commands
{
    public class ToggleAmenityStatusCommand : IRequest<Response<string>>
    {
        public Guid Id { get; set; }
    }

    public class ToggleAmenityStatusCommandHandler
        : IRequestHandler<ToggleAmenityStatusCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        public ToggleAmenityStatusCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<string>> Handle(
            ToggleAmenityStatusCommand request, CancellationToken cancellationToken)
        {
            var amenity = await _context.Amenities.FindAsync(new object[] { request.Id }, cancellationToken)
                ?? throw new ApiException("Không tìm thấy tiện nghi!");

            amenity.IsActive = !(amenity.IsActive ?? true);
            await _context.SaveChangesAsync(cancellationToken);

            var status = amenity.IsActive == true ? "kích hoạt" : "vô hiệu hóa";
            return new Response<string>($"Đã {status} tiện nghi '{amenity.Name}'!");
        }
    }
}
