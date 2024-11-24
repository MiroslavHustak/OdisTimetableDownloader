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

    //testing
    
    let internal canopyResult () = 
    
        try
            canopy.configuration.edgeDir <- @"c:/temp/driver" 
            canopy.classic.start canopy.classic.edgeBETA
    
            canopy.configuration.compareTimeout <- 100.0 
    
            //In CSS selectors, the . before a word or identifier specifies a class.    
            //<li class="Card_wrapper__ZQ5Fp">
            let linksShown () = (canopy.classic.elements ".Card_actions__HhB_f").Length >= 1 // //".Card_wrapper__ZQ5Fp" //.Card_actions__HhB_f

            let linksShown2 () = (canopy.classic.elements ".Card_actions__HhB_f").Length >= 1 // //".Card_wrapper__ZQ5Fp" //.Card_actions__HhB_f //<div class="Card_menu__mL6mB" aria-labelledby="headlessui-menu-button-52" id="headlessui-menu-items-69" role="menu" tabindex="0"><div class="py-1" role="none"><a href="/changes/2227" class="text-gray-700 Card_menuItem__Q0EYk" id="headlessui-menu-item-70" role="menuitem" tabindex="-1">POZOR! ZMĚNA! Oprava kruhového objezdu v Opavě - Jaktaři</a><a href="https://kodis-files.s3.eu-central-1.amazonaws.com/249_2024_11_13_2024_11_19_v_db3f413f26.pdf" target="_blank" class="text-gray-700 Card_menuItem__Q0EYk" id="headlessui-menu-item-71" role="menuitem" tabindex="-1">Úplná uzavírka Sportovní ulice ve Velkých Heralticích</a></div></div>
            
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

                canopy.classic.url url

                let pdfLinkSeq () =

                    Thread.Sleep 10000                     
                    canopy.classic.waitFor linksShown 
                    
                    //canopy.classic.elements "a[title='Aktuální jízdní řád']" //title="Pravidelný jízdní řád" 

                    canopy.classic.elements "a"
                    |> Seq.map 
                        (fun item -> 
                                   let href = string <| item.GetAttribute("href")
                                   match href.EndsWith("pdf") with
                                   | true  -> 
                                            printfn "%s" href
                                            Some href
                                   | false -> 
                                            None
                        )
                    |> Seq.distinct

                let clickCondition () =
                    try
                        let nextButton = canopy.classic.elementWithText "a" "Další"
                        nextButton.Displayed && nextButton.Enabled
                    with
                    | _ -> false
    
                let pdfLinkList1 = pdfLinkSeq () |> Seq.distinct |> List.ofSeq

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
            
            let list = 
                urls 
                |> List.collect scrapeUrl
                |> List.filter (fun item -> not <| item.Contains "2022")
           
            serializeToJsonThoth2 list "CanopyResults/canopy_results.json" 

        with
        | ex -> Error (string ex.Message)

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
            | ex -> Error (string ex.Message)
    
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
                             | Error err -> { Message1 = String.Empty; Message2 = err }      
                | _ -> 
                     return { Message1 = String.Empty; Message2 = sprintf "Request failed with status code %d" (int response.statusCode) }                                           
            } 
        |> Async.Catch 
        |> Async.RunSynchronously  
        |> Result.ofChoice    
        |> function
            | Ok value -> value 
            | Error ex -> { Message1 = String.Empty; Message2 = string ex.Message }   