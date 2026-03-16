# Workflow Step/Item

## 1. Prerequisito

Prima di qualsiasi analisi, piano, uso tool o modifica:
- leggere il file `AGENTS.md` del repository corrente;
- non eseguire attività operative finché tale lettura non è completata.

Se `AGENTS.md` non è stato letto nella richiesta corrente:
- non proporre checklist;
- non usare tool;
- non eseguire attività operative.

## 2. Checklist Degli Step

Per ogni nuovo piano:
- presentare una checklist concettuale di 1-7 step;
- usare `🟦` per step aperti;
- usare `🟧 ~~testo~~` per step completati;
- usare sempre il formato `Step <numero>: <descrizione>`.

Regole:
- mantenere visibili step aperti e completati;
- presentare la checklist una sola volta per turno o per stage decisionale, salvo cambiamenti reali del piano;
- non ripubblicare la stessa checklist identica se non cambiano stato, perimetro o step;
- nei messaggi successivi aggiornare solo avanzamento, validazione o richiesta decisionale;
- dopo la checklist degli step, mostrare subito e solo la scelta `1/2`;
- non mostrare ancora la checklist degli item dello step.

## 3. Scelta A Livello Step

Mostrare sempre:
- `🟡 1. Vuoi eseguire solo lo STEP <numero reale dello step proposto>?`
- `🟡 2. Vuoi eseguire tutti gli STEP rimanenti del piano corrente assieme?`

Regole:
- input valido solo `1` o `2`;
- se input non valido, non proseguire e ripresentare esclusivamente la richiesta di scelta;
- prima della risposta utente, tutti gli step restano `🟦`;
- non marcare step come completati prima della scelta esplicita;
- non inferire mai `1` o `2` da frasi generiche.

Comportamento:
- se scelta `1`, eseguire solo lo step indicato;
- se scelta `2`, eseguire tutti gli step rimanenti del piano corrente;
- se scelta `2`, eseguire assieme anche tutti gli item aperti di ciascuno step rimanente, senza ulteriori conferme intermedie.

## 4. Checklist Degli Item

Mostrare la checklist degli item solo dopo una scelta valida `1` a livello step per quello specifico step.

Regole:
- preferire 3-7 item concreti per step, salvo task molto piccoli;
- usare `🟦` per item aperti;
- usare `🟧 ~~testo~~` per item completati;
- usare sempre il formato `<Numero Step>.<Numero progressivo item> : <descrizione>`;
- mantenere visibili item aperti e completati.

Se l'utente ha già scelto `1` a livello step:
- non ristampare la checklist completa degli step;
- mostrare solo gli item dello step corrente e la relativa scelta `1/2`.

## 5. Scelta A Livello Item

Mostrare sempre:
- `🟡 1. Vuoi eseguire un item alla volta dello STEP <n>?`
- `🟡 2. Vuoi eseguire tutti gli item dello STEP <n> assieme?`

Regole:
- input valido solo `1` o `2`;
- se input non valido, non proseguire e ripresentare esclusivamente la richiesta di scelta;
- prima della risposta utente, tutti gli item restano `🟦`;
- non marcare item come completati prima della scelta esplicita;
- non inferire mai `1` o `2` da frasi generiche.

Comportamento:
- se scelta `1`, eseguire solo il primo item aperto dello step e poi riproporre `1/2` sugli item rimanenti dello stesso step;
- se scelta `2`, eseguire tutti gli item aperti dello step;
- uno step è completato solo quando tutti i suoi item sono completati.

## 6. Regole Di Esecuzione

- nessuna esecuzione preventiva prima di un input valido `1` o `2`;
- prima di ogni step e prima della prosecuzione item-by-item, attendere sempre una risposta esplicita `1` o `2`;
- dopo ogni item o uso di tool, validare l'esito in 1-2 frasi e correggere se serve;
- testare e verificare il codice modificato;
- riformattare i file toccati;
- se compare `Accesso negato`, usare permessi elevati se disponibili e consentiti.
