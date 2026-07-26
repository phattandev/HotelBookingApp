using AutoMapper;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.ProvinceDto;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using FluentValidation;

namespace HotelBookingApp.Application.Features.Provinces.Commands
{
    public class CreateProvinceCommand : IRequest<Response<ProvinceDto>>
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }

    public class CreateProvinceCommandHandler : IRequestHandler<CreateProvinceCommand, Response<ProvinceDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CreateProvinceCommandHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context; _mapper = mapper;
        }

        public async Task<Response<ProvinceDto>> Handle(CreateProvinceCommand request, CancellationToken cancellationToken)
        {
            var province = _mapper.Map<Province>(request);
            _context.Provinces.Add(province);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<ProvinceDto>(_mapper.Map<ProvinceDto>(province), "Thêm tỉnh/thành phố thành công!");
        }
    }

    public class CreateProvinceCommandValidator : FluentValidation.AbstractValidator<CreateProvinceCommand>
    {
        public CreateProvinceCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Tên tỉnh/thành phố không được để trống.");
            RuleFor(x => x.Code).NotEmpty().WithMessage("Mã không được để trống.")
                .Matches(@"^[a-zA-Z0-9]+$").WithMessage("Mã không được chứa ký tự đặc biệt.");
            RuleFor(x => x.Slug).NotEmpty().WithMessage("Slug không được để trống.");
            RuleFor(x => x.Type).NotEmpty().WithMessage("Phân loại không được để trống.");
        }
    }
}