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


open canopy.runner
open canopy.types
open canopy.configuration
open canopy.classic

module MyCanopy =

    //testing
    
    let internal canopyResult () = 
    
        try
            canopy.configuration.edgeDir <- @"c:/temp/driver" 
            canopy.classic.start canopy.classic.edgeBETA
    
            canopy.configuration.compareTimeout <- 100.0 
    
            //In CSS selectors, the . before a word or identifier specifies a class.    
            //<li class="Card_wrapper__ZQ5Fp">

            let linksShown () = (canopy.classic.elements ".Card_actions__HhB_f").Length >= 1
            
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

            let scrapeUrl (url: string) =
                try
                    canopy.classic.url url

                    let pdfLinkSeq () =

                        Thread.Sleep 20000                     
                        canopy.classic.waitFor linksShown    
                         
                        canopy.classic.elements "button[title='Budoucí jízdní řády']"  //buttons
                        |> Seq.collect 
                            (fun button -> 
                                        canopy.classic.click button 

                                        Thread.Sleep 2000   
                                            
                                        let result = 
                                            canopy.classic.elements "a"
                                            |> Seq.map 
                                                (fun item ->                                                     
                                                           let href = string <| item.GetAttribute("href")
                                                           match href.EndsWith("pdf") with
                                                           | true  -> Some href     
                                                           | false -> None
                                                                    
                                                )                                            
                                        canopy.classic.navigate forward
                                        result
                            )                               
                        |> Seq.distinct
                        |> Seq.toList                        
                                         
                    let clickCondition () =
                        try
                            let nextButton = canopy.classic.elementWithText "a" "Další"
                            nextButton.Displayed && nextButton.Enabled
                        with
                        | _ -> false
    
                    let pdfLinkList1 = pdfLinkSeq () |> List.distinct

                    let pdfLinkList2 = 
                        Seq.initInfinite (fun _ -> clickCondition())
                        |> Seq.takeWhile ((=) true) 
                        |> Seq.collect
                            (fun _ -> 
                                    canopy.classic.click (canopy.classic.elementWithText "a" "Další")
                                    pdfLinkSeq ()
                            )
                        |> Seq.distinct
                        |> Seq.toList                  

                    (pdfLinkList1 @ pdfLinkList2) |> List.choose id  

                with
                | _ ->
                     Console.BackgroundColor <- ConsoleColor.Blue 
                     Console.ForegroundColor <- ConsoleColor.White 
                     printfn "Na tomto odkazu se buď momentálně nenachází žádné JŘ, anebo to Canopy nezvládl: %s" url 
                     [] 
                                     
            let list = 
                urls 
                |> List.collect scrapeUrl
                |> List.filter
                    (fun (item : string) 
                        -> 
                        printfn "%s" item
                        not <| item.Contains "2022"
                    )    

            serializeToJsonThoth2 list "CanopyResults/canopy_results.json" 

        with
        | ex -> Error (sprintf "%s %s" <| string ex.Message <| " Error Canopy 001")

    type ResponsePut = 
        {
            Message1 : string
            Message2 : string
        }

    let internal decoderPutTest : Decoder<ResponsePut> =
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
                         Decode.fromString decoderPutTest jsonMsg   
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