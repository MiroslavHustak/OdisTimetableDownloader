namespace Database2

open System
open System.IO
open Thoth.Json.Net
open Newtonsoft.Json.Linq

//**************************

open Helpers
open Helpers.Builders
open Helpers.CloseApp

open Logging.Logging

open Settings.SettingsGeneral
  
      
module LogFileData =               

    //Thoth           
    let private decoder : Decoder<string*string*string> = Decode.tuple3 Decode.string Decode.string Decode.string         
           
    //Thoth
    let internal extractLogEntriesThoth () = 

        try            
            pyramidOfDoom
                {
                    let filepath = Path.GetFullPath(logFileName) |> Option.ofNullEmpty  
                    let! filepath = filepath, Error (sprintf "%s%s" "Chyba při čtení cesty k souboru " logFileName)
    
                    let fInfodat: FileInfo = new FileInfo(logFileName)
                    let! _ =  fInfodat.Exists |> Option.ofBool, Error (sprintf "Soubor %s nenalezen" logFileName) 
                                           
                    return 
                        File.ReadAllLines(logFileName)
                        |> Array.toList
                        |> List.map (fun json -> Decode.fromString decoder json) 
                        |> List.distinct 
                        |> Result.sequence    
                }

            |> function
                | Ok value  -> value
                | Error err -> [err, String.Empty, String.Empty]
        with
        | ex -> 
              printfn "%s" "Tato chyba není zaznamenána v log file. Err2002B."
              printfn "%s" <| string ex.Message //proste s tim nic nezrobime, kdyz to nebude fungovat... 
              [] //tady nevadi List.empty jakozto vystup 
                   
                
     //NewtonSoft
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
                        |> Array.map 
                            (fun line ->                             
                                       let item = JArray.Parse(line)                   
                                       //tady nevadi pripadne String.Empty   
                                       let timestamp = string item.[0] //nelze Array.item 0
                                       let logName = string item.[1]
                                       let message = string item.[2]  

                                       timestamp, logName, message  
                            )                 
                        |> List.ofArray         
                        |> List.distinct 
                        |> Ok
                }

            |> function
                | Ok value -> value
                | Error _  -> [] //k tomu nedojde
        with
        | ex -> 
              printfn "%s" "Tato chyba není zaznamenána v log file. Err2002A."
              printfn "%s" <| string ex.Message //proste s tim nic nezrobime, kdyz to nebude fungovat... 
              [] //tady nevadi List.empty jakozto vystup 