namespace Logging

open System

open Newtonsoft.Json
open NReco.Logging.File
open Microsoft.Extensions.Logging

//**********************************

open Settings.SettingsGeneral

module Logging =     

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

            string sb    
        with
        | ex -> 
              printfn "%s" "Tato chyba není zaznamenána v log file. Err2001."
              printfn "%s" <| string ex.Message //proste s tim nic nezrobime, kdyz to nebude fungovat...
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
       
    let private logger = loggerFactory.CreateLogger("TimetableDownloader")
   
    let internal logInfoMsg msg = logger.LogInformation(msg)