# Model

## Scopo

`ImageGallery.Model` contiene i modelli condivisi tra i diversi progetti della soluzione.

## Contenuto

- modelli di dominio condivisi;
- modelli per creazione e aggiornamento;
- contratti dati riusati tra API e client.

## Vantaggi

- evita duplicazione tra API e client;
- centralizza i contratti di input/output;
- semplifica il mapping tra livelli applicativi.

## Uso nella soluzione

Il progetto e referenziato sia dall'API sia dal client MVC per mantenere allineati i tipi usati nello scambio dati.
