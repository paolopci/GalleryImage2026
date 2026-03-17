using AutoMapper;
using ImageGallery.API.Services;
using ImageGallery.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageGallery.API.Controllers;

// Tutte le azioni richiedono un access token con scope
// "imagegalleryapi.fullaccess", definito nella policy configurata nell'API.
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
        // Il controller espone DTO del progetto Model, non direttamente le entity EF.
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
        // Il nome file viene generato lato server per evitare collisioni e non fidarsi
        // di eventuali nomi provenienti dal client.
        var fileName = $"{Guid.NewGuid()}.jpg";
        var filePath = Path.Combine(webRootPath, "images", fileName);

        // I byte dell'immagine vengono salvati fisicamente in wwwroot/images.
        await System.IO.File.WriteAllBytesAsync(filePath, imageForCreation.Bytes);

        imageEntity.FileName = fileName;

        _galleryRepository.AddImage(imageEntity);
        await _galleryRepository.SaveChangesAsync();

        var imageToReturn = _mapper.Map<Image>(imageEntity);

        // Restituisce 201 con header Location che punta all'endpoint GetImage.
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

        // AutoMapper aggiorna l'entity esistente con i valori ricevuti dal client.
        _mapper.Map(imageForUpdate, imageFromRepo);

        _galleryRepository.UpdateImage(imageFromRepo);
        await _galleryRepository.SaveChangesAsync();

        return NoContent();
    }
}
