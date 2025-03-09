using AutoMapper;
using WebServiceCosmetics.Models;
using WebServiceCosmetics.Models.DTO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebServiceCosmetics.Mappings
{
    public class RawMaterialProfile : Profile
    {
        public RawMaterialProfile()
        {
            // Маппинг из RawMaterialModel в RawMaterialDTO
            CreateMap<RawMaterialModel, RawMaterialModelDto>()
                .ForMember(dest => dest.UnitName, opt => opt.MapFrom(src => src.Unit.Name)); // Маппим имя юнита

            // Маппинг из RawMaterialDTO в RawMaterialModel
            CreateMap<RawMaterialModelDto, RawMaterialModel>()
                .ForMember(dest => dest.Unit, opt => opt.Ignore()); // Игнорируем Unit, так как это будет нужно установить отдельно
        }
    }
}
