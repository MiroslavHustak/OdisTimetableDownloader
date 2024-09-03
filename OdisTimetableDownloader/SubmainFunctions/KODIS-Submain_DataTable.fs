namespace SubmainFunctions

open System
open System.IO
open System.Net
open System.Threading
open System.Net.NetworkInformation
open System.Text.RegularExpressions

open FsHttp
open FSharp.Control
open FsToolkit.ErrorHandling
open FSharp.Quotations.Evaluator.QuotationEvaluationExtensions

//************************************************************

open MyFsToolkit
open MyFsToolkit.Builders
open EmbeddedTP.EmbeddedTP

//************************************************************

open Types
open Types.Types

open Logging.Logging

open Settings.Messages
open Settings.SettingsKODIS
open Settings.SettingsGeneral

open Helpers.MyString

open Helpers.CloseApp   
open Helpers.MsgBoxClosing
open Helpers.ProgressBarFSharp  

open DataModelling.DataModel
open TransformationLayers.TransformationLayerSend

module KODIS_SubmainDataTable =    
        
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!     
    // DO NOT DIVIDE this module into parts in line with the main design yet - KODIS keeps making unpredictable changes or amendments
    // LEAVE THE COMMENTED CODE AS IT IS !!! DO NOT DELETE IT !!! IT IS THERE FOR A REASON.


    //*************************Helpers************************************************************

    //space for helpers  
    
    //********************* Infinite checking for Json files download ******************************
    
    //Cancellation tokens for educational purposes
    let private cts = new CancellationTokenSource() //TODO podumat, kaj zrobit cts.Dispose()
    let private tokenJson = cts.Token 

    let internal startNetChecking () =  

        AsyncSeq.initInfinite (fun _ -> tokenJson.IsCancellationRequested)
        |> AsyncSeq.takeWhile ((=) false) 
        |> AsyncSeq.iterAsync
            (fun _ ->
                    async
                        {
                            match not <| NetworkInterface.GetIsNetworkAvailable() with
                            | true  -> (processorJson () 120000).Post(First 1)                                                                                                                                            
                            | false -> () 
                            do! Async.Sleep(3000)                            
                        }
            )   
        |> Async.StartImmediate   

    //************************Main code***********************************************************
        
    //data from settings -> http request -> IO operation -> saving json files on HD 
    let internal downloadAndSaveJson jsonLinkList pathToJsonList = //FsHttp
               
        let l = jsonLinkList |> List.length

        let counterAndProgressBar =
            MailboxProcessor.Start <|
                fun inbox ->
                           let rec loop n = async { match! inbox.Receive() with Inc i -> progressBarContinuous n l; return! loop (n + i) }
                           loop 0

        let result = 
            (jsonLinkList, pathToJsonList)            
            ||> List.Parallel.map2 
                (fun (uri: string) path
                    ->                       
                     async
                         {    
                             use! response = (>>) get Request.sendAsync <| uri 

                             match response.statusCode with
                             | HttpStatusCode.OK
                                 ->                                                                                                   
                                  counterAndProgressBar.Post(Inc 1)                                                   
                                  do! (>>) response.SaveFileAsync Async.AwaitTask <| path                                                   
                                  return Ok ()                                
                             | _ ->  
                                  return Error "HttpStatusCode.OK is not OK"     
                         }  
                     |> Async.Catch
                     |> Async.RunSynchronously
                     |> Result.ofChoice
                )
           
        pyramidOfInferno
            {
                let errorFn1 err = 
                    logInfoMsg <| sprintf "Err001 %s" err
                    closeItBaby msg5A

                let errorFn2 (err : exn) = 
                    logInfoMsg <| sprintf "Err002 %s" (string err.Message)
                    closeItBaby msg5A

                let! value = result |> Result.sequence, errorFn2 
                let! value = value |> Result.sequence, errorFn1

                return value |> List.head
            }           
    
    //input from saved json files -> change of input data -> output into seq
    let private digThroughJsonStructure () = //prohrabeme se strukturou json souboru 
        
        let kodisTimetables : Reader<string list, string seq> = 

            reader //Reader monad for educational purposes only, no real benefit here  
                {
                    let! pathToJsonList = fun env -> env 

                    let result () = 
                        pathToJsonList 
                        |> Seq.ofList 
                        |> Seq.collect 
                            (fun pathToJson 
                                ->   
                                 let json = 
                                     pyramidOfDoom
                                         {
                                             let filepath = Path.GetFullPath(pathToJson) //pathToJson pod kontrolou, filepath nebude null
                                                
                                             let fInfoDat = new FileInfo(pathToJson)
                                             let! _ = fInfoDat.Exists |> Option.ofBool, String.Empty
                                     
                                             let fs = File.ReadAllText(pathToJson) //pathToJson pod kontrolou, fs nebude null
                                            
                                             return fs
                                         }   
                                         
                                 JsonProvider1.Parse(json) 
                                 |> Option.ofNull  
                                 |> function 
                                     | Some value -> value |> Seq.map _.Timetable                                                
                                     | None       -> Seq.empty //tady nelze Result.sequence 
                            )  
                        
                    return
                        try
                           let (|EmptySeq|_|) a =
                               match Seq.isEmpty a with
                               | true  -> Some () 
                               | false -> None
                            
                           let value = result ()   
                           
                           value
                           |> function
                               | EmptySeq -> Error msg16                                        
                               | _        -> Ok value

                        with ex -> Error <| string ex.Message  

                        |> function
                            | Ok value  -> 
                                         value
                            | Error err -> 
                                         logInfoMsg <| sprintf "Err004A %s" err 
                                         closeItBaby msg16 
                                         Seq.empty      
                }
            
        let kodisTimetables2 : Reader<string list, string seq> = 

            reader //Reader monad for educational purposes only, no real benefit here  
                {
                    let! pathToJsonList2 = fun env -> env 

                    let result () = 

                        pathToJsonList2 
                        |> Seq.ofList 
                        |> Seq.collect 
                            (fun pathToJson 
                                ->                                       
                                 let json = //tady nelze Result.sequence 
                                     pyramidOfDoom
                                         {
                                             let filepath = Path.GetFullPath(pathToJson) //pathToJson pod kontrolou, filepath nebude null
                                             
                                             let fInfoDat = new FileInfo(pathToJson)
                                             let! _ = fInfoDat.Exists |> Option.ofBool, String.Empty
                                     
                                             let fs = File.ReadAllText(pathToJson) //pathToJson pod kontrolou, fs nebude null                                       
                                                                                                  
                                             return fs
                                         }    

                                 let kodisJsonSamples = JsonProvider2.Parse(json) |> Option.ofNull
                                 
                                 let timetables = 
                                     kodisJsonSamples 
                                     |> function 
                                         | Some value -> 
                                                       value.Data
                                                       |> Seq.map _.Timetable  //nejde Some, nejde Ok
                                         | None       -> 
                                                       Seq.empty  
                                 
                                 let vyluky = 
                                     kodisJsonSamples 
                                     |> function 
                                        | Some value -> 
                                                      value.Data 
                                                      |> Seq.collect _.Vyluky  //nejde Some, nejde Ok
                                        | None       -> 
                                                      Seq.empty  
                                 
                                 let attachments = 
                                     vyluky
                                     |> Option.ofNull 
                                     |> function
                                         | Some value ->
                                                       value
                                                       |> Seq.collect (fun item -> item.Attachments)
                                                       |> Array.ofSeq
                                                       |> Array.Parallel.map (fun item -> item.Url |> Option.ofNullEmptySpace)                                
                                                       |> Array.choose id //co neprojde, to beze slova ignoruju
                                                       |> Array.toSeq
                                         | None       ->
                                                       Seq.empty  

                                 Seq.append timetables attachments   
                            )                     
                        
                    return
                        try 
                            let (|EmptySeq|_|) a =
                               match Seq.isEmpty a with
                               | true  -> Some () 
                               | false -> None

                            let value = result () 
                            
                            value
                            |> function
                                | EmptySeq -> Error msg16                                        
                                | _        -> Ok value

                        with ex -> Error <| string ex.Message  

                        |> function
                            | Ok value  -> 
                                         value
                            | Error err ->
                                         logInfoMsg <| sprintf "Err004 %s" err 
                                         closeItBaby msg16 
                                         Seq.empty      
                }       
         
        let kodisAttachments : Reader<string list, string seq> = //Reader monad for educational purposes only, no real benefit here
            
                reader 
                    {
                        let! pathToJsonList = fun env -> env 
                        
                        let result () = 

                            pathToJsonList
                            |> Seq.ofList 
                            |> Seq.collect  //vzhledem ke komplikovanosti nepouzivam Result.sequence pro Array.collect (po zmene na seq ocekavam to same), nejde Some, nejde Ok jako vyse
                                (fun pathToJson 
                                    -> 
                                     let fn1 (value : JsonProvider1.Attachment seq) = 
                                         value
                                         |> Array.ofSeq
                                         |> Array.Parallel.map (fun item -> item.Url |> Option.ofNullEmptySpace) //jj, funguje to :-)                                    
                                         |> Array.choose id //co neprojde, to beze slova ignoruju
                                         |> Array.toSeq

                                     let fn2 (item : JsonProvider1.Vyluky) =    
                                         item.Attachments 
                                         |> Option.ofNull        
                                         |> function 
                                             | Some value ->
                                                           value |> fn1
                                             | None       -> 
                                                           msg5 () 
                                                           logInfoMsg <| sprintf "007A %s" "resulting in None"
                                                           Seq.empty                

                                     let fn3 (item : JsonProvider1.Root) =  
                                         item.Vyluky
                                         |> Option.ofNull  
                                         |> function 
                                             | Some value ->
                                                           value |> Seq.collect fn2 
                                             | None       ->
                                                           msg5 () 
                                                           logInfoMsg <| sprintf "007B %s" "resulting in None"
                                                           Seq.empty   

                                     let json = //tady nelze Result.sequence 
                                         pyramidOfDoom
                                             {
                                                 let filepath = Path.GetFullPath(pathToJson) //pathToJson pod kontrolou, filepath nebude null
                                                 
                                                 let fInfoDat = new FileInfo(pathToJson)
                                                 let! _ = fInfoDat.Exists |> Option.ofBool, String.Empty
                                     
                                                 let fs = File.ReadAllText(pathToJson) //pathToJson pod kontrolou, fs nebude null

                                                 return fs
                                             }    
                                                          
                                     let kodisJsonSamples = JsonProvider1.Parse(json) |> Option.ofNull  
                                                          
                                     kodisJsonSamples 
                                     |> function 
                                         | Some value -> 
                                                       value |> Seq.collect fn3 
                                         | None       -> 
                                                       msg5 () 
                                                       logInfoMsg <| sprintf "007C %s" "resulting in None"
                                                       Seq.empty                                
                                ) 
                    
                        return 
                            try
                                let (|EmptySeq|_|) a =
                                    match Seq.isEmpty a with
                                    | true  -> Some () 
                                    | false -> None

                                let value = result () 
                                 
                                value
                                |> function
                                    | EmptySeq -> Error msg16                                        
                                    | _        -> Ok value

                            with ex -> Error <| string ex.Message  

                            |> function
                                | Ok value  ->
                                             value
                                | Error err ->
                                             logInfoMsg <| sprintf "Err006A %s" err 
                                             msg5 ()   
                                             closeItBaby msg16 
                                             Seq.empty     
                    }
        
        let addOn () =  
            [
                //pro pripad, kdyby KODIS strcil odkazy do uplne jinak strukturovaneho jsonu, tudiz by neslo pouzit dany type provider, anebo kdyz je vubec do jsonu neda (nize uvedene odkazy)
                //@"https://kodis-files.s3.eu-central-1.amazonaws.com/76_2023_10_09_2023_10_20_v_f2b77c8fad.pdf"
                @"https://kodis-files.s3.eu-central-1.amazonaws.com/46_A_2024_07_01_2024_09_01_faa5f15c1b.pdf"
                @"https://kodis-files.s3.eu-central-1.amazonaws.com/46_B_2024_07_01_2024_09_01_b5f542c755.pdf"
            ]
            |> List.toSeq      

        let taskAllJsonLists () = //TODO nekdy overit rychlost

            [
                async { return kodisAttachments pathToJsonList }
                async { return kodisTimetables pathToJsonList }
                async { return kodisTimetables2 pathToJsonList2 }
            ]         
            |> Async.Parallel 
            |> Async.Catch
            |> Async.RunSynchronously
            |> Result.ofChoice                      
            |> function
                | Ok value  ->
                             value |> Seq.ofArray |> Seq.concat  
                | Error err ->
                             logInfoMsg <| sprintf "Err214 %s" (string err.Message)
                             msg5 ()
                             Seq.empty       

        let taskJsonList2 () = 

             try 
                let task = kodisTimetables2 pathToJsonList2 
                (Seq.append <| task <| addOn()) |> Seq.distinct    

             with
             | ex ->  
                   logInfoMsg <| sprintf "Err214 %s" (string ex.Message)
                   msg5 ()
                   Seq.empty         

        //taskAllJsonLists ()
        taskJsonList2 ()  
    
    //input from seq -> change of input data -> output into datatable -> filtering data from datable -> links*paths     
    let private filterTimetables () dt param (pathToDir : string) diggingResult = 

        //*************************************Helpers for SQL columns********************************************

        let extractSubstring (input : string) =
            
            try
                let pattern = @"202[3-9]_[0-1][0-9]_[0-3][0-9]_202[4-9]_[0-1][0-9]_[0-3][0-9]"
                let regex = new Regex(pattern) 
                let matchResult = regex.Match(input)
        
                match matchResult.Success with
                | true  -> Ok input 
                | false -> Ok String.Empty 
            with ex -> Error <| string ex.Message                  
                  
            |> function
                | Ok value  -> 
                             value  
                | Error err ->
                             logInfoMsg <| sprintf "Err008 %s" err
                             msg9 ()
                             String.Empty    
        
        let extractSubstring1 (input : string) =

            try
                let pattern = @"202[3-9]_[0-1][0-9]_[0-3][0-9]_202[4-9]_[0-1][0-9]_[0-3][0-9]"
                let regex = new Regex(pattern) 
                let matchResult = regex.Match(input)
        
                match matchResult.Success with
                | true  -> Ok matchResult.Value
                | false -> Ok String.Empty
            with ex -> Error <| string ex.Message                 

            |> function
                | Ok value  -> 
                             value  
                | Error err ->
                             logInfoMsg <| sprintf "Err009 %s" err
                             msg9 ()
                             String.Empty     

        let extractSubstring2 (input : string) : (string option * int) =

            let prefix = "NAD_"
            
            match input.StartsWith(prefix) with
            | false -> 
                     (None, 0)
            | true  ->
                     let startIdx = prefix.Length
                     let restOfString = input.Substring(startIdx)

                     match restOfString.IndexOf('_') with
                     | -1             -> 
                                       (None, 0)
                     | idx 
                         when idx > 0 ->
                                       let result = restOfString.Substring(0, idx)
                                       (Some(result), result.Length)
                     | _              ->
                                       (None, 0)

        //zamerne nepouzivam jednotny kod pro NAD (extractSubstring2) a X - pro pripad, ze KODIS zase neco zmeni
        let extractSubstring3 (input : string) : (string option * int) =

            match input with            
            | _ when input.[0] = 'X' ->
                                      match input.IndexOf('_') with
                                      | index 
                                          when index > 1 -> 
                                                          let result = input.Substring(1, index - 1)
                                                          (Some(result), result.Length)
                                      | _                -> 
                                                          (None, 0)
            | _                      -> 
                                      (None, 0)       

        let extractStartDate (input : string) =

             let result = 
                 match input.Equals(String.Empty) with
                 | true  -> String.Empty
                 | _     -> input.[0..min 9 (input.Length - 1)] 
             result.Replace("_", "-")
         
        let extractEndDate (input : string) =

            let result = 
                match input.Equals(String.Empty) with
                | true  -> String.Empty
                | _     -> input.[max 0 (input.Length - 10)..]
            result.Replace("_", "-")

        let splitString (input : string) =   

            match input.StartsWith(pathKodisAmazonLink) with
            | true  -> [pathKodisAmazonLink; input.Substring(pathKodisAmazonLink.Length)]
            | false -> [pathKodisAmazonLink; input]

        //*************************************Splitting Kodis links into DataTable columns********************************************
        let splitKodisLink input =

            let oldPrefix = 
                try
                    Regex.Split(input, extractSubstring1 input) 
                    |> List.ofArray
                    |> List.item 0
                    |> splitString
                    |> List.item 1
                    |> Ok
                with ex -> Error <| string ex.Message
                     
                |> function
                    | Ok value  -> 
                                 value  
                    | Error err ->
                                 logInfoMsg <| sprintf "Err010 %s" err
                                 msg9 ()
                                 String.Empty      

            let totalDateInterval = extractSubstring1 input

            let partAfter =
                try
                    Regex.Split(input, totalDateInterval)
                    |> List.ofArray
                    |> List.item 1 
                    |> Ok
                with ex -> Error <| string ex.Message
                         
                |> function
                    | Ok value  -> 
                                 value  
                    | Error err ->
                                 logInfoMsg <| sprintf "Err011 %s" err
                                 msg9 ()
                                 String.Empty   
        
            let vIndex = partAfter.IndexOf "_v"
            let tIndex = partAfter.IndexOf "_t"

            let suffix = 
                match [vIndex; tIndex].Length = -2 with
                | false when vIndex <> -1 -> partAfter.Substring(0, vIndex + 2)
                | false when tIndex <> -1 -> partAfter.Substring(0, tIndex + 2)
                | _                       -> String.Empty
           
            let jsGeneratedString =
                match [vIndex; tIndex].Length = -2 with
                | false when vIndex <> -1 -> partAfter.Substring(vIndex + 2)
                | false when tIndex <> -1 -> partAfter.Substring(tIndex + 2)
                | _                       -> partAfter
        
            let newPrefix (oldPrefix : string) =

                let conditions =
                    [
                        fun () -> oldPrefix.Contains("AE") && oldPrefix.Length = 3
                        fun () -> oldPrefix.Contains("S") && oldPrefix.Length = 3
                        fun () -> oldPrefix.Contains("S") && oldPrefix.Length = 4
                        fun () -> oldPrefix.Contains("R") && oldPrefix.Length = 3
                        fun () -> oldPrefix.Contains("R") && oldPrefix.Length = 4
                        fun () -> oldPrefix.Contains("NAD")
                        fun () -> oldPrefix.Contains("X")
                    ]

                match List.filter (fun condition -> condition()) conditions with
                | [ _ ] -> 
                         let index = conditions |> List.findIndex (fun item -> item () = true) //neni treba tryFind, bo v [ _ ] je vzdy neco
                     
                         match index with
                         | 0  -> 
                               sprintf "_%s" oldPrefix
                         | 1  ->
                               sprintf "_%s" oldPrefix
                         | 2  ->
                               sprintf "%s" oldPrefix
                         | 3  ->
                               sprintf "_%s" oldPrefix
                         | 4  ->
                               sprintf "%s" oldPrefix
                         | 5  -> 
                               let newPrefix =                                 
                                   match oldPrefix |> extractSubstring2 with
                                   | (Some value, length)
                                         when length <= lineNumberLength -> sprintf "NAD%s%s_" <| createStringSeqFold(lineNumberLength - length, "0") <| value
                                   | _                                   -> oldPrefix                                 
                               oldPrefix.Replace(oldPrefix, newPrefix)                        
                         | 6  -> 
                               let newPrefix = //ponechat podobny kod jako vyse, nerobit refactoring, KODIS moze vse nekdy zmenit                                
                                   match oldPrefix |> extractSubstring3 with
                                   | (Some value, length)
                                         when length <= lineNumberLength -> sprintf "X%s%s_" <| createStringSeqFold(lineNumberLength - length, "0") <| value
                                   | _                                   -> oldPrefix                                 
                               oldPrefix.Replace(oldPrefix, newPrefix)
                         | _  ->
                               sprintf "%s" oldPrefix

                | _     ->
                         match oldPrefix.Length with                    
                         | 2  -> sprintf "%s%s" <| createStringSeqFold(2, "0") <| oldPrefix   //sprintf "00%s" oldPrefix
                         | 3  -> sprintf "%s%s" <| createStringSeqFold(1, "0") <| oldPrefix   //sprintf "0%s" oldPrefix                  
                         | _  -> oldPrefix
                          
            let input = 
                match input.Contains("_t") with 
                | true  -> input.Replace(pathKodisAmazonLink, sprintf"%s%s" pathKodisAmazonLink @"timetables/").Replace("_t.pdf", ".pdf") 
                | false -> input   
                
            let fileToBeSaved = sprintf "%s%s%s.pdf" (newPrefix oldPrefix) totalDateInterval suffix

            {
                OldPrefix = OldPrefix oldPrefix
                NewPrefix = NewPrefix (newPrefix oldPrefix)
                StartDate = StartDateDtOpt (TryParserDate.parseDate () <| extractStartDate totalDateInterval)
                EndDate = EndDateDtOpt (TryParserDate.parseDate () <| extractEndDate totalDateInterval)
                TotalDateInterval = TotalDateInterval totalDateInterval
                Suffix = Suffix suffix
                JsGeneratedString = JsGeneratedString jsGeneratedString
                CompleteLink = CompleteLink input
                FileToBeSaved = FileToBeSaved fileToBeSaved
                PartialLink = 
                    let pattern = Regex.Escape(jsGeneratedString)
                    PartialLink <| Regex.Replace(input, pattern, String.Empty)
            }
            |> dtDataTransformLayerSend  

     
        //**********************Filtering and datatable data inserting********************************************************
        let dataToBeInserted = 
            
            diggingResult    
            |> Array.ofSeq
            |> Array.Parallel.map 
                (fun item -> 
                           let item = extractSubstring item      //"https://kodis-files.s3.eu-central-1.amazonaws.com/timetables/2_2023_03_13_2023_12_09.pdf                 
                           
                           match item.Contains @"timetables/" with
                           | true  -> item.Replace("timetables/", String.Empty).Replace(".pdf", "_t.pdf")
                           | false -> item                                       
                )  
            |> Array.sort //jen quli testovani
            |> Array.filter
                (fun item -> 
                           let cond1 = (item |> Option.ofNullEmptySpace).IsSome
                           let cond2 = item |> Option.ofNullEmpty |> Option.toBool //for learning purposes - compare with (not String.IsNullOrEmpty(item))
                           cond1 && cond2 
                )         
            |> Array.map (fun item -> splitKodisLink item) 
            |> Array.toList

        
        //**********************Cesty pro soubory pro aktualni a dlouhodobe platne a pro ostatni********************************************************
        let createPathsForDownloadedFiles filteredList =
            
            filteredList
            |> List.map 
                (fun item -> fst item |> function CompleteLink value -> value, snd item |> function FileToBeSaved value -> value)
            |> List.map
                (fun (link, file) 
                    -> 
                     let path =                                         
                         let (|IntType|StringType|OtherType|) (param : 'a) = //zatim nevyuzito, mozna -> TODO podumat nad refactoringem nize uvedeneho 
                             match param.GetType() with
                             | typ when typ = typeof<int>    -> IntType   
                             | typ when typ = typeof<string> -> StringType  
                             | _                             -> OtherType                                                      
                                                
                         let pathToDir = sprintf "%s\\%s" pathToDir file //pro ostatni
                         
                         // v pripade opakovani situace s A, B zrobit dalsi logiku 
                         match pathToDir.Contains("JR_ODIS_aktualni_vcetne_vyluk") || pathToDir.Contains("JR_ODIS_teoreticky_dlouhodobe_platne_bez_vyluk") with 
                         | true ->   
                                 true //pro aktualni a dlouhodobe platne
                                 |> function
                                     | true when file.Substring(0, 1) = "0"    -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 0 sortedLines)
                                     | true when file.Substring(0, 1) = "1"    -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 0 sortedLines)
                                     | true when file.Substring(0, 1) = "2"    -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 1 sortedLines)
                                     | true when file.Substring(0, 1) = "3"    -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 2 sortedLines)
                                     | true 
                                         when 
                                             (file.Substring(0, 1) = "4" && not <| file.Contains("46_A") && not <| file.Contains("46_B"))
                                                                               -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 3 sortedLines)
                                     | true when file.Substring(0, 1) = "5"    -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 4 sortedLines)
                                     | true when file.Substring(0, 1) = "6"    -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 5 sortedLines)
                                     | true when file.Substring(0, 1) = "7"    -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 6 sortedLines)
                                     | true when file.Substring(0, 1) = "8"    -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 7 sortedLines)
                                     | true when file.Substring(0, 1) = "9"    -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 8 sortedLines)
                                     | true when file.Substring(0, 1) = "S"    -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 9 sortedLines)
                                     | true when file.Substring(0, 1) = "R"    -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 10 sortedLines)
                                     | true when file.Substring(0, 2) = "_S"   -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 9 sortedLines)
                                     | true when file.Substring(0, 2) = "_R"   -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 10 sortedLines) 
                                     | true when file.Substring(0, 4) = "46_A" -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 11 sortedLines)
                                     | true when file.Substring(0, 4) = "46_B" -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 11 sortedLines)
                                     | _                                       -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 11 sortedLines)                                                           
                         | _    -> 
                                 pathToDir                               
                     link, path 
                )   

        match param with 
        | CurrentValidity           -> DataTable.InsertSelectSort.sortLinksOut dt dataToBeInserted CurrentValidity |> createPathsForDownloadedFiles
        | FutureValidity            -> DataTable.InsertSelectSort.sortLinksOut dt dataToBeInserted FutureValidity |> createPathsForDownloadedFiles
        // | ReplacementService     -> DataTable.InsertSelectSort.sortLinksOut dt dataToBeInserted ReplacementService |> createPathsForDownloadedFiles 
        | WithoutReplacementService -> DataTable.InsertSelectSort.sortLinksOut dt dataToBeInserted WithoutReplacementService |> createPathsForDownloadedFiles          
     
    //IO operations made separate in order to have some structure in the free-monad-based design (for educational purposes)   
    let internal deleteAllODISDirectories pathToDir = 

        let deleteIt : Reader<string list, unit> = 
    
            reader //Reader monad for educational purposes only, no real benefit here  
                {
                    let! getDefaultRecordValues = fun env -> env 
                    
                    return 
                        try
                            //rozdil mezi Directory a DirectoryInfo viz Unique_Identifier_And_Metadata_File_Creator.sln -> MainLogicDG.fs
                            let dirInfo = new DirectoryInfo(pathToDir)                                                   
                                in
                                dirInfo.EnumerateDirectories() 
                                |> Seq.filter (fun item -> getDefaultRecordValues |> List.contains item.Name) //prunik dvou kolekci (plus jeste Seq.distinct pro unique items)
                                |> Seq.distinct 
                                |> Seq.iter _.Delete(true)  
                                |> Ok
                                //smazeme pouze adresare obsahujici stare JR, ostatni ponechame              
                        with ex -> Error <| string ex.Message
                        
                        |> function
                            | Ok value  -> 
                                         value  
                            | Error err ->
                                         logInfoMsg <| sprintf "Err012 %s" err
                                         closeItBaby msg16 
                }

        deleteIt listODISDefault4 
 
    //Operations on data made separate in order to have some structure in the free-monad-based design (for educational purposes)   
    let internal createNewDirectoryPaths pathToDir : Reader<string list, string list> =
        
        reader
            { 
                let! getDefaultRecordValues = //Reader monad for educational purposes only, no real benefit here
                    fun env -> env in return getDefaultRecordValues |> List.map (fun item -> sprintf"%s\%s"pathToDir item) 
            } 

    //Operations on data made separate in order to have some structure in the free-monad-based design (for educational purposes)   
    let internal createDirName variant : Reader<string list, string> = 

        reader
            {
                let! getDefaultRecordValues = fun env -> env //Reader monad for educational purposes only, no real benefit here

                return 
                    match variant with 
                    | CurrentValidity           -> getDefaultRecordValues |> List.item 0
                    | FutureValidity            -> getDefaultRecordValues |> List.item 1
                    // | ReplacementService     -> getDefaultRecordValues |> List.item 2                                
                    | WithoutReplacementService -> getDefaultRecordValues |> List.item 2
            } 

    //IO operations made separate in order to have some structure in the free-monad-based design (for educational purposes)   
    let internal deleteOneODISDirectory variant pathToDir =

        //smazeme pouze jeden adresar obsahujici stare JR, ostatni ponechame

        let deleteIt : Reader<string list, unit> =  

            reader //Reader monad for educational purposes only, no real benefit here  
                {   
                    let! getDefaultRecordValues = fun env -> env
                                                          
                    return 
                        try
                            //rozdil mezi Directory a DirectoryInfo viz Unique_Identifier_And_Metadata_File_Creator.sln -> MainLogicDG.fs
                            let dirInfo = new DirectoryInfo(pathToDir)        
                                in
                                dirInfo.EnumerateDirectories()
                                |> Seq.filter (fun item -> item.Name = createDirName variant getDefaultRecordValues) 
                                |> Seq.iter _.Delete(true) //trochu je to hack, ale nemusim se zabyvat tryHead, bo moze byt empty kolekce  
                                |> Ok                                             
                        with ex -> Error <| string ex.Message
                        
                        |> function
                            | Ok value  -> 
                                         value  
                            | Error err ->
                                         logInfoMsg <| sprintf "Err012B %s" err
                                         closeItBaby msg16  
                }

        deleteIt listODISDefault4         
 
    //list -> aby bylo mozno pouzit funkci createFolders bez uprav
    //Operations on data made separate in order to have some structure in the free-monad-based design (for educational purposes)     
    let internal createOneNewDirectoryPath pathToDir dirName = [ sprintf"%s\%s"pathToDir dirName ] 
  
    //IO operations made separate in order to have some structure in the free-monad-based design (for educational purposes)    
    let internal createFolders dirList =  
        try
            dirList
            |> List.iter
                (fun (dir: string) 
                    ->                
                     match dir.Contains("JR_ODIS_aktualni_vcetne_vyluk") || dir.Contains("JR_ODIS_teoreticky_dlouhodobe_platne_bez_vyluk") with 
                     | true ->    
                             sortedLines 
                             |> List.iter
                                 (fun item -> 
                                            let dir = dir.Replace("_vyluk", sprintf "%s\\%s" "_vyluk" item)
                                            Directory.CreateDirectory(dir) |> ignore
                                 )           
                     | _    -> 
                             Directory.CreateDirectory(sprintf "%s" dir) |> ignore           
                ) |> Ok
        with ex -> Error <| string ex.Message
        
        |> function
            | Ok value  -> 
                         value  
            | Error err ->
                         logInfoMsg <| sprintf "Err013 %s" err
                         closeItBaby msg16   
    
    //input from data filtering (links * paths) -> http request -> IO operation -> saving pdf data files on HD    
    let private downloadAndSaveTimetables =      //FsHttp         
        
        cts.Cancel()  

        reader
            {
                let! context = fun env -> env 

                msgParam3 context.dir

                let l = context.list |> List.length

                let counterAndProgressBar =
                    MailboxProcessor.Start <|
                        fun inbox 
                            ->
                             let rec loop n =
                                 async
                                     { 
                                         match! inbox.Receive() with
                                         | Inc i -> progressBarContinuous n l; return! loop (n + i)
                                     }
                             loop 0
                                                 
                return                  
                    context.list
                    |> List.unzip             
                    ||> context.listMappingFunction
                        (fun uri (pathToFile: string) 
                            ->                         
                             async
                                 {    
                                     match not <| NetworkInterface.GetIsNetworkAvailable() with                                     
                                     | true  ->                                    
                                              processorPdf.Post(Incr 1)   
                                     | false ->  
                                              counterAndProgressBar.Post(Inc 1)

                                              let get uri =
                                                  http 
                                                      {
                                                          config_timeoutInSeconds 120  //for educational purposes
                                                          GET(uri) 
                                                      }    
                                              
                                              use! response = (>>) get Request.sendAsync <| uri  
                                                
                                              match response.statusCode with
                                              | HttpStatusCode.OK 
                                                  -> 
                                                   let pathToFileExist =  
                                                       pyramidOfDoom
                                                           {
                                                               let filepath = Path.GetFullPath(pathToFile) |> Option.ofNullEmpty 
                                                               let! filepath = filepath, None

                                                               let fInfodat: FileInfo = new FileInfo(filepath)
                                                               let! _ = not fInfodat.Exists |> Option.ofBool, None   
                                                                               
                                                               return Some ()
                                                           } 
                                                                           
                                                   match pathToFileExist with
                                                   | Some _ -> return! (>>) response.SaveFileAsync Async.AwaitTask <| pathToFile      //Original FsHttp library function    
                                                   | None   -> return ()  //nechame chybu tise projit  
                                                                                                                                                                
                                              | _                
                                                  -> 
                                                   return ()      //nechame chybu tise projit                                                                                                                                         
                                 } 
                             |> Async.Catch
                             |> Async.RunSynchronously  
                             |> Result.ofChoice                      
                             |> function
                                 | Ok _      ->    
                                              ()
                                 | Error err ->
                                              logInfoMsg <| sprintf "Err014 %s" (string err.Message)
                                              msgParam2 uri  //nechame chybu projit v loop => nebude Result.sequence
                        )  
                    |> List.head  
                    
            } 
        
     
    let internal operationOnDataFromJson () dt variant dir =   

        //operation on data
        //input from saved json files -> change of input data -> output into seq >> input from seq -> change of input data -> output into datatable -> data filtering (links*paths)  
        
        try digThroughJsonStructure >> filterTimetables () dt variant dir <| () |> Ok
        with ex -> Error <| string ex.Message
        
        |> function
            | Ok value  -> 
                         value  
            | Error err ->
                         logInfoMsg <| sprintf "Err018 %s" err
                         closeItBaby msg16  
                         []
                           
    let internal downloadAndSave = 

         reader
            {    
                let! context = fun env -> env
                
                return
                    match context.dir |> Directory.Exists with 
                    | false ->
                             msgParam5 context.dir 
                             msg13 ()    
                             logInfoMsg <| sprintf "Err019A, directory %s does not exist" context.dir
                    | true  ->
                             try
                                 //input from data filtering (links * paths) -> http request -> saving pdf files on HD
                                 match context.list with
                                 | [] ->
                                       msgParam13 context.dir       
                                 | _  ->
                                       downloadAndSaveTimetables context  
                                       msgParam4 context.dir  
                             with
                             | ex -> 
                                   logInfoMsg <| sprintf "Err019 %s" (string ex.Message)
                                   closeItBaby msg16   
            }               