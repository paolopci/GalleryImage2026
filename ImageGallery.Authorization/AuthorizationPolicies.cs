using Microsoft.AspNetCore.Authorization;

namespace ImageGallery.Authorization
{
    public static class AuthorizationPolicies
    {
        public static AuthorizationPolicy CanAddImage()
        {
            // creo una policy per permettere a un utente authenticato con role = "PayingUser" e paese =="be" (Belgio)
            // di poter aggiungere una nuova immagine.
            return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim("paese", "be")
            .RequireRole("PayingUser")
            .Build();
        }

    }
}
