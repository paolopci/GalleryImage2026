# Workflow Skill Authorization

## 1. Regola Generale

Prima di usare operativamente una o più skill:
- richiedere sempre conferma preventiva esplicita in chat;
- attendere una risposta chiara e inequivocabile prima di procedere.

## 2. Distinzione Tra Lettura ed Esecuzione

Non costituiscono esecuzione operativa:
- valutazione teorica di una skill;
- lettura del file descrittivo della skill;
- verifica della sua pertinenza rispetto al task.

Costituiscono esecuzione operativa:
- avvio di una skill;
- uso di una skill per modificare dati o file;
- uso di una skill che innesca operazioni irreversibili o esterne.

## 3. Ambito Delle Skill Consentite

Gestire ed eseguire solo:
- le skill elencate in `allowed_tools`;
- oppure l'elenco equivalente delle skill abilitate e disponibili nella sessione corrente.

Se una skill non è autorizzata o non è disponibile:
- esplicitarlo;
- non procedere con la sua esecuzione.

## 4. Regole Di Conferma

Per ogni chiamata a una skill che può modificare dati o innescare operazioni irreversibili:
- richiedere una conferma esplicita dedicata;
- attendere una risposta valida e riferita chiaramente all'autorizzazione richiesta;
- non riutilizzare conferme generiche per autorizzare azioni diverse.

Le regole di input `1` o `2` del workflow step/item:
- non si applicano automaticamente alle conferme skill;
- restano limitate alle scelte di avanzamento del piano.

## 5. Dopo La Conferma

Dopo ogni conferma valida:
- validare in 1-2 righe che la skill è stata autorizzata correttamente;
- solo dopo procedere con l'esecuzione.

Se la conferma non è valida o resta ambigua:
- non avviare la skill;
- chiedere chiarimento.
