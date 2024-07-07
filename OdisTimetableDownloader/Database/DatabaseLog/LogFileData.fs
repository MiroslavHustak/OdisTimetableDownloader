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
    
    // For educational purposes
    // V komercni aplikaci zkopirovat log file a pouzivat kopii pro cteni

    //Thoth           
    let private decoder : Decoder<string*string*string> = Decode.tuple3 Decode.string Decode.string Decode.string         
           
    //Thoth
    let internal extractLogEntriesThoth () = 

        let rec attemptExtractLogEntries () = 

            try            
                pyramidOfDoom
                    {
                        let filepath = Path.GetFullPath(logFileName) |> Option.ofNullEmpty  
                        let! filepath = filepath, Error (sprintf "%s%s" "Chyba při čtení cesty k souboru " logFileName)
    
                        let fInfodat: FileInfo = new FileInfo(logFileName)
                        let! _ = fInfodat.Exists |> Option.ofBool, Error (sprintf "Soubor %s nenalezen" logFileName) 

                        let fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.None)
                        //To ensure that the log file is not being written to while you are reading it -> FileShare.None setting
                    
                        let reader = new StreamReader(fs)
                    
                        let lines = 
                            reader.ReadToEnd()
                            |> fun content -> content.Split([|Environment.NewLine|], StringSplitOptions.RemoveEmptyEntries)
                            |> Array.toList
                            |> List.map (fun jArrayLine -> Decode.fromString decoder jArrayLine) 
                            |> List.distinct 
                            |> Result.sequence    

                        fs.Flush()                    
                        fs.Close()
                        fs.Dispose()

                        reader.Close()
                        reader.Dispose()

                        return lines
                        (*
                        return
                            File.ReadAllLines(logFileName)
                            |> Array.toList
                            |> List.map (fun jArrayLine -> Decode.fromString decoder jArrayLine) 
                            |> List.distinct 
                            |> Result.sequence    
                        *)
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
                  [] //tady nevadi List.empty jakozto vystup 
                   
        attemptExtractLogEntries ()    
        
     //NewtonSoft  + File.ReadAllLines for educational purposes
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
              printfn "%s" "Err2002A"
              printfn "%s" <| string ex.Message //proste s tim nic nezrobime, kdyz to nebude fungovat... 
              [] //tady nevadi List.empty jakozto vystup 