using System.Text;
using System.Text.Json;
using ImageGallery.Client.ViewModels;
using ImageGallery.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace ImageGallery.Client.Controllers
{
    // Tutte le azioni richiedono un utente autenticato tramite cookie/OIDC.
    [Authorize]
    public class GalleryController : Controller
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<GalleryController> _logger;

        public GalleryController(
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment environment,
            ILogger<GalleryController> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory)); ;
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
        }

        public async Task<IActionResult> Index()
        {
            // Recupera e scrive nei log token/claim dell'utente autenticato.
            await LoginIdentityInformation();

            var httpClient = _httpClientFactory.CreateClient("APIClient");
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/images/");

            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            using (var responseStream = await response.Content.ReadAsStreamAsync())
            {
                // Deserializza la risposta JSON della API e la passa alla view MVC.
                var images = await JsonSerializer.DeserializeAsync<List<Image>>(responseStream, JsonSerializerOptions);
                return View(new GalleryIndexViewModel(images ?? new List<Image>()));
            }
        }

        public async Task<IActionResult> EditImage(Guid id)
        {
            var httpClient = _httpClientFactory.CreateClient("APIClient");
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/images/{id}");

            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            using (var responseStream = await response.Content.ReadAsStreamAsync())
            {
                var deserializedImage = await JsonSerializer.DeserializeAsync<Image>(responseStream, JsonSerializerOptions);

                if (deserializedImage == null)
                {
                    throw new Exception("Deserialized image must not be null");
                }

                // La view usa un ViewModel dedicato, separato dal DTO restituito dalla API.
                var editImageViewModel = new EditImageViewModel()
                {
                    Id = deserializedImage.Id,
                    Title = deserializedImage.Title,
                };

                return View(editImageViewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditImage(EditImageViewModel editImageViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Costruisce il DTO da inviare alla API per l'aggiornamento.
            var imageForUpdate = new ImageForUpdate(editImageViewModel.Title);

            // Serializza il DTO in JSON per la richiesta HTTP PUT.
            var serializeImageForUpdate = JsonSerializer.Serialize(imageForUpdate);

            var httpClient = _httpClientFactory.CreateClient("APIClient");
            var request = new HttpRequestMessage(HttpMethod.Put, $"/api/images/{editImageViewModel.Id}")
            {
                Content = new StringContent(
                    serializeImageForUpdate,
                    System.Text.Encoding.Unicode,
                    "application/json")
            };

            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            return RedirectToAction("Index");
        }

        // [Authorize(Roles = "PayingUser")]
        [Authorize(Policy = "UserCanAddImage")]
        public IActionResult AddImage()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //  [Authorize(Roles = "PayingUser")]
        [Authorize(Policy = "UserCanAddImage")]
        public async Task<IActionResult> AddImage(AddImageViewModel addImageViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Il DTO viene costruito a partire dai dati del form MVC.
            ImageForCreation? imageForCreation = null;

            // Il form prevede un solo file immagine; viene usato il primo elemento caricato.
            var imageFile = addImageViewModel.Files.First();

            if (imageFile.Length > 0)
            {
                using (var fileStream = imageFile.OpenReadStream())
                using (var ms = new MemoryStream())
                {
                    // Il file viene letto in memoria e inviato alla API come array di byte.
                    fileStream.CopyTo(ms);
                    var title = ResolveImageTitle(addImageViewModel.Title);
                    imageForCreation = new ImageForCreation(
                        title, ms.ToArray());
                }
            }

            // Serializza il payload da inviare alla API protetta.
            var serializedImageForCreation = JsonSerializer.Serialize(imageForCreation);

            var httpClient = _httpClientFactory.CreateClient("APIClient");

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/images")
            {
                Content = new StringContent(
                    serializedImageForCreation,
                    System.Text.Encoding.Unicode,
                    "application/json")
            };

            var response = await httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return RedirectToAction("Index");
        }

        private string ResolveImageTitle(string? title)
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                return title.Trim();
            }

            var givenName = User.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value;
            var displayName = string.IsNullOrWhiteSpace(givenName) ? "the user" : givenName;

            return $"An image by {displayName}";
        }


        // per ottenere un token salvato
        public async Task LoginIdentityInformation()
        {
            // Recupera l'id_token salvato nella sessione di autenticazione locale.
            var identityToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.IdToken);

            // Get the saved access token
            var accessToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

            // get the refresh token
            var refreshToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);



            var userClaimsStringBuilder = new StringBuilder();
            foreach (var claim in User.Claims)
            {
                userClaimsStringBuilder.AppendLine($"Claim type: {claim.Type} - Claim value: {claim.Value}");
            }

            if (_environment.IsDevelopment())
            {
                // In Development mostriamo i token completi per confronto con il corso e debug locale.
                _logger.LogInformation(
                    $"Identity token:{Environment.NewLine}{identityToken}{Environment.NewLine}{Environment.NewLine}" +
                    $"Access token:{Environment.NewLine}{accessToken}{Environment.NewLine}{Environment.NewLine}" +
                    $"Refresh token:{Environment.NewLine}{refreshToken}{Environment.NewLine}{Environment.NewLine}" +
                    $"{userClaimsStringBuilder}");
            }
            else
            {
                // Fuori da Development evitiamo di scrivere token sensibili nei log.
                _logger.LogInformation($"User claims:{Environment.NewLine}{userClaimsStringBuilder}");
            }

        }

    }
}

