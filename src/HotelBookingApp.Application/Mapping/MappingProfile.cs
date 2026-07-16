using AutoMapper;
using HotelBookingApp.Application.DTOs.ProvinceDto;
using HotelBookingApp.Application.DTOs.UserDto;
using HotelBookingApp.Application.DTOs.WardDto;
using HotelBookingApp.Domain.Models;

namespace HotelBookingApp.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Map 2 chiều giữa Entity và DTO
            CreateMap<Province, ProvinceDto>().ReverseMap();

            // Map từ Command sang Entity để Insert/Update nhanh hơn
            CreateMap<Features.Provinces.Commands.CreateProvinceCommand, Province>();
            CreateMap<Features.Provinces.Commands.UpdateProvinceCommand, Province>();

            // Sửa đoạn code cũ thành đoạn mã an toàn này:
            CreateMap<Ward, WardDto>()
                .ForMember(dest => dest.ProvinceName, opt => opt.MapFrom(src =>
                    src.Province != null ? src.Province.Name : string.Empty));
            CreateMap<Features.Wards.Commands.CreateWardCommand, Ward>();
            CreateMap<Features.Wards.Commands.UpdateWardCommand, Ward>();

            // --- USER MAPPINGS ---
            CreateMap<User, UserDto>();
            CreateMap<Features.Users.Commands.CreateUserCommand, User>();
            CreateMap<Features.Users.Commands.UpdateUserCommand, User>();
        }
    }
}
