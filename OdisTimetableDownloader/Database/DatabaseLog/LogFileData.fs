namespace Database2

open System
open System.IO
open Thoth.Json.Net
open Newtonsoft.Json
open Newtonsoft.Json.Linq

//**************************

open Helpers
open Helpers.Builders
open Helpers.CloseApp

open Logging.Logging

open Settings.SettingsGeneral  
open FSharp.Data.Runtime.BaseTypes
open System.Text.Json
      
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

        let rec attemptExtractLogEntries () = 
        
            try            
                pyramidOfDoom
                    {
                        let filepath = Path.GetFullPath(logFileName) |> Option.ofNullEmpty  
                        let! filepath = filepath, Error (sprintf "%s%s" "Chyba při čtení cesty k souboru " logFileName)
            
                        let fInfodat: FileInfo = new FileInfo(logFileName)
                        let! _ = fInfodat.Exists |> Option.ofBool, Error (sprintf "Soubor %s nenalezen" logFileName) 
        
                        //toto je fakticky custom-made deserializace s pouzitim Thoth pro decoding Json
                        //FileShare.None -> zakaz zapisu do souboru v dobe cteni
                        let fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.None) 
                        use! fs = fs |> Option.ofNull, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " logFileName)                        
                    
                        let reader = new StreamReader(fs) //For large files, StreamReader may offer better performance and memory efficiency
                        use! reader = reader |> Option.ofNull, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " logFileName) 
                        
                        let jsonContent = reader.ReadToEnd()
                        let! jsonContent = jsonContent |> Option.ofNullEmpty, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " logFileName)  
                    
                        let lines = 
                            jsonContent
                            |> fun content -> content.Split([|Environment.NewLine|], StringSplitOptions.RemoveEmptyEntries)
                            |> Array.toList
                            |> List.map (fun jArrayLine -> Decode.fromString decoder jArrayLine) 
                            |> List.distinct 
                            |> Result.sequence    
                
                        return lines
                    }
        
                |> function
                    | Ok value  -> value
                    | Error err -> [err, String.Empty, String.Empty]
        
            with
            | :? IOException as ex 
                 ->
                  // Handle IO exceptions (file is locked) and retry after a delay
                  System.Threading.Thread.Sleep(1000) //nekonecny cyklus, nekdy se to ujme :-)
                  printfn "%s" "Tak si zopakujeme načítání záznamů v logfile ..."
        
                  attemptExtractLogEntries ()
        
            | :? UnauthorizedAccessException as ex 
                 -> 
                  printfn "Err2002E"
                  printfn "%s" <| string ex.Message 
                  [] 
                            
            | ex -> 
                  printfn "%s" "Err2002D"
                  printfn "%s" <| string ex.Message 
                  []
                   
        attemptExtractLogEntries ()  
   
    //Thoth + System.IO (File.ReadAllLines) + JsonArray
    let internal extractLogEntriesThoth2 () = 

        let rec attemptExtractLogEntries () = 

            try            
                pyramidOfDoom
                    {
                        let filepath = Path.GetFullPath(logFileName) |> Option.ofNullEmpty  
                        let! filepath = filepath, Error (sprintf "Chyba při čtení cesty k souboru %s" logFileName)
    
                        let fInfoDat = new FileInfo(logFileName)
                        let! _ = fInfoDat.Exists |> Option.ofBool, Error (sprintf "Soubor %s nenalezen" logFileName)
                        
                        //For small to medium files, File.ReadAllLines is usually faster
                        let fs = File.ReadAllLines(logFileName) //Automaticky zrobi close, dispose
                        let! fs = fs |> Option.ofNull, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " logFileName)

                        let lines = 
                            fs
                            |> List.ofSeq
                            |> List.map (fun jArrayLine -> Decode.fromString decoder jArrayLine) 
                            |> List.distinct
                            |> Result.sequence                              
                         
                        return lines
                    }

                |> function
                    | Ok value  -> value
                    | Error err -> [err, String.Empty, String.Empty] 

            with
            | :? IOException as ex 
                 ->
                  // Handle IO exceptions (file is locked) and retry after a delay
                  System.Threading.Thread.Sleep(1000) //nekonecny cyklus, nekdy se to ujme :-)
                  printfn "%s" "Tak si zopakujeme načítání záznamů v logfile ..."

                  attemptExtractLogEntries ()

            | :? UnauthorizedAccessException as ex 
                 -> 
                  printfn "Err2002C"
                  printfn "%s" <| string ex.Message //proste s tim nic nezrobime, kdyz to nebude fungovat... 
                  []
                            
            | ex -> 
                  printfn "%s" "Err2002B"
                  printfn "%s" <| string ex.Message //proste s tim nic nezrobime, kdyz to nebude fungovat... 
                  []                 
                   
        attemptExtractLogEntries ()    

    //*********************************************************************************************


    //Nepouzivano -> Newtonsoft.Json  + File.ReadAllLines for educational purposes
    let internal extractLogEntries () = 

        try            
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

            |> function
                | Ok value -> value
                | Error _  -> [] //k tomu nedojde
        with
        | ex -> 
              printfn "%s" "Err2002A"
              printfn "%s" <| string ex.Message //proste s tim nic nezrobime, kdyz to nebude fungovat... 
              [] //tady nevadi List.empty jakozto vystup 