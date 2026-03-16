# Security Baseline

## 1. Gestione Segreti

- non committare segreti in file di configurazione versionati;
- evitare credenziali in chiaro in file come `appsettings*.json`, `launchSettings.json` o equivalenti;
- preferire variabili d'ambiente, user secrets o strumenti dedicati alla gestione sicura dei segreti.

## 2. Configurazione Sicura

- separare configurazioni locali, di sviluppo e di produzione;
- non assumere che valori sensibili possano restare inline nel repository;
- documentare i punti di configurazione sensibili senza esporre i valori reali.

## 3. Validazione Input

- validare gli input in ingresso lato API o lato boundary applicativo appropriato;
- gestire esplicitamente input mancanti, non validi o fuori formato;
- evitare di propagare dati non validati lungo il flusso applicativo.

## 4. Risposte E Error Handling

- restituire codici di stato coerenti con il risultato dell'operazione;
- distinguere chiaramente tra errori di validazione, risorse non trovate, conflitti e errori interni;
- non esporre dettagli sensibili o stack trace nei messaggi destinati all'utente finale.

## 5. Adattamento Al Progetto

Questa baseline è intenzionalmente generica:
- integrare eventuali regole specifiche del framework o della piattaforma nel file locale del repository;
- mantenere qui solo principi riusabili tra progetti diversi.
