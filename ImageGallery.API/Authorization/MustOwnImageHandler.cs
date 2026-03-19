using ImageGallery.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ImageGallery.API.Authorization;

public class MustOwnImageHandler : AuthorizationHandler<MustOwnImageRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IGalleryRepository _galleryRepository;

    public MustOwnImageHandler(IGalleryRepository galleryRepository, IHttpContextAccessor httpContextAccessor)
    {
        _galleryRepository = galleryRepository ?? throw new ArgumentNullException(nameof(galleryRepository));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                         MustOwnImageRequirement requirement)
    {
        // Recupera l'identificativo della risorsa protetta dal contesto di autorizzazione
        // e, come fallback, dalla route corrente.
        var imageId = GetImageId(context);

        if (!Guid.TryParse(imageId, out Guid imageIdAsGuid))
        {
            context.Fail();
            return;
        }

        // Il claim "sub" rappresenta l'identificativo univoco dell'utente autenticato.
        var ownerId = context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        if (ownerId == null)
        {
            context.Fail();
            return;
        }

        // L'autorizzazione viene concessa solo se l'utente risulta proprietario dell'immagine.
        if (!await _galleryRepository.IsImageOwnerAsync(imageIdAsGuid, ownerId))
        {
            context.Fail();
            return;
        }

        context.Succeed(requirement);
    }

    private string? GetImageId(AuthorizationHandlerContext context)
    {
        if (context.Resource is AuthorizationFilterContext authorizationFilterContext)
        {
            return authorizationFilterContext.RouteData.Values["id"]?.ToString();
        }

        if (context.Resource is HttpContext httpContext)
        {
            return httpContext.GetRouteValue("id")?.ToString();
        }

        return _httpContextAccessor.HttpContext?.GetRouteValue("id")?.ToString();
    }
}
