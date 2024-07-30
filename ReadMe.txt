Konzolová aplikace pro hromadné stahování jízdních řádů 
************************************************************************************************
Vyžaduje .NET 8, teoreticky OS Windows 7 a vyšší, prakticky zkoušeno jen pod OS Windows 10.

Poslední aktualizace instalačního souboru: 30-07-2024


Poznámky pro instalaci:

Pokud se k vám náhodou do počítače ještě nenastěhoval .NET 8 (např. společně s nějakým update OS Windows), 
je třeba jej instalovat (64 bit), např. odtud: https://dotnet.microsoft.com/en-us/download/dotnet/8.0

Aplikace není podepsána žádným "codesign certificate" - proklikat se přes Windows Defender nebo dát výjimku antiviru je 
pro mne daleko menší buzerace, než nějaký certifikát získat. Pokud potřebujete předem vidět zdrojový kód, viz odkaz níže.


Poznámky pro uživatele:

JŘ ODIS dopravců MDPO a DPO jsou stahovány přímo z jejich webových stránek a jsou uloženy v podobě, v jaké jsou staženy, bez roztřídění. 

Proto doporučuji přednostně stahovat jízdní řády ze stránek KODISu. Trvá to sice déle, 
neb se musí nejdříve stáhnout JSON soubory s odkazy na JŘ a teprvé poté se stahují 
dané JŘ, nicméně JŘ jsou poté programem roztříděny. Rovněž doporučuji stáhnout všechny JŘ najednou.

Doporučuji sledovat změny linek na www.kodis.cz a poté stahovat změny JŘ (nebo lépe celý komplet najednou). 

Dlouhodobě platné JŘ je třeba chápat jen jako orientační - bez spolupráce KODISu nemohu vědět, zdali po výluce budou 
platit dlouhodobě platné JŘ v daném adresáři, či budou po ukončení platnosti výlukových JŘ vytvořeny 
nějaké zcela jiné dlouhodobě platné JŘ.

KODIS může kdykoliv změnit strukturu svých souborů na webu či může mít v nich chyby (stává se) či může 
opomenout včas zahrnout (či vůbec vložit) odkaz na JŘ do JSON souboru, případně přidá další JSON soubor
odkud čerpám informace pro stažení - pak chvíli trvá, než si toho všimnu. JSON soubory jsou veřejně dostupné.
Z těchto důvodů raději častěji stahujte instalační soubor a kontrolujte datum poslední aktualizace instalačního souboru.

Pokud naleznete problém, ocením, když mi pošlete informaci na emailovou adresu miroslav.hustak@atlas.cz .

"Mobilní" verze toho programu bude k dispozici, až najdu někoho, kdo pomůže s UX/UE. Není to někdo, koho znáte? Uvítám doporučení. 

Zdrojový kód:
https://github.com/MiroslavHustak/OdisTimetableDownloader



























