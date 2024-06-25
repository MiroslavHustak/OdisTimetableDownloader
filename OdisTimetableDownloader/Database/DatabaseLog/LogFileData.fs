namespace Database2

open System.IO
open Newtonsoft.Json.Linq

//**************************

open Helpers
open Helpers.CloseApp

open Logging.Logging

open Settings.SettingsGeneral

module LogFileData =
  
    let internal extractLogEntries () =
        
        try
            File.ReadAllLines(logFileName)
            |> Array.map (fun line -> JArray.Parse(line))       
            |> Array.map 
                (fun item -> 
                           //tady nevadi pripadne String.Empty   
                           let timestamp = string item.[0] //nelze Array.item 0
                           let logName = string item.[1]
                           let message = string item.[2]  
                           //failwith "test extractLogEntries"
                           timestamp, logName, message                     
                ) 
            |> List.ofArray         
            |> List.distinct        
        with
        | ex -> 
              printfn "%s" "Je třeba zavolat programátora, tato chyba není zaznamenána v log file. Err2002."
              printfn "%s" <| string ex.Message //proste s tim nic nezrobime, kdyz to nebude fungovat... 
              //logInfoMsg <| sprintf "Err2002 %s" (string ex.Message)                    
              [] //tady nevadi List.empty jakozto vystup 