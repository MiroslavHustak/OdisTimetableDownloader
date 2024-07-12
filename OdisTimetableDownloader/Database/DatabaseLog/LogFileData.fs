namespace Database2

open System
open System.IO
open Thoth.Json.Net
open Newtonsoft.Json
open Newtonsoft.Json.Linq

open FSharp.Control

//**************************

open Helpers
open Helpers.Builders
open Helpers.CloseApp

open Types.ErrorTypes

open Logging.Logging

open Settings.SettingsGeneral  
      
module LogFileData =   
    
    // For educational purposes
    // V komercni aplikaci zkopirovat log file a pouzivat kopii pro cteni
    
    //Thoth           
    let private decoder : Decoder<string*string*string> =   
        Decode.tuple3 
        <| Decode.string
        <| Decode.string 
        <| Decode.string         
           
    //Thoth + StreamReader + JsonArray
    let internal extractLogEntriesThoth () = 

        //[<TailCall>]  Ok
        let rec attemptExtractLogEntries counter =  
                        
            try   
                pyramidOfDoom
                    {
                        let filepath = Path.GetFullPath(logFileName) |> Option.ofNullEmpty  
                        let! filepath = filepath, Error (sprintf "%s%s" "Chyba při čtení cesty k souboru " logFileName)
            
                        let fInfodat: FileInfo = new FileInfo(logFileName)
                        let! _ = fInfodat.Exists |> Option.ofBool, Error (sprintf "Soubor %s nenalezen" logFileName) 
        
                        //toto je fakticky custom-made deserializace s pouzitim Thoth pro decoding Json
                        //FileShare.None -> zakaz zapisu do souboru v dobe cteni
                        use fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.None) 
                        let! _ = fs |> Option.ofNull, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " logFileName)                        
                    
                        use reader = new StreamReader(fs) //For large files, StreamReader may offer better performance and memory efficiency
                        let! _ = reader |> Option.ofNull, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " logFileName) 
                        
                        let jsonContent = reader.ReadToEnd()
                        let! jsonContent = jsonContent |> Option.ofNullEmpty, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " logFileName)  
                
                        return 
                            jsonContent
                            |> fun content -> content.Split([|Environment.NewLine|], StringSplitOptions.RemoveEmptyEntries)
                            |> Array.toList
                            |> List.map (fun jArrayLine -> Decode.fromString decoder jArrayLine) 
                            |> List.distinct 
                            |> Result.sequence    
                    }  
                    
                |> function
                    | Ok value  -> Ok value
                    | Error err -> Error (AllOtherErrors err)
            with
            | :? IOException as ex                 -> Error (IOExnErr <| string ex.Message) 
            | :? UnauthorizedAccessException as ex -> Error (UnauthorizedAccessExnErr <| string ex.Message)    
            | ex                                   -> Error (AllOtherErrors <| string ex.Message)                
                  
            |> function
                | Ok value   ->
                              value
                | Error case ->
                              case
                              |> function
                                  | IOExnErr err 
                                      ->                                 
                                       // Handle IO exceptions (file is locked) and retry after a delay
                                       System.Threading.Thread.Sleep(1000) 

                                       match counter < 10 with  //10 pokusu o zapis do log file
                                       | false -> 
                                                printfn "Err2002E"
                                                printfn "Pokusy o zápis do log file %s selhaly" logFileName
                                                []
                                       | true  ->  
                                                printfn "Další pokus číslo: %i" counter
                                                attemptExtractLogEntries (counter + 1)

                                  | UnauthorizedAccessExnErr err 
                                      -> 
                                       printfn "Err2002C"
                                       printfn "%s" err //proste s tim nic nezrobime, kdyz to nebude fungovat... 
                                       [] 

                                  | AllOtherErrors err 
                                      -> 
                                       printfn "%s" "Err2002B"
                                       printfn "%s" err //proste s tim nic nezrobime, kdyz to nebude fungovat... 
                                       []
                                       
                                  | _ ->
                                       []
                   
        attemptExtractLogEntries 0 
   
    //Thoth + System.IO (File.ReadAllLines) + JsonArray
    let internal extractLogEntriesThoth2 () = 
       
        //[<TailCall>]  Ok
        let rec attemptExtractLogEntries counter = 

            try  
                //raise (IOException("test"))

                pyramidOfDoom
                    {
                        let filepath = Path.GetFullPath(logFileName) |> Option.ofNullEmpty  
                        let! filepath = filepath, Error (sprintf "Chyba při čtení cesty k souboru %s" logFileName)
    
                        let fInfoDat = new FileInfo(logFileName)
                        let! _ = fInfoDat.Exists |> Option.ofBool, Error (sprintf "Soubor %s nenalezen" logFileName)
                        
                        //For small to medium files, File.ReadAllLines is usually faster
                        let fs = File.ReadAllLines(logFileName) //Automaticky zrobi close, dispose
                        let! fs = fs |> Option.ofNull, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " logFileName)
                                        
                        return 
                            fs
                            |> List.ofSeq
                            |> List.map (fun jArrayLine -> Decode.fromString decoder jArrayLine) 
                            |> List.distinct
                            |> Result.sequence 
                    }

                |> function
                    | Ok value  -> Ok value
                    | Error err -> Error (AllOtherErrors err) 

            with
            | :? IOException as ex                 -> Error (IOExnErr <| string ex.Message) 
            | :? UnauthorizedAccessException as ex -> Error (UnauthorizedAccessExnErr <| string ex.Message)    
            | ex                                   -> Error (AllOtherErrors <| string ex.Message)                
                  
            |> function
                | Ok value   ->
                              value
                | Error case ->
                              case
                              |> function
                                  | IOExnErr err 
                                      ->                                 
                                       // Handle IO exceptions (file is locked) and retry after a delay
                                       System.Threading.Thread.Sleep(1000) 

                                       match counter < 10 with  //10 pokusu o zapis do log file
                                       | false -> 
                                                printfn "Err2002E"
                                                printfn "Pokusy o zápis do log file %s selhaly" logFileName
                                                []
                                       | true  ->  
                                                printfn "Další pokus číslo: %i" counter
                                                attemptExtractLogEntries (counter + 1)

                                  | UnauthorizedAccessExnErr err 
                                      -> 
                                       printfn "Err2002C"
                                       printfn "%s" err //proste s tim nic nezrobime, kdyz to nebude fungovat... 
                                       [] 

                                  | AllOtherErrors err 
                                      -> 
                                       printfn "%s" "Err2002B"
                                       printfn "%s" err //proste s tim nic nezrobime, kdyz to nebude fungovat... 
                                       []
                                       
                                  | _ ->
                                       []
                   
        attemptExtractLogEntries 0 
    
    //*********************************************************************************************

    //Nepouzivano -> Newtonsoft.Json  + File.ReadAllLines for educational purposes
    let internal extractLogEntries () = 
        
        //nepouzivano, pouze for educational purposes, bez try-with bloku
        pyramidOfDoom
            {
                let filepath = Path.GetFullPath(logFileName) |> Option.ofNullEmpty  
                let! filepath = filepath, Error (sprintf "%s%s" "Chyba při čtení cesty k souboru " logFileName)
    
                let fInfodat: FileInfo = new FileInfo(logFileName)
                let! _ =  fInfodat.Exists |> Option.ofBool, Error (sprintf "Soubor %s nenalezen" logFileName) 
                                           
                return 
                    File.ReadAllLines(logFileName)
                    |> Seq.map 
                        (fun line ->                             
                                   let item = JArray.Parse(line)                   
                                   //tady nevadi pripadne String.Empty   
                                   let timestamp = string item.[0] //nelze Array.item 0
                                   let logName = string item.[1]
                                   let message = string item.[2]  

                                   timestamp, logName, message  
                        )                 
                    |> List.ofSeq        
                    |> List.distinct 
                    |> Ok
            }

           