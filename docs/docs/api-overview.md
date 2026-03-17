# API

## Scopo

`ImageGallery.API` espone gli endpoint HTTP usati dal client MVC per la gestione della galleria immagini.

## Componenti principali

- `Controllers`: endpoint REST esposti dall'applicazione.
- `DbContext`: accesso ai dati tramite Entity Framework Core.
- `Entities`: entita persistite nel database.
- `Services`: logica di accesso ai dati e operazioni applicative.
- `Profiles`: mapping AutoMapper tra entita e DTO.

## Flusso applicativo

1. Il client invia richieste HTTP all'API.
2. I controller validano l'input e delegano ai servizi.
3. I servizi usano `GalleryDbContext` per leggere o salvare i dati.
4. I risultati vengono restituiti come modelli condivisi o DTO.

## Note di documentazione

La reference tecnica completa dei namespace e dei tipi pubblici e disponibile nella sezione `API` generata automaticamente da DocFX.
