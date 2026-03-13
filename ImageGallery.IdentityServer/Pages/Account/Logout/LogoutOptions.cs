namespace ImageGallery.IdentityServer.Pages.Logout;

// Questa classe centralizza alcune opzioni statiche del flusso di logout
// usato dalle Razor Pages dell'IdentityServer.
public static class LogoutOptions
{
    // Se true, prima di eseguire il logout viene mostrata una pagina di conferma
    // all'utente. Serve a evitare sign-out accidentali avviati da link o redirect.
    public static readonly bool ShowLogoutPrompt = true;

    // Se true, dopo il logout completato l'utente viene reindirizzato automaticamente
    // al PostLogoutRedirectUri del client, se presente nel contesto di logout.
    public static readonly bool AutomaticRedirectAfterSignOut = true;
}
