namespace SubmainFunctions

open System
open System.IO
open System.Net
open System.Threading
open System.Net.NetworkInformation
open System.Text.RegularExpressions

open FsHttp
open FSharp.Data
open FSharp.Control
open FsToolkit.ErrorHandling
open Microsoft.FSharp.Quotations
open FSharp.Quotations.Evaluator.QuotationEvaluationExtensions

open Types

open EmbeddedTP.EmbeddedTP

open Logging.Logging

open Settings.Messages
open Settings.SettingsKODIS
open Settings.SettingsGeneral

open Helpers
open Helpers.MyString
open Helpers.Builders
open Helpers.CloseApp    
open Helpers.ConsoleFixers
open Helpers.MsgBoxClosing
open Helpers.ProgressBarFSharp  
open Helpers.CollectionSplitting

open Database.Select
open Database.InsertInto
open Database.Connection

open DataModelling.DataModel
open TransformationLayers.TransformationLayerSend

//*************************************************************************************
//Are you reviewing my SQL skills? If so, you are at the right place :-).
//If not, please direct your attention to code contained in KODIS-Submain_DataTable.fs.
//*************************************************************************************

module KODIS_Submain =
    
    //DO NOT DIVIDE this module into parts in line with the main design pattern yet - KODIS keeps making unpredictable changes or amendments

    type internal KodisTimetables = JsonProvider1 

    //*************************Helpers************************************************************

    //space for helpers

    let inline private expr (param : 'a) = Expr.Value(param)  
    
    //********************* Infinite checking for net conn during Json files download ******************************
    
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
    let internal downloadAndSaveJson () = //FsHttp
               
        let l = jsonLinkList |> List.length

        let counterAndProgressBar =
            MailboxProcessor.Start
                (fun inbox 
                    ->
                     let rec loop n =
                         async
                             { 
                                 match! inbox.Receive()  with
                                 | Incr i             ->
                                                       progressBarContinuous n l                                                        
                                                       return! loop (n + i)
                                 | Fetch replyChannel ->
                                                       replyChannel.Reply n 
                                                       return! loop n
                             }
                     loop 0
                )

        //a redundant function, just messing about.. 
        let updateJson1 (jsonLinkList1 : string list) (pathToJsonList1 : string list) =     
            
            Console.Write("\r" + new string(' ', (-) Console.WindowWidth 1) + "\r")
            Console.CursorLeft <- 0 
               
            (jsonLinkList1, pathToJsonList1)
            ||> List.Parallel.iter2   //just messing about...
                (fun (uri: string) path
                    ->
                     use response = get >> Request.send <| uri 
                     response.SaveFile <| path         
                )  
       
        let updateJson listTuple =    
        
            Console.Write("\r" + new string(' ', (-) Console.WindowWidth 1) + "\r")
            Console.CursorLeft <- 0   
            
            let (jsonLinkList1, pathToJsonList1) = listTuple     

            let result = 
                (jsonLinkList1, pathToJsonList1)
                ||> List.map2
                     (fun (uri: string) path
                         ->                       
                          async
                              {    
                                  //failwith "Simulated exception"  
                                  use! response = get >> Request.sendAsync <| uri 

                                  match response.statusCode with
                                  | HttpStatusCode.OK
                                      ->                                                                                                   
                                       counterAndProgressBar.Post(Incr 1)                                                   
                                       do! response.SaveFileAsync >> Async.AwaitTask <| path                                                   
                                       return Ok ()                                
                                  | _ -> 
                                       return Error "HttpStatusCode not OK"      
                              }                         
                          |> Async.Catch 
                          |> Async.RunSynchronously
                          |> Result.ofChoice                                  
                     ) 
                |> Result.sequence              
               
            pyramidOfInferno
                {
                    let errorFn1 err = 
                        logInfoMsg <| sprintf "Err001A %s" err
                        closeItBaby msg5A

                    let errorFn2 (err : exn) = 
                        logInfoMsg <| sprintf "Err002A %s" (string err.Message)
                        closeItBaby msg5A

                    let! value = result, errorFn2 
                    let! value = value |> List.head, errorFn1

                    return value
                }             
                                                 
        msg2 ()      
        
        let fSharpAsyncParallel () =  

            msg15 ()

            let numberOfThreads1 = numberOfThreads () l

            let myList =       
                (splitListIntoEqualParts numberOfThreads1 jsonLinkList, splitListIntoEqualParts numberOfThreads1 pathToJsonList)
                ||> List.zip                 
                        
            fun i -> <@ async { return updateJson (%%expr myList |> List.item %%(expr i)) } @>
            |> List.init numberOfThreads1 
            |> List.map _.Compile()       
            |> Async.Parallel 
            |> Async.Catch //zachytilo failwith "Simulated exception"
            |> Async.RunSynchronously
            |> Result.ofChoice           
            |> function
                | Ok _      ->                             
                             msg3 () 
                             msg4 ()
                | Error err ->
                             logInfoMsg <| sprintf "Err003A %s" (string err.Message)
                             closeItBaby msg5A

        fSharpAsyncParallel ()   
        
        (*
        let fSharpAsyncParallel1 () =  

            msg15
            updateJson1 jsonLinkList pathToJsonList
            
        fSharpAsyncParallel1 ()    
        *)
   
    //input from saved json files -> change of input data -> output into array
    let private digThroughJsonStructure () = //prohrabeme se strukturou json souboru 
    
        let kodisTimetables : Reader<string list, string array> = 

            reader //Reader monad for educational purposes only, no real benefit here  
                {
                    let! pathToJsonList = fun env -> env 

                    let result () = 
                        pathToJsonList 
                        |> Array.ofList 
                        |> Array.collect 
                            (fun pathToJson 
                                ->   
                                 let kodisJsonSamples = JsonProvider1.Parse(File.ReadAllText pathToJson) |> Option.ofNull 
                                 //let kodisJsonSamples = KodisTimetables.GetSample() |> Option.ofObj  //v pripade jen jednoho json               
                                 kodisJsonSamples 
                                 |> function 
                                     | Some value -> 
                                                   value
                                                   |> Option.ofNull 
                                                   |> function
                                                       | Some value -> value |> Array.map _.Timetable  //quli tomuto je nutno Array //nejde Some, nejde Ok
                                                       | None       -> [||]  
                                     | None       -> 
                                                   [||] 
                            )                     
                        
                    return
                        try
                           let value = result ()   
                           value
                           |> function
                               | [||] -> 
                                       logInfoMsg <| sprintf "Err004A %s" "msg16" 
                                       closeItBaby msg16
                                       [||]
                               | _    -> 
                                       value
                        with
                        | ex -> 
                              logInfoMsg <| sprintf "Err005A %s" (string ex.Message) 
                              closeItBaby msg16 
                              [||]      
                                                  
                }

        let kodisAttachments : Reader<string list, string array> = //Reader monad for educational purposes only, no real benefit here
        
            reader 
                {
                    let! pathToJsonList = fun env -> env 
                    
                    let result () = 

                        pathToJsonList
                        |> Array.ofList 
                        |> Array.collect  //vzhledem ke komplikovanosti nepouzivam Result.sequence pro Array.collect, nejde Some, nejde Ok jako vyse
                            (fun pathToJson 
                                -> 
                                 let fn1 (value: JsonProvider1.Attachment array) = 
                                     value
                                     |> List.ofArray
                                     |> List.Parallel.map (fun item -> item.Url |> Option.ofNullEmptySpace) //jj, funguje to :-)                                    
                                     |> List.choose id //co neprojde, to beze slova ignoruju
                                     |> List.toArray

                                 let fn2 (item: JsonProvider1.Vyluky) =  //quli tomuto je nutno Array     
                                     item.Attachments 
                                     |> Option.ofNull        
                                     |> function 
                                         | Some value ->
                                                       value |> fn1
                                         | None       -> 
                                                       msg5 () 
                                                       logInfoMsg <| sprintf "007A %s" "resulting in None"
                                                       [||]                 

                                 let fn3 (item: JsonProvider1.Root) =  //quli tomuto je nutno Array 
                                     item.Vyluky
                                     |> Option.ofNull  
                                     |> function 
                                         | Some value ->
                                                       value |> Array.collect fn2 
                                         | None       ->
                                                       msg5 () 
                                                       logInfoMsg <| sprintf "007B %s" "resulting in None"
                                                       [||] 
                                                      
                                 let kodisJsonSamples = KodisTimetables.Parse(File.ReadAllText pathToJson) |> Option.ofNull  
                                                      
                                 kodisJsonSamples 
                                 |> function 
                                     | Some value -> 
                                                   value |> Array.collect fn3 
                                     | None       -> 
                                                   msg5 () 
                                                   logInfoMsg <| sprintf "007C %s" "resulting in None"
                                                   [||]                                 
                            ) 
                
                    return
                        try
                            let value = result ()
                            value
                            |> function
                                | [||] -> 
                                        logInfoMsg <| sprintf "Err006A %s" "msg16" 
                                        msg5 ()
                                        closeItBaby msg16
                                        [||]
                                | _    -> 
                                        value
                        with
                        | ex -> 
                              logInfoMsg <| sprintf "Err007D %s" (string ex.Message) 
                              msg5 ()
                              closeItBaby msg16 
                              [||]                          
                }
        
        let addOn () =  
            [
                //pro pripad, kdyby KODIS strcil odkazy do uplne jinak strukturovaneho jsonu, tudiz by neslo pouzit dany type provider, anebo kdyz je vubec do jsonu neda (nize uvedene odkazy)
                //@"https://kodis-files.s3.eu-central-1.amazonaws.com/76_2023_10_09_2023_10_20_v_f2b77c8fad.pdf"
                //@"https://kodis-files.s3.eu-central-1.amazonaws.com/64_2023_10_09_2023_10_20_v_02e6717b5c.pdf" 
                //@"https://kodis-files.s3.eu-central-1.amazonaws.com/timetables/119_2024_03_03_2024_12_09.pdf"               
            ] |> List.toArray 
   
        (Array.append (Array.append <| kodisAttachments pathToJsonList <| kodisTimetables pathToJsonList) <| addOn()) |> Array.distinct 
        //(Array.append <| kodisAttachments () <| kodisTimetables ()) |> Array.distinct 

        //kodisAttachments() |> Set.ofArray //over cas od casu
        //kodisTimetables() |> Set.ofArray //over cas od casu

    //input from array -> change of input data -> output into database -> filtering in database -> links*paths  
    let private filterTimetables () param (pathToDir: string) diggingResult = 

        //*************************************Helpers for SQL columns********************************************

        let extractSubstring (input : string) =
            
            try
                let pattern = @"202[3-9]_[0-1][0-9]_[0-3][0-9]_202[4-9]_[0-1][0-9]_[0-3][0-9]"
                let regex = new Regex(pattern) 
                let matchResult = regex.Match(input)
        
                match matchResult.Success with
                | true  -> input 
                | false -> String.Empty 
            with            
            | ex ->
                  logInfoMsg <| sprintf "Err008A %s" (string ex.Message) 
                  msg9 ()
                  String.Empty            
        
        let extractSubstring1 (input : string) =

            try
                let pattern = @"202[3-9]_[0-1][0-9]_[0-3][0-9]_202[4-9]_[0-1][0-9]_[0-3][0-9]"
                let regex = new Regex(pattern) 
                let matchResult = regex.Match(input)
        
                match matchResult.Success with
                | true  -> matchResult.Value
                | false -> String.Empty
            with            
            | ex ->
                  logInfoMsg <| sprintf "Err009A %s" (string ex.Message) 
                  msg9 ()
                  String.Empty 

        let extractStartDate (input : string) =
             let result = 
                 match input.Equals(String.Empty) with
                 | true  -> String.Empty
                 | _     -> input.[0..min 9 (input.Length - 1)]  //A substring from input that includes the first 10 characters (or fewer, if input is shorter than 10 characters) of the string.
                 //The min function to determine the minimum value between 9 and input.Length - 1. If input.Length - 1 is less than 9, then it will return input.Length - 1. Otherwise, it will return 9.
             result.Replace("_", "-")
         
        let extractEndDate (input : string) =
            let result = 
                match input.Equals(String.Empty) with
                | true  -> String.Empty
                | _     -> input.[max 0 (input.Length - 10)..] //A substring from input that includes the last 10 characters (or fewer, if input is shorter than 10 characters) of the string.
                //The max function to ensure the computed length (after subtracting 10) is not negative. If the computed length is negative (i.e., input.Length < 10), it will be adjusted to 0 because max will return the maximum of 0
            result.Replace("_", "-")

        let splitString (input : string) =   
            match input.StartsWith(pathKodisAmazonLink) with
            | true  -> [pathKodisAmazonLink; input.Substring(pathKodisAmazonLink.Length)]
            | false -> [pathKodisAmazonLink; input]

        //*************************************Splitting Kodis links into SQL columns********************************************
        let splitKodisLink input =
           
            let oldPrefix = 
                try
                    Regex.Split(input, extractSubstring1 input) 
                    |> Array.toList
                    |> List.item 0
                    |> splitString
                    |> List.item 1
                with
                | ex -> 
                      logInfoMsg <| sprintf "Err010A %s" (string ex.Message) 
                      msg9 ()
                      String.Empty     

            let totalDateInterval = extractSubstring1 input

            let partAfter =
                try
                    Regex.Split(input, totalDateInterval)
                    |> Array.toList
                    |> List.item 1    
                with
                | ex -> 
                      logInfoMsg <| sprintf "Err011A %s" (string ex.Message) 
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
                        fun () -> oldPrefix.Contains("NAD") && oldPrefix.Length = 5
                        fun () -> oldPrefix.Contains("NAD") && oldPrefix.Length = 6
                        fun () -> oldPrefix.Contains("NAD") && oldPrefix.Length = 7
                        fun () -> oldPrefix.Contains("X") && oldPrefix.Length = 4
                        fun () -> oldPrefix.Contains("X") && oldPrefix.Length = 5
                        fun () -> oldPrefix.Contains("X") && oldPrefix.Length = 6
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
                               sprintf "%s" oldPrefix
                         | 6  ->
                               oldPrefix.Replace("NAD_", "NAD_00")
                         | 7  ->
                               oldPrefix.Replace("NAD_", "NAD_0")
                         | 8  -> 
                               let s1 = oldPrefix
                               let s2 = sprintf "X_%s%s" <| createStringSeq(2, "0") <| s1.[2..]
                               oldPrefix.Replace(s1, s2)
                         | 9  ->
                               let s1 = oldPrefix
                               let s2 = sprintf "X_%s%s" <| createStringSeq(1, "0") <| s1.[2..]
                               oldPrefix.Replace(s1, s2)
                         | 10 ->
                               sprintf "%s" oldPrefix
                         | _  ->
                               sprintf "%s" oldPrefix

                | _     ->
                         match oldPrefix.Length with                    
                         | 2  -> sprintf "%s%s" <| createStringSeq(2, "0") <| oldPrefix   //sprintf "00%s" oldPrefix
                         | 3  -> sprintf "%s%s" <| createStringSeq(1, "0") <| oldPrefix   //sprintf "0%s" oldPrefix                  
                         | _  -> oldPrefix

            let input = 
                match input.Contains("_t") with 
                | true  -> input.Replace(pathKodisAmazonLink, sprintf"%s%s" pathKodisAmazonLink @"timetables/").Replace("_t.pdf", ".pdf") 
                | false -> input   
        
            let fileToBeSaved = sprintf "%s%s%s.pdf" (newPrefix oldPrefix) totalDateInterval suffix

            let record : DbDataSend = 
                {
                    oldPrefix = OldPrefix oldPrefix
                    newPrefix = NewPrefix (newPrefix oldPrefix)
                    startDate = StartDate (extractStartDate totalDateInterval)
                    endDate = EndDate (extractEndDate totalDateInterval)
                    totalDateInterval = TotalDateInterval totalDateInterval
                    suffix = Suffix suffix
                    jsGeneratedString = JsGeneratedString jsGeneratedString
                    completeLink = CompleteLink input
                    fileToBeSaved = FileToBeSaved fileToBeSaved
                }

            record |> dbDataTransformLayerSend  
     
        //**********************Filtering and SQL data inserting********************************************************
        let dataToBeInserted =           
            diggingResult       
            |> Array.Parallel.map 
                (fun item -> 
                           let item = extractSubstring item      //"https://kodis-files.s3.eu-central-1.amazonaws.com/timetables/2_2023_03_13_2023_12_09.pdf                 
                           match item.Contains @"timetables/" with
                           | true  -> item.Replace("timetables/", String.Empty).Replace(".pdf", "_t.pdf")
                           | false -> item  
                )  
            |> Array.toList
            |> List.sort //jen quli testovani
            |> List.filter
                (fun item -> 
                           let cond1 = (item |> Option.ofNullEmptySpace).IsSome
                           let cond2 = item |> Option.ofNullEmpty |> Option.toBool //for learning purposes - compare with (not String.IsNullOrEmpty(item))
                           cond1 && cond2 
                )         
            |> List.map 
                (fun item -> splitKodisLink item) 
        
        (insertWithCE getConnection closeConnection dataToBeInserted).Force()  
        //insert getConnection closeConnection dataToBeInserted  
        
        //**********************Cesty pro soubory pro aktualni a dlouhodobe platne a pro ostatni********************************************************
        let createPathsForDownloadedFiles list =
            
            list 
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
                                           
                         match pathToDir.Contains("JR_ODIS_aktualni_vcetne_vyluk") || pathToDir.Contains("JR_ODIS_teoreticky_dlouhodobe_platne_bez_vyluk") with 
                         | true ->   
                                 true //pro aktualni a dlouhodobe platne
                                 |> function
                                     | true when file.Substring(0, 1) = "0"  -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 0 sortedLines)
                                     | true when file.Substring(0, 1) = "1"  -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 0 sortedLines)
                                     | true when file.Substring(0, 1) = "2"  -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 1 sortedLines)
                                     | true when file.Substring(0, 1) = "3"  -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 2 sortedLines)
                                     | true when file.Substring(0, 1) = "4"  -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 3 sortedLines)
                                     | true when file.Substring(0, 1) = "5"  -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 4 sortedLines)
                                     | true when file.Substring(0, 1) = "6"  -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 5 sortedLines)
                                     | true when file.Substring(0, 1) = "7"  -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 6 sortedLines)
                                     | true when file.Substring(0, 1) = "8"  -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 7 sortedLines)
                                     | true when file.Substring(0, 1) = "9"  -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 8 sortedLines)
                                     | true when file.Substring(0, 1) = "S"  -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 9 sortedLines)
                                     | true when file.Substring(0, 1) = "R"  -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 10 sortedLines)
                                     | true when file.Substring(0, 2) = "_S" -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 9 sortedLines)
                                     | true when file.Substring(0, 2) = "_R" -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 10 sortedLines)
                                     | _                                     -> pathToDir.Replace("_vyluk", sprintf "%s\\%s\\" <| "_vyluk" <| List.item 11 sortedLines)                                                           
                         | _    -> 
                                 pathToDir    
                     link, path 
                )          

        let selectDataFromDb = 

            match param with 
            | CurrentValidity           -> "dbo.ITVF_GetLinksCurrentValidity()" |> select getConnection closeConnection pathToDir |> createPathsForDownloadedFiles
            | FutureValidity            -> "dbo.ITVF_GetLinksFutureValidity()" |> select getConnection closeConnection pathToDir |> createPathsForDownloadedFiles
            //| ReplacementService        -> "dbo.ITVF_GetLinksReplacementService()" |> select getConnection closeConnection pathToDir |> createPathsForDownloadedFiles   
            | WithoutReplacementService -> "dbo.ITVF_GetLinksWithoutReplacementService()" |> select getConnection closeConnection pathToDir |> createPathsForDownloadedFiles 
                   
        selectDataFromDb
 
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
                                |> Seq.toList
                                |> List.Parallel.iter (fun (item : DirectoryInfo) -> item.Delete(true)) //List.Parallel for educational purposes
                                //smazeme pouze adresare obsahujici stare JR, ostatni ponechame             
                        with
                        | ex -> 
                              logInfoMsg <| sprintf "Err012A %s" (string ex.Message)
                              closeItBaby msg16 
                }

        deleteIt listODISDefault4
    
        msg10 () 
        msg11 ()     
 

    //Operations on data made separate in order to have some structure in the free-monad-based design (for educational purposes)  
    let internal createNewDirectories pathToDir : Reader<string list, string list> =
        //Reader monad for educational purposes only, no real benefit here
        reader
            { 
                let! getDefaultRecordValues = 
                    fun env -> env in return getDefaultRecordValues |> List.map (fun item -> sprintf"%s\%s"pathToDir item) 
            } 

    //Operations on data made separate in order to have some structure in the free-monad-based design (for educational purposes)           
    let internal createDirName variant : Reader<string list, string> = //Reader monad for educational purposes only, no real benefit here

        reader
            {
                let! getDefaultRecordValues = fun env -> env

                return 
                    match variant with 
                    | CurrentValidity           -> getDefaultRecordValues |> List.item 0
                    | FutureValidity            -> getDefaultRecordValues |> List.item 1
                    //| ReplacementService        -> getDefaultRecordValues |> List.item 2                                
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
                        with
                        | ex -> 
                              logInfoMsg <| sprintf "Err012C %s" (string ex.Message)
                              closeItBaby msg16 
                }

        deleteIt listODISDefault4    

        msg10 () 
        msg11 ()   
 
    //list -> aby bylo mozno pouzit funkci createFolders bez uprav
    //Operations on data made separate in order to have some structure in the free-monad-based design (for educational purposes)   
    let internal createOneNewDirectory pathToDir dirName = [ sprintf"%s\%s"pathToDir dirName ] 
 
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
                )              
        with
        | ex ->           
              logInfoMsg <| sprintf "Err013A %s" (string ex.Message)
              closeItBaby msg16        

    //input from data filtering (links*paths) -> http request -> IO operation -> saving pdf data files on HD           
    let private downloadAndSaveTimetables pathToDir =     //FsHttp
        
        msgParam3 pathToDir  

        cts.Cancel()

        let asyncDownload (counterAndProgressBar : MailboxProcessor<Msg>) list =   
                       
            list 
            |> List.iter 
                (fun (uri, (pathToFile: string)) 
                    ->                         
                    async
                        {   
                            match not <| NetworkInterface.GetIsNetworkAvailable() with
                            | true  ->                                    
                                     processorPdf.Post(Incr 1)
                            | false ->
                                     //failwith "Simulated exception"  
                                     counterAndProgressBar.Post(Incr 1)
                                       
                                     let get uri =
                                         http 
                                            {
                                                config_timeoutInSeconds 120  //for educational purposes
                                                GET(uri) 
                                            }    
                                     
                                     use! response = get >> Request.sendAsync <| uri  
                                     
                                     match response.statusCode with
                                     | HttpStatusCode.OK -> return! response.SaveFileAsync >> Async.AwaitTask <| pathToFile      //Original FsHttp library function                                                                                                 
                                     | _                 -> return logInfoMsg "Err013AB HttpStatusCode not OK"  //nechame chybu projit v loop                                                                                                                                  
                        } 
                    |> Async.Catch
                    |> Async.RunSynchronously  
                    |> Result.ofChoice                      
                    |> function
                        | Ok _      ->    
                                     ()
                        | Error err ->
                                     logInfoMsg <| sprintf "Err014A %s" (string err.Message)
                                     msgParam2 uri  //nechame chybu projit v loop => nebude Result.sequence
                )  

        reader
            {   
                return! 
                    (fun (env : (string*string) list)
                        ->       
                         cts.Cancel()

                         match not <| NetworkInterface.GetIsNetworkAvailable() with
                         | true  ->                                    
                                  processorPdf.Post(Incr 1) //vypnuti programu
                         | false ->                  
                                  let l = env |> List.length

                                  let numberOfThreads1 = numberOfThreads () l

                                  let counterAndProgressBar =
                                      MailboxProcessor.Start
                                         (fun inbox 
                                             ->
                                              let rec loop n =
                                                  async
                                                      { 
                                                          match! inbox.Receive() with
                                                          | Incr i             -> 
                                                                                progressBarContinuous n l  
                                                                                return! loop (n + i)
                                                          | Fetch replyChannel ->
                                                                                replyChannel.Reply n 
                                                                                return! loop n
                                                     }
                                              loop 0
                                         )                       
                                                 
                                  let myList = splitListIntoEqualParts numberOfThreads1 env                             
                              
                                  fun i -> <@ async { return asyncDownload counterAndProgressBar (%%expr myList |> List.item %%(expr i)) } @>
                                  |> List.init myList.Length
                                  |> List.map _.Compile()       
                                  |> Async.Parallel 
                                  |> Async.Catch 
                                  |> Async.RunSynchronously
                                  |> Result.ofChoice  
                                  |> function
                                      | Ok _      ->
                                                   msgParam4 pathToDir
                                      | Error err ->
                                                   logInfoMsg <| sprintf "Err015A %s" (string err.Message)   
                                                   msgParam7 msg23  //nechame chybu projit v loop                                            
                    )                        
            } 
            
    let internal operationOnDataFromJson variant dir =   
        
        try    
            //operation on data
            //input from saved json files -> change of input data -> output into array >> input from array -> change of input data -> output into database -> data filtering (links*paths)
            digThroughJsonStructure >> filterTimetables () variant dir <| ()                       
        with
        | ex -> 
              logInfoMsg <| sprintf "Err016 %s" (string ex.Message)
              closeItBaby msg16 
              []
                           
    let internal downloadAndSave dir list = 

        match dir |> Directory.Exists with 
        | false -> 
                 msgParam5 dir 
                 msg13 ()                                               
        | true  ->
                 try
                     //input from data filtering (links*paths) -> http request -> saving pdf files on HD
                     match list with
                     | [] -> msgParam13 dir       
                     | _  -> downloadAndSaveTimetables dir list      
                 with
                 | ex -> 
                       logInfoMsg <| sprintf "Err017 %s" (string ex.Message)
                       closeItBaby msg16   