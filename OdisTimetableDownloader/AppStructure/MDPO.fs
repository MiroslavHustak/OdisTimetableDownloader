namespace MainFunctions

open System
open System.IO

//**********************

open Types.Types
open Logging.Logging
open Helpers.CloseApp

open Settings.Messages
open Settings.SettingsGeneral

open SubmainFunctions.MDPO_Submain    

module WebScraping_MDPO =

    //Design pattern for WebScraping_MDPO : AbstractApplePlumCherryApricotBrandyProxyDistilleryBean 

    //************************Main code*******************************************************************************

    type private State =  //not used
        { 
            TimetablesDownloadedAndSaved : unit //zatim nevyuzito
        }

    let private stateDefault = 
        {          
            TimetablesDownloadedAndSaved = () //zatim nevyuzito
        }

    type private Actions =
        | StartProcess
        | DeleteOneODISDirectory
        | CreateFolders
        | FilterDownloadSave    
        | EndProcess

    type private Environment = 
        {
            FilterTimetables : unit -> string -> Map<string, string>
            DownloadAndSaveTimetables : string -> Map<string, string> -> unit
        }

    let private environment : Environment =
        { 
            FilterTimetables = filterTimetables 
            DownloadAndSaveTimetables = downloadAndSaveTimetables       
        }    

    let internal webscraping_MDPO pathToDir =  

        let stateReducer (state: State) (action: Actions) (environment: Environment) =

            let dirList pathToDir = [ sprintf"%s\%s"pathToDir ODISDefault.OdisDir6 ]
           
            match action with                                                   
            | StartProcess           -> 
                                      try 
                                          Console.Clear()
                                          let processStartTime = sprintf "Začátek procesu: %s" <| DateTime.Now.ToString("HH:mm:ss") 
                                              in msgParam7 processStartTime |> Ok
                                      with
                                      | ex -> Error <| string ex.Message        
                                      |> function
                                          | Ok value  -> 
                                                       value  
                                          | Error err ->
                                                       logInfoMsg <| sprintf "Err051 %s" err
                                                       closeItBaby msg16                        

            | DeleteOneODISDirectory ->                                     
                                                                          
                                      try
                                          let dirName = ODISDefault.OdisDir6
                                          //rozdil mezi Directory a DirectoryInfo viz Unique_Identifier_And_Metadata_File_Creator.sln -> MainLogicDG.fs
                                          let dirInfo = new DirectoryInfo(pathToDir)    
                                              in 
                                              dirInfo.EnumerateDirectories()
                                              |> Seq.filter (fun item -> item.Name = dirName) 
                                              |> Seq.iter _.Delete(true) //trochu je to hack, ale nemusim se zabyvat tryHead, bo moze byt empty kolekce
                                              |> Ok

                                      with
                                      | ex -> Error <| string ex.Message
                                      
                                      |> function
                                      | Ok value  -> 
                                                   value  
                                      | Error err ->
                                                   logInfoMsg <| sprintf "Err051A %s" err
                                                   closeItBaby msg16    
                                      msg12 () 
                                    
            | CreateFolders          -> 
                                      try
                                          dirList pathToDir
                                          |> List.iter (fun dir -> Directory.CreateDirectory(dir) |> ignore)
                                          |> Ok

                                      with
                                      | ex -> Error <| string ex.Message  
                                      
                                      |> function
                                          | Ok value  -> 
                                                       value  
                                          | Error err ->
                                                       logInfoMsg <| sprintf "Err051B %s" err
                                                       closeItBaby msg16             
                              
            | FilterDownloadSave     -> 
                                      //filtering timetable links, downloading and saving timetables in the pdf format 
                                      try
                                          let pathToSubdir = dirList pathToDir |> List.head    
                                          
                                          match pathToSubdir |> Directory.Exists with 
                                          | false ->                                              
                                                   msgParam5 pathToSubdir   
                                                   msg1 () 
                                                   Error String.Empty
                                          | true  -> 
                                                   environment.FilterTimetables () pathToSubdir 
                                                   |> environment.DownloadAndSaveTimetables pathToSubdir 
                                                   |> Ok
                                      with
                                      | ex -> Error <| string ex.Message  
                                      
                                      |> function
                                          | Ok value  -> 
                                                       value  
                                          | Error err ->
                                                       logInfoMsg <| sprintf "Err051C %s" err
                                                       closeItBaby msg16               
                                                                                
            | EndProcess             -> 
                                      try  
                                          let processEndTime = sprintf "Konec procesu: %s" <| DateTime.Now.ToString("HH:mm:ss")                       
                                              in msgParam7 processEndTime |> Ok
                                      with
                                      | ex -> Error <| string ex.Message  

                                      |> function
                                          | Ok value  -> 
                                                       value  
                                          | Error err ->
                                                       logInfoMsg <| sprintf "Err051D %s" err
                                                       closeItBaby msg16              
                                 
        stateReducer stateDefault StartProcess environment
        stateReducer stateDefault DeleteOneODISDirectory environment
        stateReducer stateDefault CreateFolders environment
        stateReducer stateDefault FilterDownloadSave environment
        stateReducer stateDefault EndProcess environment