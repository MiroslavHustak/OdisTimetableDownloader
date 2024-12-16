namespace MyCanopy

open System
open System.Threading

open Serialization.Serialisation

open System
open System.IO
open System.Net

open FsHttp
open Thoth.Json.Net
open FsToolkit.ErrorHandling

open MyFsToolkit
open System.Threading
open MyFsToolkit.Builders

module MyCanopy = 
    
    let internal canopyResult () = 

        let urls = 
            [
                "https://www.kodis.cz/lines/city?tab=MHD+Ostrava"
                "https://www.kodis.cz/lines/region?tab=75" 
                "https://www.kodis.cz/lines/city?tab=MHD+Opava"
                "https://www.kodis.cz/lines/region?tab=232-293"
                "https://www.kodis.cz/lines/city?tab=MHD+Frýdek-Místek"
                "https://www.kodis.cz/lines/region?tab=331-392"
                "https://www.kodis.cz/lines/city?tab=MHD+Havířov"
                "https://www.kodis.cz/lines/region?tab=440-465"
                "https://www.kodis.cz/lines/city?tab=MHD+Karviná"
                "https://www.kodis.cz/lines/city?tab=MHD+Orlová"
                "https://www.kodis.cz/lines/region?tab=531-583"
                "https://www.kodis.cz/lines/city?tab=MHD+Nový+Jičín"
                "https://www.kodis.cz/lines/city?tab=MHD+Studénka"
                "https://www.kodis.cz/lines/region?tab=613-699"
                "https://www.kodis.cz/lines/city?tab=MHD+Třinec"
                "https://www.kodis.cz/lines/city?tab=MHD+Český+Těšín"
                "https://www.kodis.cz/lines/region?tab=731-788"
                "https://www.kodis.cz/lines/city?tab=MHD+Krnov"
                "https://www.kodis.cz/lines/city?tab=MHD+Bruntál"
                "https://www.kodis.cz/lines/region?tab=811-885"
                "https://www.kodis.cz/lines/region?tab=901-990"
                "https://www.kodis.cz/lines/train?tab=S1-S34"
                "https://www.kodis.cz/lines/train?tab=R8-R62"
                "https://www.kodis.cz/lines/city?tab=NAD+MHD"
                "https://www.kodis.cz/lines/region?tab=NAD"   
            ]

        let scrapeGeneral () = 

            canopy.classic.elements "a" //tohle pak vezme vse, jak odkazy z Card_actions__HhB_f, tak odkazy z cudliku 'Budoucí jízdní řády' 
            |> List.map 
                (fun item
                    ->                                                     
                    let href = string <| item.GetAttribute("href")
                    match href.EndsWith("pdf") with
                    | true  -> Some href     
                    | false -> None                                                                    
                )    

        let clickCondition () =

            try                             
                let nextButton = canopy.classic.elementWithText "a" "Další"
                nextButton.Displayed && nextButton.Enabled
            with
            | _ -> false  
        
        let currentAndFutureLinks () = 

            try
                canopy.configuration.edgeDir <- @"c:/temp/driver" 
                canopy.classic.start canopy.classic.edgeBETA
    
                canopy.configuration.compareTimeout <- 100.0 
    
                //In CSS selectors, the . before a word or identifier specifies a class.    
                //<li class="Card_wrapper__ZQ5Fp">

                let linksShown () = (canopy.classic.elements ".Card_actions__HhB_f").Length >= 1               

                let scrapeUrl (url: string) =
                    try
                        canopy.classic.url url

                        let pdfLinkList () =

                            Thread.Sleep 15000            
                        
                            canopy.classic.waitFor linksShown                         
                        
                            let buttons = canopy.classic.elements "button[title='Budoucí jízdní řády']"  //buttons, je jich +- 12 na kazdu page
                         
                            let result =  
                                buttons
                                |> List.mapi 
                                    (fun i button 
                                        -> 
                                        canopy.classic.click button 

                                        Thread.Sleep 2000   
                                            
                                        let result = scrapeGeneral ()                                       
                                    
                                        match i = buttons.Length - 1 with 
                                        | true  ->       
                                                canopy.classic.waitForElement "[id*='headlessui-menu-item']" //u posledniho pockame na pop-up z 'Budoucí jízdní řády', kery prekryva "Další"
                                                canopy.classic.click button //klikneme jeste jednou na posledni, abychom odkryli "Další"
                                                Thread.Sleep 2000   
                                        | false -> 
                                                () 

                                        canopy.classic.navigate canopy.classic.forward //je to zajimave, ale zrobi to 'back', tj. to, co potrebuju
                                        result 
                                    )
                                |> List.concat    
                                |> List.distinct   
                            
                            result //musi tady byt specialne takto (nelze primo bez result, tj. vlastne 2x), aby se spustilo "Další"
                            
                        let pdfLinkList1 = pdfLinkList () |> List.distinct  //prvni pruchod

                        let pdfLinkList2 = 
                            Seq.initInfinite (fun _ -> clickCondition())  //druhy a dalsi
                            |> Seq.takeWhile ((=) true) 
                            |> Seq.collect
                                (fun _ -> 
                                        try 
                                            canopy.classic.click (canopy.classic.elementWithText "a" "Další")
                                            pdfLinkList ()
                                        with
                                        | ex -> 
                                              printfn "%s" (string ex.Message) 
                                              []
                                )
                            |> Seq.distinct
                            |> Seq.toList                  

                        (pdfLinkList1 @ pdfLinkList2) |> List.choose id  

                    with
                    | ex ->
                         Console.BackgroundColor <- ConsoleColor.Blue 
                         Console.ForegroundColor <- ConsoleColor.White 
                     
                         printfn "Na tomto odkazu se buď momentálně nenachází žádné JŘ, anebo to Canopy nezvládl: %s" url // (string ex.Message) 
                         printfn "Zkusíme něco dalšího." 
                                                  
                         [] 

                urls 
                |> List.collect scrapeUrl
                |> List.filter
                    (fun (item : string) 
                        -> 
                        printfn "%s" item
                        not <| (item.Contains "2022" || item.Contains "2023")
                    )     
            with
            | ex -> 
                  printf "%s %s" <| string ex.Message <| " Error Canopy 001 - c&f links"
                  []

        let currentLinks () = 
            
            try
                canopy.configuration.edgeDir <- @"c:/temp/driver" 
                canopy.classic.start canopy.classic.edgeBETA
                
                canopy.configuration.compareTimeout <- 100.0 
                
                //In CSS selectors, the . before a word or identifier specifies a class.    
                //<li class="Card_wrapper__ZQ5Fp">
            
                let linksShown () = (canopy.classic.elements ".Card_actions__HhB_f").Length >= 1
            
                let scrapeUrl (url: string) =
                    try
                        canopy.classic.url url
            
                        let pdfLinkList () =
                            Thread.Sleep 15000  
                            canopy.classic.waitFor linksShown  
                            scrapeGeneral ()  
                                        
                        let pdfLinkList1 = pdfLinkList () |> List.distinct  //prvni pruchod
            
                        let pdfLinkList2 = 
                            Seq.initInfinite (fun _ -> clickCondition())  //druhy a dalsi
                            |> Seq.takeWhile ((=) true) 
                            |> Seq.collect
                                (fun _ -> 
                                        try 
                                            canopy.classic.click (canopy.classic.elementWithText "a" "Další")
                                            pdfLinkList ()
                                        with
                                        | ex -> 
                                              printfn "%s" (string ex.Message) 
                                              []
                                )
                            |> Seq.distinct
                            |> Seq.toList                  
            
                        (pdfLinkList1 @ pdfLinkList2) |> List.choose id  
            
                    with
                    | ex ->
                          Console.BackgroundColor <- ConsoleColor.Blue 
                          Console.ForegroundColor <- ConsoleColor.White 
                            
                          printfn "Na tomto odkazu to Canopy opravdu nezvládl: %s" url // (string ex.Message) 
                          []        

                urls 
                |> List.collect scrapeUrl
                |> List.filter
                    (fun (item : string) 
                        -> 
                        printfn "%s" item
                        not <| (item.Contains "2022" || item.Contains "2023")
                    )   
            with
            | ex -> 
                  printf "%s %s" <| string ex.Message <| " Error Canopy 001 - c only links"
                  []
        
        try
            let list = (currentAndFutureLinks () @ currentLinks ()) |> List.distinct
            serializeToJsonThoth2 list "CanopyResults/canopy_results.json" 
        with
        | ex ->  
              Error <| (sprintf "%s %s" <| string ex.Message <| " Error Canopy 001 combined")    

    type ResponsePut = 
        {
            Message1 : string
            Message2 : string
        }

    let internal decoderPut : Decoder<ResponsePut> =
        Decode.object
            (fun get ->
                      {
                          Message1 = get.Required.Field "Message1" Decode.string
                          Message2 = get.Required.Field "Message2" Decode.string
                      }
            )

    let internal putToRestApiTest () =
    
        let getJsonString path =
    
            try
                pyramidOfDoom
                    {
                        let filepath = Path.GetFullPath path |> Option.ofNullEmpty 
                        let! filepath = filepath, Error (sprintf "%s%s" "Chyba při čtení cesty k souboru " path)
    
                        let fInfodat : FileInfo = FileInfo filepath
                        let! _ =  fInfodat.Exists |> Option.ofBool, Error (sprintf "Soubor %s nenalezen" path) 
                     
                        use fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.None) 
                        let! _ = fs |> Option.ofNull, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " filepath)                        
                        
                        use reader = new StreamReader(fs) //For large files, StreamReader may offer better performance and memory efficiency
                        let! _ = reader |> Option.ofNull, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " filepath) 
                    
                        let jsonString = reader.ReadToEnd()
                        let! jsonString = jsonString |> Option.ofNullEmpty, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " filepath)                      
                                      
                        return Ok jsonString 
                    }
            with
            | ex -> Error (sprintf "%s %s" <| string ex.Message <| " Error Canopy 002")
    
        async
            {
                let path = "CanopyResults/canopy_results.json"                
                let url = "http://kodis.somee.com/api/" 
                let apiKeyTest = "test747646s5d4fvasfd645654asgasga654a6g13a2fg465a4fg4a3"
                                                      
                let thothJsonPayload =                    
                    match getJsonString path with
                    | Ok jsonString -> jsonString                                  
                    | Error _       -> String.Empty            
               
                let! response = 
                    http
                        {
                            PUT url
                            header "X-API-KEY" apiKeyTest 
                            body 
                            json thothJsonPayload
                        }
                    |> Request.sendAsync       
                        
                match response.statusCode with
                | HttpStatusCode.OK 
                    -> 
                     let! jsonMsg = Response.toTextAsync response
    
                     return                          
                         Decode.fromString decoderPut jsonMsg   
                         |> function
                             | Ok value  -> value   
                             | Error err -> { Message1 = String.Empty; Message2 = (sprintf "%s %s" <| err <| " Error Canopy 003") }      
                | _ -> 
                     return { Message1 = String.Empty; Message2 = sprintf "Request failed with status code %d" (int response.statusCode) }                                           
            } 
        |> Async.Catch 
        |> Async.RunSynchronously  
        |> Result.ofChoice    
        |> function
            | Ok value -> value 
            | Error ex -> { Message1 = String.Empty; Message2 = (sprintf "%s %s" <| string ex.Message <| " Error Canopy 004")}   