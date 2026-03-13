using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageGallery.Client.Controllers
{
    public class AuthenticationController : Controller
    {
        // Endpoint opzionale di appoggio per viste o redirect legati all'autenticazione.
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login(string? returnUrl = null)
        {
            // Se il chiamante non specifica una destinazione finale, dopo il login
            // l'utente viene riportato alla galleria protetta.
            var redirectUrl = string.IsNullOrWhiteSpace(returnUrl)
                ? Url.Action("Index", "Gallery")
                : returnUrl;

            // Challenge delega l'autenticazione allo schema OpenID Connect.
            // In pratica il client MVC non autentica direttamente l'utente:
            // reindirizza invece il browser verso l'IDP configurato in Program.cs.
            // Sarà l'IDP a mostrare la login page, validare le credenziali e,
            // al termine, rimandare l'utente a questa applicazione con il risultato del login.
            return Challenge(
                new AuthenticationProperties { RedirectUri = redirectUrl },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        [Authorize]
        public IActionResult Logout()
        {
            // Dopo il logout completo, l'utente torna alla home pubblica del client.
            var callbackUrl = Url.Action("Index", "Home");

            // SignOut esegue due operazioni coordinate:
            // 1. chiude il cookie locale del client MVC, cioè la sessione applicativa;
            // 2. avvia il sign-out verso l'IDP OpenID Connect, così da terminare
            //    anche la sessione centralizzata gestita dal provider di identità.
            // Questo evita che l'utente risulti disconnesso solo localmente ma ancora
            // autenticato presso l'IDP.
            return SignOut(
                new AuthenticationProperties { RedirectUri = callbackUrl },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
}
