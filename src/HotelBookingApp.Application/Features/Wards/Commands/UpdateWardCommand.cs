using AutoMapper;
using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.WardDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Wards.Commands
{
    public class UpdateWardCommand : IRequest<Response<WardDto>>
    {
        public Guid Id { get; set; }
        public Guid ProvinceId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool IsActive { get; set; }
    }

    public class UpdateWardCommandHandler : IRequestHandler<UpdateWardCommand, Response<WardDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public UpdateWardCommandHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context; _mapper = mapper;
        }

        public async Task<Response<WardDto>> Handle(UpdateWardCommand request, CancellationToken cancellationToken)
        {
            var ward = await _context.Wards.Include(w => w.Province).FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);
            if (ward == null) return new Response<WardDto>("Không tìm thấy phường/xã với mã " + request.Id);

            // Kiểm tra trùng lặp mã hoặc tên phường/xã trong cùng một tỉnh/thành phố (loại trừ chính nó)
            var duplicate = await _context.Wards.AnyAsync(w => 
                w.ProvinceId == request.ProvinceId && 
                w.Id != request.Id &&
                (w.Code == request.Code || w.Name.ToLower() == request.Name.ToLower()), 
                cancellationToken);
                
            if (duplicate)
                throw new ApiException($"Mã hoặc tên Phường/Xã đã tồn tại trong Tỉnh/Thành phố này!");

            _mapper.Map(request, ward);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<WardDto>(_mapper.Map<WardDto>(ward), "Cập nhật phường/xã thành công");
        }
    }

    public class UpdateWardCommandValidator : AbstractValidator<UpdateWardCommand>
    {
        public UpdateWardCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Id không được để trống.");
            RuleFor(x => x.ProvinceId).NotEmpty().WithMessage("ID Tỉnh/Thành phố không được để trống.");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Tên Phường/Xã không được để trống.");
            RuleFor(x => x.Code).NotEmpty().WithMessage("Mã không được để trống.")
                .Matches(@"^[a-zA-Z0-9]+$").WithMessage("Mã không được chứa ký tự đặc biệt.");
            RuleFor(x => x.Slug).NotEmpty().WithMessage("Slug không được để trống.");
            RuleFor(x => x.Type).NotEmpty().WithMessage("Phân loại không được để trống.");
        }
    }
}