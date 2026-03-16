using AutoMapper;
using ImageGallery.API.Services;
using ImageGallery.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageGallery.API.Controllers;

[Authorize(Policy = "ImageGalleryApiFullAccess")]
//[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly IGalleryRepository _galleryRepository;
    private readonly IWebHostEnvironment _hostingEnvironment;
    private readonly IMapper _mapper;

    public ImagesController(
        IGalleryRepository galleryRepository,
        IWebHostEnvironment hostingEnvironment,
        IMapper mapper)
    {
        _galleryRepository = galleryRepository ??
            throw new ArgumentNullException(nameof(galleryRepository));
        _hostingEnvironment = hostingEnvironment ??
            throw new ArgumentNullException(nameof(hostingEnvironment));
        _mapper = mapper ??
            throw new ArgumentNullException(nameof(mapper));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Image>>> GetImages()
    {
        var imagesFromRepo = await _galleryRepository.GetImagesAsync();
        var imagesToReturn = _mapper.Map<IEnumerable<Image>>(imagesFromRepo);

        return Ok(imagesToReturn);
    }

    [HttpGet("{id}", Name = "GetImage")]
    public async Task<ActionResult<Image>> GetImage(Guid id)
    {
        var imageFromRepo = await _galleryRepository.GetImageAsync(id);

        if (imageFromRepo == null)
        {
            return NotFound();
        }

        var imageToReturn = _mapper.Map<Image>(imageFromRepo);

        return Ok(imageToReturn);
    }

    [HttpPost]
    public async Task<ActionResult<Image>> CreateImage([FromBody] ImageForCreation imageForCreation)
    {
        var imageEntity = _mapper.Map<API.Entities.Image>(imageForCreation);
        var webRootPath = _hostingEnvironment.WebRootPath;
        var fileName = $"{Guid.NewGuid()}.jpg";
        var filePath = Path.Combine(webRootPath, "images", fileName);

        await System.IO.File.WriteAllBytesAsync(filePath, imageForCreation.Bytes);

        imageEntity.FileName = fileName;

        _galleryRepository.AddImage(imageEntity);
        await _galleryRepository.SaveChangesAsync();

        var imageToReturn = _mapper.Map<Image>(imageEntity);

        return CreatedAtRoute(
            "GetImage",
            new { id = imageToReturn.Id },
            imageToReturn);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteImage(Guid id)
    {
        var imageFromRepo = await _galleryRepository.GetImageAsync(id);

        if (imageFromRepo == null)
        {
            return NotFound();
        }

        _galleryRepository.DeleteImage(imageFromRepo);
        await _galleryRepository.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateImage(Guid id, [FromBody] ImageForUpdate imageForUpdate)
    {
        var imageFromRepo = await _galleryRepository.GetImageAsync(id);

        if (imageFromRepo == null)
        {
            return NotFound();
        }

        _mapper.Map(imageForUpdate, imageFromRepo);

        _galleryRepository.UpdateImage(imageFromRepo);
        await _galleryRepository.SaveChangesAsync();

        return NoContent();
    }
}
