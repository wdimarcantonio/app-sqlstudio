# ISTRUZIONI

** OBIETTIVO **

Lo scopo di questo progetto è quello di creare un'applicazione che permetta di importare un file excel e fare delle query utilizzando il linguaggio SQL.
Una volta scritta la query è possibile eseguirla per vedere in griglia il risultato.
Il risultato dell'elaborazione poi può essere esportato in un file excel oppure importato in un database MicrosoftSQL. 
Oltre alla modalità di scrittura della query SQL dovrebbe essere possibile utilizzare uno strumento per costruire la query in maniera semplificata, quindi selezaionare le colonne dell'excel, attribuirgli degli alias e per ogni colonna applicare delle trasformazioni.

Dovrebbe essere possibile gestire più origini dati e connessioni: Es. Più fogli excel, più database SQL Server, più file csv. 

Ogni Origine dati può essere usata nella query utilizzando un alias.

la query tra le varie connessioni può essere fatta utilizzando il JOIN.


** TECNICHE **

L'applicazione deve essere sviluppata utilizzando le tecnologie seguenti:
- .NET 8
- WPF
- Entity Framework
- SQL Server
- Excel

**INTERFACCIA**

L'interfaccia deve essere composta da tre sezioni:
- Importazione file excel
- Costruzione query
- Esecuzione query

** Importazione file excel **

Nella sezione di importazione file excel è possibile caricare un file excel e visualizzare le prime 10 righe.

** Costruzione query **

Nella sezione di costruzione query è possibile selezionare le colonne dell'excel, attribuirgli degli alias e per ogni colonna applicare delle trasformazioni.

** Esecuzione query **

Nella sezione di esecuzione query è possibile eseguire la query e visualizzare il risultato in griglia.

L'interfaccia deve essere sviluppata con uno stile moderno e con un design responsive.
L'interfaccia deve essere sviluppata utilizzando l'agente Gemini PRO.