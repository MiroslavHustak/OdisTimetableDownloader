namespace Logging

open System

open Newtonsoft.Json
open NReco.Logging.File
open Microsoft.Extensions.Logging

//**********************************

open MyFsToolkit

//**********************************

open Settings.SettingsGeneral

module Logging =   
    
    let private closeItBaby err = 

        printfn "\nChyba při zápisu do logfile. Zmáčkni cokoliv pro ukončení programu a mrkni se na problém. Popis chyby: %s" err     
        Console.ReadKey() |> ignore 
        System.Environment.Exit 1 

    // Function to format log entry as JSON array
    let private formatLogEntry (msg: LogMessage) =

        try
            let sb = System.Text.StringBuilder()
            use sw = new System.IO.StringWriter(sb)
            use jsonWriter = new JsonTextWriter(sw)

            jsonWriter.WriteStartArray()
            jsonWriter.WriteValue(string DateTime.Now)
            //jsonWriter.WriteValue(string msg.LogLevel)

            jsonWriter.WriteValue(msg.LogName)
            //jsonWriter.WriteValue(msg.EventId.Id)

            jsonWriter.WriteValue(msg.Message)
            jsonWriter.WriteEndArray()

            Ok <| string sb    
        with
        | ex -> Error <| string ex.Message

        |> function
            | Ok value  -> 
                         value
            | Error err -> 
                         printfn "%s" "Je třeba kontaktovat programátora, tato chyba není zaznamenána v log file. Err2001."
                         printfn "%s" err //proste s tim nic nezrobime, kdyz to nebude fungovat...
                         String.Empty  

    //***************************Log files******************************       
    
    let private loggerFactory = 
        LoggerFactory.Create(
            fun builder ->                                        
                         builder.AddFile(
                             logFileName, 
                             fun fileLoggerOpts
                                 ->     
                                  //ostatni properties nefungovaly, TODO zjistit cemu  
                                  fileLoggerOpts.FileSizeLimitBytes <- 52428800
                                  fileLoggerOpts.MaxRollingFiles <- 10   
                                  fileLoggerOpts.FormatLogEntry <- formatLogEntry
                             ) 
                             |> ignore
        )
       
    let private logger = 
        loggerFactory.CreateLogger("TimetableDownloader")
                
    let internal logInfoMsg msg = 
        logger.LogInformation(msg)
        loggerFactory.Dispose()