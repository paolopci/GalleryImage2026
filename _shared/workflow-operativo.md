# Workflow Operativo

## Principio iniziale

Leggi sempre `AGENTS.md` come prima azione di ogni nuova richiesta sul progetto, prima di analisi, piano, uso tool o modifiche.

Se `AGENTS.md` non è stato letto nella richiesta corrente:
- non proporre checklist;
- non usare tool;
- non eseguire attività operative.

## Pianificazione a step

Dopo la lettura iniziale di `AGENTS.md`:
- analizza il task;
- identifica il perimetro della modifica;
- presenta una checklist concettuale di 1-7 step.

Per ogni step:
- usa `🟦` per gli step aperti;
- usa `🟧 ~~testo~~` per gli step completati;
- usa sempre il formato `Step <numero>: <descrizione>`.

Regole:
- mantieni visibili step aperti e completati;
- ripubblica la checklist solo se cambiano stato, perimetro o contenuto del piano;
- nei messaggi successivi aggiorna solo avanzamento, validazione o richiesta decisionale;
- dopo la checklist mostra subito e solo la scelta `1/2` a livello step.

## Scelta a livello step

Mostra sempre:
- `🟡 1. Vuoi eseguire solo lo STEP <numero reale dello step proposto>?`
- `🟡 2. Vuoi eseguire tutti gli STEP rimanenti del piano corrente assieme?`

Regole:
- input valido solo `1` o `2`;
- non inferire mai la scelta da frasi generiche;
- prima della risposta utente tutti gli step restano aperti;
- non marcare step come completati prima della scelta esplicita.

Comportamento:
- se la scelta è `1`, esegui solo lo step indicato;
- se la scelta è `2`, esegui tutti gli step rimanenti del piano corrente;
- se l'input non è valido, non proseguire e ripresenta solo la richiesta di scelta.

## Checklist degli item

Mostra la checklist degli item solo dopo una scelta valida `1` a livello step.

Per ogni item:
- usa `🟦` per gli item aperti;
- usa `🟧 ~~testo~~` per gli item completati;
- usa sempre il formato `<Numero Step>.<Numero progressivo item> : <descrizione>`.

Regole:
- preferisci 3-7 item concreti per step, salvo task molto piccoli;
- mantieni visibili item aperti e completati;
- non mostrare gli item prima della scelta esplicita sullo step.

## Scelta a livello item

Mostra sempre:
- `🟡 1. Vuoi eseguire un item alla volta dello STEP <n>?`
- `🟡 2. Vuoi eseguire tutti gli item dello STEP <n> assieme?`

Regole:
- input valido solo `1` o `2`;
- non inferire mai la scelta da frasi generiche;
- prima della risposta utente tutti gli item restano aperti;
- non marcare item come completati prima della scelta esplicita.

Comportamento:
- se la scelta è `1`, esegui solo il primo item aperto e poi riproponi la scelta sugli item rimanenti;
- se la scelta è `2`, esegui tutti gli item aperti dello step;
- se a livello step è stata scelta l'opzione `2`, esegui direttamente tutti gli item aperti degli step inclusi senza ulteriori conferme;
- se l'input non è valido, non proseguire e ripresenta solo la richiesta di scelta.

Uno step è completato solo quando tutti i suoi item sono completati.

## Esecuzione e validazione

- Non eseguire attività operative prima di una scelta valida `1` o `2`.
- Dopo ogni uso di tool o modifica, valida l'esito in 1-2 frasi e correggi se serve.
- Testa e verifica il codice modificato.
- Riformatta i file toccati.
- Se compare `Accesso negato`, usa permessi elevati se disponibili e consentiti.
- Mantieni in italiano il contenuto del piano e dei deliverable.

## Skill

La lettura o valutazione teorica di una skill non equivale a esecuzione operativa.

Per usare operativamente una skill:
- richiedi sempre una conferma preventiva esplicita in chat;
- usa solo skill autorizzate e disponibili nella sessione corrente;
- non riutilizzare le scelte `1/2` del workflow come autorizzazione per una skill;
- attendi una risposta chiara e inequivocabile prima di procedere;
- dopo la conferma, valida in 1-2 righe che l'autorizzazione è stata ricevuta correttamente.
