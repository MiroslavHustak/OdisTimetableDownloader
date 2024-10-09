namespace SubmainFunctions3

open System
open System.IO
open System.Net
open System.Threading
open System.Threading.Tasks
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


module KODIS_SubmainRecord3 =    
        
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!     
    // DO NOT DIVIDE this module into parts in line with the main design yet - KODIS keeps making unpredictable changes or amendments
    // LEAVE THE COMMENTED CODE AS IT IS !!! DO NOT DELETE IT !!! IT IS THERE FOR A REASON.


    //*************************Helpers************************************************************

    let private readAllText pathToJson = 

        pyramidOfDoom
            {
                let filepath = Path.GetFullPath pathToJson //pathToJson pod kontrolou, filepath nebude null
                                                
                let fInfoDat = FileInfo pathToJson
                let! _ = fInfoDat.Exists |> Option.ofBool, String.Empty

                return File.ReadAllText pathToJson //pathToJson pod kontrolou, fs nebude null                                            
            }    

    let private readAllTextAsync pathToJson : Async<string> = 

        pyramidOfDoom
            {
                let filepath = Path.GetFullPath pathToJson //pathToJson pod kontrolou, filepath nebude null
                                                
                let fInfoDat = FileInfo pathToJson
                let! _ = fInfoDat.Exists |> Option.ofBool, async { return String.Empty }

                return File.ReadAllTextAsync pathToJson |> Async.AwaitTask //pathToJson pod kontrolou, fs nebude null                                            
            }    

    let private tempJson1, tempJson2 = 
        [
            readAllTextAsync pathkodisMHDTotal
            readAllTextAsync pathkodisMHDTotal2_0
        ]         
        |> Async.Parallel 
        |> Async.Catch
        |> Async.RunSynchronously
        |> Result.ofChoice                      
        |> function
            | Ok value  ->
                         let list = value |> List.ofArray 
                           
                         match List.tryItem 0 list, List.tryItem 1 list with
                         | Some a, Some b -> 
                                           a, b
                         | _              -> 
                                           sprintf "Err999 %s" >> logInfoMsg <| msg5A
                                           closeItBaby msg5A
                                           String.Empty, String.Empty                                
            | Error exn -> 
                         sprintf "Err999A %s" >> logInfoMsg <| (string exn.Message)
                         closeItBaby msg5A
                         String.Empty, String.Empty 
    
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
                            do! Async.Sleep 3000                            
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
                           let rec loop n = 
                               async { match! inbox.Receive() with Inc i -> progressBarContinuous n l; return! loop (n + i) }
                           loop 0

        let result = 
            (jsonLinkList, pathToJsonList)            
            ||> List.Parallel.map2 
                (fun (uri: string) path
                    ->                       
                     async
                         {    
                             use! response = get >> Request.sendAsync <| uri 

                             match response.statusCode with
                             | HttpStatusCode.OK
                                 ->                                                                                                   
                                  counterAndProgressBar.Post(Inc 1)                                                   
                                  do! response.SaveFileAsync >> Async.AwaitTask <| path                                                   
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
                    sprintf "Err001 %s" >> logInfoMsg <| err
                    closeItBaby msg5A

                let errorFn2 (err : exn) = 
                    sprintf "Err002 %s" >> logInfoMsg <| (string err.Message)
                    closeItBaby msg5A

                let! value = result |> Result.sequence, errorFn2 
                let! value = value |> Result.sequence, errorFn1

                return 
                    value 
                    |> List.tryHead 
                    |> function Some value -> value | None -> ()
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
                                 try
                                     let json = readAllText pathToJson                                        
                                     JsonProvider1.Parse json 
                                 with 
                                 | _ -> JsonProvider1.Parse tempJson1 
                                 
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

                        with 
                        | ex -> Error <| string ex.Message  

                        |> function
                            | Ok value  -> 
                                         value
                            | Error err -> 
                                         sprintf "Err004A %s" >> logInfoMsg <| err 
                                         closeItBaby msg16 
                                         Seq.empty      
                }
            
        let kodisTimetables3 : Reader<string list, string seq> = 

            reader //Reader monad for educational purposes only, no real benefit here  
                {
                    let! pathToJsonList3 = fun env -> env 
                    
                    let result () = 
                        
                        pathToJsonList3 
                        |> Seq.ofList 
                        |> Seq.collect 
                            (fun pathToJson 
                                ->     
                                 let kodisJsonSamples =                                     
                                     try   
                                         let json = readAllText pathToJson //tady nelze Result.sequence 
                                         JsonProvider2.Parse json
                                     with 
                                     | _ -> JsonProvider2.Parse tempJson2 
                                 
                                 let timetables = 
                                     kodisJsonSamples
                                     |> Option.ofNull
                                     |> function 
                                         | Some value -> 
                                                       value.Data
                                                       |> Seq.map _.Timetable  //nejde Some, nejde Ok
                                         | None       -> 
                                                       Seq.empty  
                                 
                                 let vyluky = 
                                     kodisJsonSamples
                                     |> Option.ofNull
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
                                                       |> List.ofSeq
                                                       |> List.Parallel.map (fun item -> item.Url |> Option.ofNullEmptySpace)                                
                                                       |> List.choose id //co neprojde, to beze slova ignoruju
                                                       |> List.toSeq
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

                        with 
                        | ex -> Error <| string ex.Message  

                        |> function
                            | Ok value  -> 
                                         value
                            | Error err ->
                                         sprintf "Err004 %s" >> logInfoMsg <| err 
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
                                    |> List.ofSeq
                                    |> List.Parallel.map (fun item -> item.Url |> Option.ofNullEmptySpace) //jj, funguje to :-)                                    
                                    |> List.choose id //co neprojde, to beze slova ignoruju
                                    |> List.toSeq

                                let fn2 (item : JsonProvider1.Vyluky) =    
                                    item.Attachments 
                                    |> Option.ofNull        
                                    |> function 
                                        | Some value ->
                                                      value |> fn1
                                        | None       -> 
                                                      msg5 () 
                                                      sprintf "007A %s" >> logInfoMsg <| "resulting in None"
                                                      Seq.empty                

                                let fn3 (item : JsonProvider1.Root) =  
                                    item.Vyluky
                                    |> Option.ofNull  
                                    |> function 
                                        | Some value ->
                                                      value |> Seq.collect fn2 
                                        | None       ->
                                                      msg5 () 
                                                      sprintf "007B %s" >> logInfoMsg <| "resulting in None"
                                                      Seq.empty                                      
                                                          
                                let kodisJsonSamples = 
                                    try
                                        let json = readAllText pathToJson //tady nelze Result.sequence 
                                        JsonProvider1.Parse json 
                                    with
                                    | _ -> JsonProvider1.Parse tempJson1                                    
                                                          
                                kodisJsonSamples 
                                |> Option.ofNull  
                                |> function 
                                    | Some value -> 
                                                  value |> Seq.collect fn3 
                                    | None       -> 
                                                  msg5 () 
                                                  sprintf "007C %s" >> logInfoMsg <| "resulting in None"
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

                        with 
                        | ex -> Error <| string ex.Message  

                        |> function
                            | Ok value  ->
                                         value
                            | Error err ->
                                         sprintf "Err006A %s" >> logInfoMsg <| err 
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

        let taskAllJsonListsParallel () = 
            [
                async { return kodisAttachments pathToJsonList }
                async { return kodisTimetables pathToJsonList }
                async { return kodisTimetables3 pathToJsonList3 }
            ]         
            |> Async.Parallel 
            |> Async.Catch
            |> Async.RunSynchronously
            |> Result.ofChoice                      
            |> function
                | Ok value  ->
                             let task = value |> Seq.ofArray |> Seq.concat 
                             (Seq.append <| task <| addOn()) |> Seq.distinct
                | Error err ->
                             sprintf "Err214 %s" >> logInfoMsg <| (string err.Message)
                             msg5 ()
                             Seq.empty       
        
        let taskAllJsonLists () = 
            try 
                let task1 = kodisAttachments pathToJsonList 
                let task2 = kodisTimetables pathToJsonList 
                let task3 = kodisTimetables3 pathToJsonList3 
                   
                let task = Seq.append <| task1 <| task2
                let task = Seq.append <| task <| task3                         
               
                (Seq.append <| task <| addOn()) |> Seq.distinct |> Ok                     
            with
            | ex -> Error (string ex.Message)
                                  
            |> function
                | Ok value  ->                             
                             value
                | Error err ->
                             sprintf "Err214 %s" >> logInfoMsg <| err
                             msg5 ()
                             Seq.empty      

        let taskAllJsonLists2 () = 
            task
                {
                    try 
                        let task1 = Task.Run(fun () -> kodisAttachments pathToJsonList)
                        let task2 = Task.Run(fun () -> kodisTimetables pathToJsonList)
                        let task3 = Task.Run(fun () -> kodisTimetables3 pathToJsonList3)
        
                        let! results = Task.WhenAll [| task1; task2; task3 |]
        
                        let combinedTask = 
                            results 
                            |> Seq.concat  // Concatenate the results
                            |> Seq.append (addOn())  // Append the add-on task
                            |> Seq.distinct
        
                        return combinedTask
                    with
                    | ex -> 
                          sprintf "Err214 %s" >> logInfoMsg <| ex.Message
                          msg5 ()
                          return Seq.empty
                }
        
        //je to jen 1-2 vteriny, rychlost stejna       
        //taskAllJsonLists () 
        //taskAllJsonLists2 ()
        taskAllJsonListsParallel ()  
    
    //input from seq -> change of input data -> output into datatable -> filtering data from datable -> links*paths     
    let private filterTimetables () param (pathToDir : string) diggingResult = 

        //*************************************Helpers for SQL columns********************************************

        let extractSubstring (input : string) =
            
            try
                let pattern = @"202[3-9]_[0-1][0-9]_[0-3][0-9]_202[4-9]_[0-1][0-9]_[0-3][0-9]"
                let regex = Regex pattern 
                let matchResult = regex.Match input
        
                match matchResult.Success with
                | true  -> Ok input 
                | false -> Ok String.Empty 
            with
            | ex -> Error <| string ex.Message                  
                  
            |> function
                | Ok value  -> 
                             value  
                | Error err ->
                             sprintf "Err008 %s" >> logInfoMsg <| err
                             msg9 ()
                             String.Empty    
        
        let extractSubstring1 (input : string) =

            try
                let pattern = @"202[3-9]_[0-1][0-9]_[0-3][0-9]_202[4-9]_[0-1][0-9]_[0-3][0-9]"
                let regex = Regex pattern 
                let matchResult = regex.Match input
        
                match matchResult.Success with
                | true  -> Ok matchResult.Value
                | false -> Ok String.Empty
            with 
            | ex -> Error <| string ex.Message                 

            |> function
                | Ok value  -> 
                             value  
                | Error err ->
                             sprintf "Err009 %s" >> logInfoMsg <| err
                             msg9 ()
                             String.Empty     

        let extractSubstring2 (input : string) : (string option * int) =

            let prefix = "NAD_"
            
            match input.StartsWith prefix with
            | false -> 
                     (None, 0)
            | true  ->
                     let startIdx = prefix.Length
                     let restOfString = input.Substring startIdx

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
                 | _     -> input.[ 0..min 9 (input.Length - 1) ] 
             result.Replace("_", "-")
         
        let extractEndDate (input : string) =

            let result = 
                match input.Equals String.Empty with
                | true  -> String.Empty
                | _     -> input.[ max 0 (input.Length - 10).. ]
            result.Replace("_", "-")

        let splitString (input : string) =   

            match input.StartsWith pathKodisAmazonLink with
            | true  -> [ pathKodisAmazonLink; input.Substring pathKodisAmazonLink.Length ]
            | false -> [ pathKodisAmazonLink; input ]

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
                with 
                | ex -> Error <| string ex.Message
                     
                |> function
                    | Ok value  -> 
                                 value  
                    | Error err ->
                                 sprintf "Err010 %s" >> logInfoMsg <| err
                                 msg9 ()
                                 String.Empty      

            let totalDateInterval = extractSubstring1 input

            let partAfter =
                try
                    Regex.Split(input, totalDateInterval)
                    |> List.ofArray
                    |> List.item 1 
                    |> Ok
                with
                | ex -> Error <| string ex.Message
                         
                |> function
                    | Ok value  -> 
                                 value  
                    | Error err ->
                                 sprintf "Err011 %s" >> logInfoMsg <| err
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
                OldPrefixRc = OldPrefix oldPrefix
                NewPrefixRc = NewPrefix (newPrefix oldPrefix)
                StartDateRc = StartDateRcOpt (TryParserDate.parseDate () <| extractStartDate totalDateInterval)
                EndDateRc = EndDateRcOpt (TryParserDate.parseDate () <| extractEndDate totalDateInterval)
                TotalDateIntervalRc = TotalDateInterval totalDateInterval
                SuffixRc = Suffix suffix
                JsGeneratedStringRc = JsGeneratedString jsGeneratedString
                CompleteLinkRc = CompleteLink input
                FileToBeSavedRc = FileToBeSaved fileToBeSaved
                PartialLinkRc = 
                    let pattern = Regex.Escape(jsGeneratedString)
                    PartialLink <| Regex.Replace(input, pattern, String.Empty)
            }
     
        //**********************Filtering and datatable data inserting********************************************************
        let dataToBeFiltered = 
            //stejna doba procesu pro vsechny varianty (List, Array, parallel)
            diggingResult    
            |> List.ofSeq
            |> List.Parallel.map 
                (fun item -> 
                           let item = extractSubstring item      //"https://kodis-files.s3.eu-central-1.amazonaws.com/timetables/2_2023_03_13_2023_12_09.pdf                 
                           
                           match item.Contains @"timetables/" with
                           | true  -> item.Replace("timetables/", String.Empty).Replace(".pdf", "_t.pdf")
                           | false -> item                                       
                )  
            |> List.sort //jen quli testovani
            |> List.filter
                (fun item -> 
                           let cond1 = (item |> Option.ofNullEmptySpace).IsSome
                           let cond2 = item |> Option.ofNullEmpty |> Option.toBool //for learning purposes - compare with (not String.IsNullOrEmpty(item))
                           cond1 && cond2 
                )         
            |> List.Parallel.map (fun item -> splitKodisLink item) 
        
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
        | CurrentValidity           -> Records.SortRecordData.sortLinksOut dataToBeFiltered CurrentValidity |> createPathsForDownloadedFiles 
        | FutureValidity            -> Records.SortRecordData.sortLinksOut dataToBeFiltered FutureValidity |> createPathsForDownloadedFiles 
        | WithoutReplacementService -> Records.SortRecordData.sortLinksOut dataToBeFiltered WithoutReplacementService |> createPathsForDownloadedFiles 
     
    //IO operations made separate in order to have some structure in the free-monad-based design (for educational purposes)   
    let internal deleteAllODISDirectories pathToDir = 

        let deleteIt : Reader<string list, unit> = 
    
            reader //Reader monad for educational purposes only, no real benefit here  
                {
                    let! getDefaultRecordValues = fun env -> env 
                    
                    return 
                        try
                            //rozdil mezi Directory a DirectoryInfo viz Unique_Identifier_And_Metadata_File_Creator.sln -> MainLogicDG.fs
                            let dirInfo = DirectoryInfo pathToDir                                                   
                                in
                                dirInfo.EnumerateDirectories() 
                                |> Seq.filter (fun item -> getDefaultRecordValues |> List.contains item.Name) //prunik dvou kolekci (plus jeste Seq.distinct pro unique items)
                                |> Seq.distinct 
                                |> Seq.iter _.Delete(true)  
                                |> Ok
                                //smazeme pouze adresare obsahujici stare JR, ostatni ponechame              
                        with 
                        | ex -> Error <| string ex.Message
                        
                        |> function
                            | Ok value  -> 
                                         value  
                            | Error err ->
                                         sprintf "Err012 %s" >> logInfoMsg <| err
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
                            let dirInfo = DirectoryInfo pathToDir        
                                in
                                dirInfo.EnumerateDirectories()
                                |> Seq.filter (fun item -> item.Name = createDirName variant getDefaultRecordValues) 
                                |> Seq.iter _.Delete(true) //trochu je to hack, ale nemusim se zabyvat tryHead, bo moze byt empty kolekce  
                                |> Ok                                             
                        with
                        | ex -> Error <| string ex.Message
                        
                        |> function
                            | Ok value  -> 
                                         value  
                            | Error err ->
                                         sprintf "Err012B %s" >> logInfoMsg <| err
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
                                            Directory.CreateDirectory dir |> ignore
                                 )           
                     | _    -> 
                             Directory.CreateDirectory (sprintf "%s" dir) |> ignore           
                )
                |> Ok
        with 
        | ex -> Error <| string ex.Message
        
        |> function
            | Ok value  -> 
                         value  
            | Error err ->
                         sprintf "Err013 %s" >> logInfoMsg <| err
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
                                 async { match! inbox.Receive() with Inc i -> progressBarContinuous n l; return! loop (n + i) }
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
                                              
                                              use! response = get >> Request.sendAsync <| uri  
                                                
                                              match response.statusCode with
                                              | HttpStatusCode.OK 
                                                  -> 
                                                   let pathToFileExist =  
                                                       pyramidOfDoom
                                                           {
                                                               let filepath = Path.GetFullPath pathToFile |> Option.ofNullEmpty 
                                                               let! filepath = filepath, None

                                                               let fInfodat: FileInfo = FileInfo filepath
                                                               let! _ = not fInfodat.Exists |> Option.ofBool, None   
                                                                               
                                                               return Some ()
                                                           } 
                                                                           
                                                   match pathToFileExist with
                                                   | Some _ -> return! response.SaveFileAsync >> Async.AwaitTask <| pathToFile      //Original FsHttp library function    
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
                                              sprintf "Err014 %s" >> logInfoMsg <| (string err.Message)
                                              msgParam2 uri  //nechame chybu projit v loop => nebude Result.sequence
                        )  
                    |> List.tryHead  
                    |> function Some value -> value | None -> ()
            } 
             
    let internal operationOnDataFromJson () variant dir =   

        //operation on data
        //input from saved json files -> change of input data -> output into seq >> input from seq -> change of input data -> output into datatable -> data filtering (links*paths)          
        try 
            digThroughJsonStructure >> filterTimetables () variant dir <| () |> Ok
        with
        | ex -> Error <| string ex.Message   
        
        |> function
            | Ok value  -> 
                         value  
            | Error err ->
                         sprintf "Err018 %s" >> logInfoMsg <| err
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
                              sprintf "Err019A, directory %s does not exist" >> logInfoMsg <| context.dir
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
                                    sprintf "Err019 %s" >> logInfoMsg <| (string ex.Message)
                                    closeItBaby msg16   
             }               