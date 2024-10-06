namespace SubmainFunctions

module MDPO_Submain =

    open System
    open System.IO
    open System.Net

    open FsHttp
    open FSharp.Data
    open FsToolkit.ErrorHandling

    //********************************
       
    open MyFsToolkit

    //********************************

    open Logging.Logging

    open Types.ErrorTypes

    open Settings.Messages
    open Settings.SettingsMDPO
    open Settings.SettingsGeneral

    open Helpers.CloseApp
    open Helpers.ProgressBarFSharp

    //DO NOT DIVIDE THIS MODULE YET !!!

    type HtmlTypeProvider = HtmlProvider<pathMdpoWebTimetables>

    //************************Submain functions************************************************************************

    let internal filterTimetables () pathToDir = 

        let urlList = //aby to bylo jednotne s DPO
            [
                pathMdpoWebTimetables
            ]

        urlList    
        |> Seq.collect 
            (fun url -> 
                      let document = FSharp.Data.HtmlDocument.Load url //neni nullable, nesu exn
                      //HtmlDocument -> web scraping -> extracting data from HTML pages
                                                                                    
                      document.Descendants "a"                  
                      |> Seq.choose 
                          (fun htmlNode    ->
                                            //htmlNode.TryGetAttribute("href") //inner text zatim nepotrebuji, cisla linek mam resena jinak  
                                            //|> Option.map (fun a -> string <| htmlNode.InnerText(), string <| a.Value()) //priste to uz tak nerobit, u string zrob Option.ofNull, atd. 
                                               
                                            htmlNode.TryGetAttribute "href" //inner text zatim nepotrebuji, cisla linek mam resena jinak  
                                            |> Option.bind
                                                (fun attr -> 
                                                           let nodes = htmlNode.InnerText() |> Option.ofNullEmpty
                                                           let attr = attr.Value() |> Option.ofNullEmpty
                                                           
                                                           match nodes, attr with
                                                           | Some nodes, Some attr -> Some (nodes, attr)
                                                           | _                     -> None 
                                                )                                                     
                          )      
                      |> Seq.filter 
                          (fun (_ , item2) -> 
                                            item2.Contains @"/qr/" && item2.Contains ".pdf"
                          )
                      |> Seq.map 
                          (fun (_ , item2) ->                                                                 
                                            let linkToPdf = sprintf "%s%s" pathMdpoWeb item2  //https://www.mdpo.cz // /qr/201.pdf
                                            let lineName (item2 : string) = item2.Replace(@"/qr/", String.Empty)  
                                            let pathToFile lineName = sprintf "%s/%s" pathToDir lineName

                                            linkToPdf, (pathToFile << lineName) item2
                          )                          
                      |> Seq.distinct                 
            )  
        |> Seq.fold (fun acc (key, value) -> Map.add key value acc) Map.empty //vyzkousime si tvorbu Map
           

    //Html Type Provider - for educational purposes
    let internal filterTimetables2 () pathToDir = 
               
        let htmlNodeSeq = HtmlTypeProvider.Load(pathMdpoWebTimetables).Lists.Html.Descendants "a" 
                                                                                    
        htmlNodeSeq              
        |> Seq.choose 
            (fun htmlNode    ->
                              htmlNode.TryGetAttribute("href") //inner text zatim nepotrebuji, cisla linek mam resena jinak 
                              |> Option.map (fun a -> string <| htmlNode.InnerText(), string <| a.Value()) //priste to uz tak nerobit, u string zrob Option.ofStringObj, atd.                                            
            )      
        |> Seq.filter 
            (fun (_ , item2) -> 
                              item2.Contains @"/qr/" && item2.Contains ".pdf"
            )
        |> Seq.map 
            (fun (_ , item2) ->                                                                 
                              let linkToPdf = sprintf "%s%s" pathMdpoWeb item2  //https://www.mdpo.cz // /qr/201.pdf
                              let lineName (item2 : string) = item2.Replace(@"/qr/", String.Empty)  
                              let pathToFile lineName = sprintf "%s/%s" pathToDir lineName

                              linkToPdf, (pathToFile << lineName) item2
            )                          
        |> Seq.distinct     
        |> Seq.fold (fun acc (key, value) -> Map.add key value acc) Map.empty

    //FsHttp
    let internal downloadAndSaveTimetables (pathToDir : string) (filterTimetables : Map<string, string>) =  

        let downloadFileTaskAsync (uri : string) (path : string) : Async<Result<unit, string>> =  
            
            async
                {                      
                    try    
                        match File.Exists(path) with
                        | true  -> 
                                 return Ok () 
                        | false -> 
                                 use! response = get >> Request.sendAsync <| uri //anebo get rucne definovane viz Bungie.NET let get uri = http { GET (uri) }                                                         
                                        
                                 match response.statusCode with
                                 | HttpStatusCode.OK                  ->                                                                   
                                                                       do! response.SaveFileAsync >> Async.AwaitTask <| path
                                                                       return Ok () 
                                 | HttpStatusCode.BadRequest          ->                                                                       
                                                                       return Error connErrorCodeDefault.BadRequest
                                 | HttpStatusCode.InternalServerError -> 
                                                                       return Error connErrorCodeDefault.InternalServerError
                                 | HttpStatusCode.NotImplemented      ->
                                                                       return Error connErrorCodeDefault.NotImplemented
                                 | HttpStatusCode.ServiceUnavailable  ->
                                                                       return Error connErrorCodeDefault.ServiceUnavailable
                                 | HttpStatusCode.NotFound            ->
                                                                       return Error uri  
                                 | _                                  ->
                                                                       return Error connErrorCodeDefault.CofeeMakerUnavailable                                                   
                                      
                    with                                                         
                    | ex -> 
                          logInfoMsg <| sprintf "Err039 %s" (string ex.Message)
                          closeItBaby msg21                                                 
                          return Error msg21    
                }                 
    
        msgParam3 pathToDir 
    
        let downloadTimetables = 
        
            let l = filterTimetables |> Map.count
        
            filterTimetables
            |> Map.toList 
            |> List.iteri  //bohuzel s Map nelze iteri
                (fun i (link, pathToFile) 
                    -> 
                     //vzhledem k nutnosti propustit chybu pri nestahnuti JR (message.msgParam2 link) nepouzito Result.sequence   
                     let mapErr3 err =    
                         function
                         | Ok value  ->
                                      value   
                                      |> List.tryFind ((=) err)
                                      |> function
                                         | Some err ->
                                                     logInfoMsg <| sprintf "Err040 %s" err
                                                     closeItBaby err                                                                      
                                         | None     -> 
                                                     msgParam2 link 
                         | Error err ->
                                      logInfoMsg <| sprintf "Err041 %s" err
                                      closeItBaby err              

                     let mapErr2 =      
                         function
                         | Ok value  ->
                                      value |> ignore
                         | Error err ->
                                      logInfoMsg <| sprintf "Err042 %s" err
                                      mapErr3 err (Ok listConnErrorCodeDefault) //Ok je legacy zruseneho reflection a Result.sequence
                                                 
                     async                                                
                         {   
                             progressBarContinuous (i + 1) l   
                             return! downloadFileTaskAsync link pathToFile                                                                                                                               
                         } 
                     |> Async.Catch
                     |> Async.RunSynchronously
                     |> Result.ofChoice  
                     |> Result.mapErr mapErr2 (lazy msgParam2 link)                                                   
                )     

        downloadTimetables  
    
        msgParam4 pathToDir