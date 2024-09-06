open System
open System.IO
open System.Data
open System.Windows
open FSharp.Control
open System.Windows.Forms

open BenchmarkDotNet.Running
open BenchmarkDotNet.Attributes

//**************************

open MyFsToolkit
open Puppeteer.Links
open BrowserDialogWindow

//**************************

open Helpers
open Helpers.CloseApp
open Helpers.ConsoleFixers

open Types
open Types.Types

open Logging.Logging

open Settings.Messages
open Settings.SettingsKODIS
open Settings.SettingsGeneral

open MainFunctions.WebScraping_DPO
open MainFunctions.WebScraping_MDPO
open MainFunctions.WebScraping_KODISFM
open MainFunctions.WebScraping_KODISFMRecord
open MainFunctions.WebScraping_KODISFMDataTable

type MyBenchmark() = 

    [<Benchmark>]
    member _.Test1 () = 
        let dt = DataTable.CreateDt.dt()                           
        let variant = CurrentValidity            
        SubmainFunctions.KODIS_SubmainDataTable.operationOnDataFromJson () dt variant String.Empty 

    [<Benchmark>]
    member _.Test2 () = 
        ()

    //dotnet run -c Release
    //let summary = BenchmarkRunner.Run<MyBenchmark>()

[<TailCall>] 
let rec private pathToFolder () =
    
    try        
        let (str, value) = openFolderBrowserDialog()    

        match value with
        | false     -> 
                     Ok str       
        | true 
            when 
                (<>) str String.Empty
                    -> 
                     Console.Clear()                                          
                     Ok String.Empty
        | _         -> 
                     Console.Clear()
                     printfn "\nNebyl vybrán adresář. Tak znovu... Anebo klikni na křížek pro ukončení aplikace. \n"                                                                     
                     Ok <| pathToFolder ()   
   
    with 
    | ex -> Error <| string ex.Message
                    
    |> function
        | Ok value  -> 
                     value  
        | Error err ->
                     logInfoMsg <| sprintf "Err043 %s" err
                     closeItBaby err
                     String.Empty

//[<EntryPoint>] 
[<EntryPoint; STAThread>] // STAThread> musi byt quli openFolderBrowserDialog()
let main argv =    

    (*
    <!-- Add this xml code into Directory.Build.props in the root (or create the file if missing) -->
    <Project>
        <PropertyGroup>
            <!-- <WarningsAsErrors>0025</WarningsAsErrors> -->
            <TreatWarningsAsErrors>true</TreatWarningsAsErrors>        
        </PropertyGroup>
    </Project>
    *)
    
    //*****************************Console******************************  
    let updateDate = "04-09-2024"

    try
        consoleAppProblemFixer() 
        consoleWindowSettings()  
        Ok ()             
    with
    | ex -> Error <| string ex.Message
                    
    |> function
        | Ok value  -> 
                     value  
        | Error err ->
                     logInfoMsg <| sprintf "Err045 %s" err
                     closeItBaby "Problém s textovým rozhraním." 
     
    //*****************************WebScraping******************************  
    
    let myWebscraping_DPO x =

        Console.Clear()
        printfn "Hromadné stahování aktuálních JŘ ODIS (včetně výluk) dopravce DP Ostrava z webu https://www.dpo.cz"
        printfn "Datum poslední aktualizace SW: %s" updateDate
        printfn "%s" <| String.replicate 70 "*"
        printfn "Nyní je třeba vybrat si adresář pro uložení JŘ dopravce DP Ostrava."
        printfn "Pokud ve vybraném adresáři existuje následující podadresář, jeho obsah bude nahrazen nově staženými JŘ."
        printfn "[%s]" <| ODISDefault.OdisDir5
        printfn "%c" <| char(32)
        printfn "Přečti si pozorně výše uvedené a stiskni:"
        printfn "Esc pro ukončení aplikace, ENTER pro výběr adresáře, nebo cokoliv jiného pro návrat na hlavní stránku."

        let pressedKey = Console.ReadKey()

        match pressedKey.Key with
        | ConsoleKey.Enter ->                                                                                     
                            Console.Clear()   
                            
                            match (>>) pathToFolder Option.ofNullEmpty <| () with
                            | Some path ->  
                                         printfn "Skvěle! Adresář byl vybrán. Nyní stiskni cokoliv pro stažení aktuálních JŘ dopravce DP Ostrava."
                                         Console.Clear()

                                         webscraping_DPO path 
                                                                           
                                         printfn "%c" <| char(32)   
                                         printfn "Stiskni Esc pro ukončení aplikace nebo cokoliv jiného pro návrat na hlavní stránku."
                                         Console.ReadKey()             
                            | None      -> 
                                         printfn "%c" <| char(32)   
                                         printfn "No jéje. Vybraný adresář neexistuje."
                                         printfn "Stiskni Esc pro ukončení aplikace nebo cokoliv jiného pro návrat na hlavní stránku."
                                         Console.ReadKey()                                
        | _                ->
                            pressedKey     

    let myWebscraping_MDPO x = 

        Console.Clear()
        printfn "Hromadné stahování aktuálních JŘ ODIS dopravce MDP Opava z webu https://www.mdpo.cz"         
        printfn "JŘ jsou pouze zastávkové - klasické JŘ stáhnete v \"celoODISové\" variantě (volba 3 na úvodní stránce)."   
        printfn "Datum poslední aktualizace SW: %s" updateDate
        printfn "%s" <| String.replicate 70 "*"
        printfn "Nyní je třeba vybrat si adresář pro uložení JŘ dopravce MDP Opava."
        printfn "Pokud ve vybraném adresáři existuje následující podadresář, jeho obsah bude nahrazen nově staženými JŘ."
        printfn "[%s]" <| ODISDefault.OdisDir6       
        printfn "%c" <| char(32) 
        printfn "Přečti si pozorně výše uvedené a stiskni:"
        printfn "Esc pro ukončení aplikace, ENTER pro výběr adresáře, nebo cokoliv jiného pro návrat na hlavní stránku."

        let pressedKey = Console.ReadKey()

        match pressedKey.Key with
        | ConsoleKey.Enter ->                                                                                     
                            Console.Clear()      
                            
                            match (>>) pathToFolder Option.ofNullEmpty <| () with
                            | Some path ->  
                                         printfn "Skvěle! Adresář byl vybrán. Nyní stiskni cokoliv pro stažení aktuálních JŘ dopravce MDP Opava."
                                         Console.Clear()

                                         webscraping_MDPO path 
                                                                           
                                         printfn "%c" <| char(32)   
                                         printfn "Stiskni Esc pro ukončení aplikace nebo cokoliv jiného pro návrat na hlavní stránku."
                                         Console.ReadKey()             
                            | None      -> 
                                         printfn "%c" <| char(32)   
                                         printfn "No jéje. Vybraný adresář neexistuje."
                                         printfn "Stiskni Esc pro ukončení aplikace nebo cokoliv jiného pro návrat na hlavní stránku."
                                         Console.ReadKey()                                
        | _                ->
                            pressedKey     
    
    let myWebscraping_KODIS () = 

        Console.Clear()
        printfn "Hromadné stahování JŘ ODIS všech dopravců v systému ODIS z webu https://www.kodis.cz"           
        printfn "Datum poslední aktualizace SW: %s" updateDate
        printfn "%s" <| String.replicate 70 "*"
        printfn "Nyní je třeba vybrat si adresář pro uložení JŘ všech dopravců v systému ODIS."
        printfn "Pokud ve vybraném adresáři existují následující podadresáře, jejich obsah bude nahrazen nově staženými JŘ."
        printfn "%4c[%s]" <| char(32) <| ODISDefault.OdisDir1
        printfn "%4c[%s]" <| char(32) <| ODISDefault.OdisDir2
        //printfn "%4c[%s]" <| char(32) <| ODISDefault.odisDir3
        printfn "%4c[%s]" <| char(32) <| ODISDefault.OdisDir4  
        printfn "%c" <| char(32) 
        printfn "Přečti si pozorně výše uvedené a stiskni:"
        printfn "Esc pro ukončení aplikace, ENTER pro výběr adresáře, nebo cokoliv jiného pro návrat na hlavní stránku."

        let pressedKey = Console.ReadKey()
        
        match pressedKey.Key with
        | ConsoleKey.Enter ->  
                            Console.Clear()           
                            match (>>) pathToFolder Option.ofNullEmpty <| () with
                            | Some path ->  
                                         Console.Clear()          
                                         printfn "Skvěle! Adresář byl vybrán. Nyní prosím vyber variantu (číslice plus ENTER, příp. jen ENTER pro kompletně všechno)."
                                         printfn "%c" <| char(32)
                                         printfn "1 = Aktuální JŘ, které striktně platí dnešní den, tj. pokud je např. pro dnešní den"
                                         printfn "%4cplatný pouze určitý jednodenní výlukový JŘ, stáhne se tento JŘ, ne JŘ platný od dalšího dne." <| char(32)
                                         printfn "2 = JŘ (včetně výlukových JŘ), platné až v budoucí době, které se však už nyní vyskytují na webu KODISu."
                                         //printfn "3 = Pouze aktuální výlukové JŘ, JŘ NAD a JŘ X linek (krátkodobé i dlouhodobé)."
                                         printfn "3 = JŘ teoreticky dlouhodobě platné bez jakýchkoliv (i dlouhodobých) výluk."
                                         printfn "%c" <| char(32) 
                                         printfn "Jakákoliv jiná klávesa plus ENTER nebo jen ENTER = KOMPLETNÍ stažení všech variant JŘ (doporučeno).\r"        
                                         printfn "%c" <| char(32) 
                                         printfn "%c" <| char(32) 
                                         printfn "Stačí stisknout pouze ENTER pro KOMPLETNÍ stažení všech variant JŘ. A buď trpělivý, chvíli to potrvá."
                                            
                                         let variant = 
                                             Console.ReadLine()
                                             |> function 
                                                 | "1" -> [ CurrentValidity ]
                                                 | "2" -> [ FutureValidity ]  
                                               //| "3" -> [ ReplacementService ]
                                                 | "3" -> [ WithoutReplacementService ]
                                                 | _   -> [ CurrentValidity; FutureValidity; WithoutReplacementService ]
                                            
                                         Console.Clear()
                                            
                                         webscraping_KODISFMRecord path variant //record-based app 
                                         //webscraping_KODISFMDataTable path variant //datatable-based app
                                         //webscraping_KODISFM path variant //database-based app
                                            
                                         printfn "%c" <| char(32)         
                                         printfn "Pokud se v údajích KODISu nacházel odkaz na JŘ, který obsahoval chybné či neúplné údaje,"
                                         printfn "daný JŘ pravděpodobně nebyl stažen (závisí na druhu chyby či povaze neúplného údaje)."
                                         printfn "%c" <| char(32)   
                                         printfn "Stiskni Esc pro ukončení aplikace, nebo cokoliv jiného pro návrat na hlavní stránku."
                                         Console.ReadKey() 
                            | None      -> 
                                         printfn "%c" <| char(32)   
                                         printfn "No jéje. Vybraný adresář neexistuje."
                                         printfn "Stiskni Esc pro ukončení aplikace, nebo cokoliv jiného pro návrat na hlavní stránku."
                                         Console.ReadKey()      
        | _                ->
                            pressedKey                        
        
    //[<TailCall>] //kontrolovano jako module function, bez varovnych hlasek
    let rec variant () = 

        let timetableVariant (fn : ConsoleKeyInfo) = 
            try
                match fn.Key with
                | ConsoleKey.Escape -> Ok <| System.Environment.Exit(0)
                | _                 -> Ok <| variant ()
            with ex -> Error <| string ex.Message
                            
            |> function
                | Ok value  -> 
                             value  
                | Error err ->
                             msgParam1 err
                             logInfoMsg <| sprintf "Err046 %s" err 
                             closeItBaby err

        Console.Clear()

        printfn "Zdravím nadšence do klasických jízdních řádů. Nyní prosím zadejte číslici plus ENTER pro výběr varianty." 
        printfn "%c" <| char(32)  
        printfn "1 = Hromadné stahování netříděných jízdních řádů ODIS pouze dopravce DP Ostrava z webu https://www.dpo.cz (nedoporučuji)"
        printfn "2 = Hromadné stahování pouze zastávkových jízdních řádů ODIS dopravce MDP Opava z webu https://www.mdpo.cz"
        printfn "3 = Hromadné stahování jízdních řádů ODIS všech dopravců v systému ODIS z webu https://www.kodis.cz"
        printfn "%c" <| char(32)  
        printfn "Anebo klikni na křížek pro ukončení aplikace."
                
        let str = "Není připojení k internetu. Obnov jej, bez něj stahování JŘ opravdu nebude fungovat :-)."        
        let boxTitle = "No jéje, zase problém ..."
                                                         
        let checkNetConnection timeout =
            [1..10] 
            |> List.filter (fun _ -> CheckNetConnection.checkNetConn(timeout).IsSome) 
            |> List.length >= 8  
        
        let checkNetConnF = checkNetConnection 500 //timeout
       
        //[<TailCall>] //kontrolovano jako module function, bez varovnych hlasek
        let rec checkConnect checkNetConnP = 

            match checkNetConnP with
            | true  -> 
                     ()
            | false -> 
                     MessageBox.Show
                         (
                             str, 
                             boxTitle, 
                             MessageBoxButtons.OK
                         )  
                         |> ignore
                         
                     (<<) checkConnect checkNetConnection <| 500 //timeout
        
        checkConnect checkNetConnF

        let captureLinks executablePath = 

            let capturedLinks = capturedLinks executablePath                  

            printfn "%s" <| String.replicate 70 "*"
        
            match (fst capturedLinks) = (jsonLinkList3 |> List.sort) with
            | true  -> printfn "Kontrola na odkazy v jsonLinkList3 proběhla v pořádku."
            | false -> printfn "Chyba v odkazech v jsonLinkList3, nutno ověření."

            printfn "%s" <| String.replicate 70 "*"

            match (snd capturedLinks) = (jsonLinkList2 |> List.tail |> List.sort)  with
            | true  -> printfn "Kontrola na odkazy v jsonLinkList2 proběhla v pořádku."
            | false -> printfn "Chyba v odkazech v jsonLinkList2, nutno ověření."            
           
        Console.ReadLine()
        |> function 
            | "1"     ->
                       myWebscraping_DPO >> timetableVariant <| ()                     
            | "2"     ->
                       myWebscraping_MDPO >> timetableVariant <| ()                      
            | "3"     ->
                       myWebscraping_KODIS >> timetableVariant <| ()   
            | "74764" -> 
                       printfn "\nTrefil jsi zrovna kód pro přístup k testování shodnosti odkazů na JSON soubory."
                       printfn "Gratuluji, ale pokud nevíš, co test obnáší, raději ukonči tento program ... \n"  
                   
                       let executablePath = @"c:\Program Files\AVG\Browser\Application\AVGBrowser.exe" 
                       captureLinks executablePath
                   
                       printfn "Stiskni cokoliv pro návrat na hlavní stránku."
                       Console.ReadKey() |> ignore

                       variant()
            | "74283" ->                       
                       printfn "\nTrefil jsi zrovna kód pro přístup k testování shodnosti odkazů na JSON soubory."
                       printfn "Asi ti test bude fungovat, neb Google Chrome má instalovaný kdekdo," 
                       printfn "ale pokud nevíš, co test obnáší, raději ukonči tento program ... \n"  
                   
                       let executablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe" 
                       captureLinks executablePath

                       printfn "Stiskni cokoliv pro návrat na hlavní stránku."
                       Console.ReadKey() |> ignore

                       variant()
            | "70800" -> 
                       printfn "\nTrefil jsi zrovna kód pro přístup k testování počtu a velikosti stažených souborů."
                       printfn "Gratuluji, ale pokud nevíš, co test obnáší, raději ukonči tento program ... \n"  
                   
                       DtDbVariantTest.Test.main () 

                       printfn "Stiskni cokoliv pro návrat na hlavní stránku."
                       Console.ReadKey() |> ignore

                       variant()  
            | _       ->
                       printfn "Varianta nebyla vybrána. Prosím zadej znovu."
                       variant()
                   
    variant()   
    0 // return an integer exit code