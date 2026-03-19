using System;
using Microsoft.AspNetCore.Authorization;

namespace ImageGallery.API.Authorization;

// Requirement marker usata per imporre che l'utente autenticato possieda l'immagine richiesta.
public class MustOwnImageRequirement : IAuthorizationRequirement
{
    public MustOwnImageRequirement()
    {
    }
}
