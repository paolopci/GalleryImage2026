using AutoMapper;
using ImageEntity = ImageGallery.API.Entities.Image;

namespace ImageGallery.API.Profiles;

public class ImageProfile : Profile
{
    public ImageProfile()
    {
        CreateMap<ImageEntity, ImageGallery.Model.Image>();
        CreateMap<ImageGallery.Model.ImageForCreation, ImageEntity>()
            .ForMember(destination => destination.FileName, options => options.Ignore())
            .ForMember(destination => destination.OwnerId, options => options.Ignore());
        CreateMap<ImageGallery.Model.ImageForUpdate, ImageEntity>()
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.FileName, options => options.Ignore())
            .ForMember(destination => destination.OwnerId, options => options.Ignore());
    }
}
