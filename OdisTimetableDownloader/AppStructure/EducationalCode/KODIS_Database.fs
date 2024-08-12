namespace MainFunctions

open System

//**********************************
         
open Types
open Types.Types

open Settings.Messages
open Settings.SettingsKODIS
open Settings.SettingsGeneral
    
open Logging.Logging

open Helpers.CloseApp  
open Helpers.FreeMonads  
        
open Database2.InsertInto
open Database2.Connection

open SubmainFunctions
open SubmainFunctions.KODIS_Submain

module WebScraping_KODISFM = 
        
    //FREE MONAD 

    let internal webscraping_KODISFM pathToDir (variantList : Validity list) = 

        let startProcess = DateTime.Now
            
        let rec interpret clp  = 

            //function //CommandLineProgram<unit> -> unit
            match clp with
            | Pure x                                -> 
                                                     x //nevyuzito

            | Free (StartProcessFM next)            -> 
                                                     try   
                                                         Console.Clear()

                                                         let processStartTime = 
                                                             try   
                                                                 startNetChecking ()
                                                                 sprintf "Začátek procesu: %s" <| DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") 
                                                             with
                                                             | ex ->       
                                                                   logInfoMsg <| sprintf "Err503 %s" (string ex.Message)                                                                    
                                                                   "Začátek procesu nemohl býti ustanoven."
                                                             in msgParam7 processStartTime |> Ok
                                                     with 
                                                     | ex -> Error <| string ex.Message 
                                                     
                                                     |> function
                                                         | Ok value  -> 
                                                                      value  
                                                         | Error err ->
                                                                      logInfoMsg <| sprintf "Err050A %s" err
                                                                      closeItBaby msg16    

                                                     let param = next ()
                                                     interpret param

            | Free (DownloadAndSaveJsonFM next)     ->    
                                                     //Http request and IO operation (data from settings -> http request -> IO operation -> saving json files on HD)
                                                     try 
                                                         startNetChecking ()
                                                         
                                                         msg2 ()    
                                                         msg15 ()
        
                                                         Console.Write("\r" + new string(' ', (-) Console.WindowWidth 1) + "\r")
                                                         Console.CursorLeft <- 0  

                                                         KODIS_Submain.downloadAndSaveJson (jsonLinkList @ jsonLinkList2) (pathToJsonList @ pathToJsonList2) 
                                                         
                                                         msg3 ()   
                                                         msg11 ()   
                                                         
                                                         Ok ()
                                                         
                                                     with 
                                                     | ex -> Error <| string ex.Message 
                                                     
                                                     |> function
                                                         | Ok value  -> 
                                                                      value  
                                                         | Error err ->
                                                                      logInfoMsg <| sprintf "Err050B %s" err
                                                                      closeItBaby msg16    
                                                      
                                                     let param = next ()
                                                     interpret param                                            
                                                
            | Free (DownloadSelectedVariantFM next) -> 
                                                     try                                                        
                                                         let connection = Database.Connection.getConnection ()

                                                         try
                                                             match variantList |> List.length with
                                                             //SingleVariantDownload
                                                             | 1 -> 
                                                                  let variant = variantList |> List.head

                                                                  //IO operation
                                                                  KODIS_Submain.deleteOneODISDirectory variant pathToDir 
                                                              
                                                                  //operation on data
                                                                  let dirList =                                                                    
                                                                      KODIS_Submain.createOneNewDirectoryPath  //list -> aby bylo mozno pouzit funkci createFolders bez uprav  
                                                                      <| pathToDir 
                                                                      <| KODIS_Submain.createDirName variant listODISDefault4 

                                                                  //IO operation 
                                                                  KODIS_Submain.createFolders dirList

                                                                  let dir = (dirList |> List.head)

                                                                  //operation on data 
                                                                  //input from saved json files -> change of input data -> output into seq -> input from seq -> change of input data -> output into database -> data filtering (link*path) 
                                                                  let list = KODIS_Submain.operationOnDataFromJson () connection variant dir

                                                                  let context listMappingFunction = 
                                                                     {
                                                                         listMappingFunction = listMappingFunction
                                                                         dir = dir
                                                                         list = list
                                                                     }   
                                                             
                                                                  match variant with
                                                                  | FutureValidity -> context List.map2 
                                                                  | _              -> context List.Parallel.map2  

                                                                  //IO operation (data filtering (link * path) -> http request -> saving pdf files on HD)
                                                                  |> KODIS_Submain.downloadAndSave 

                                                             //BulkVariantDownload       
                                                             | _ ->  
                                                                  //IO operation
                                                                  KODIS_Submain.deleteAllODISDirectories pathToDir
                                                              
                                                                  //operation on data 
                                                                  let dirList = KODIS_Submain.createNewDirectoryPaths pathToDir listODISDefault4
                                                              
                                                                  //IO operation 
                                                                  KODIS_Submain.createFolders dirList 
                                                              
                                                                  (variantList, dirList)
                                                                  ||> List.iter2 
                                                                      (fun variant dir 
                                                                          -> 
                                                                           //operation on data 
                                                                           //input from saved json files -> change of input data -> output into seq -> input from seq -> change of input data -> output into database -> data filtering (link*path) 
                                                                           let list = KODIS_Submain.operationOnDataFromJson () connection variant dir 

                                                                           let context listMappingFunction = 
                                                                              {
                                                                                  listMappingFunction = listMappingFunction
                                                                                  dir = dir
                                                                                  list = list
                                                                              }   
                                                             
                                                                           match variant with
                                                                           | FutureValidity -> context List.map2 
                                                                           | _              -> context List.Parallel.map2

                                                                           //IO operation (data filtering (link * path) -> http request -> saving pdf files on HD)
                                                                           |> KODIS_Submain.downloadAndSave    
                                                                      ) 
                                                             Ok ()       
                                                         finally
                                                             closeConnection connection 
                                                     with 
                                                     | ex -> Error <| string ex.Message 
                                                     
                                                     |> function
                                                         | Ok value  -> 
                                                                      value  
                                                         | Error err ->
                                                                      logInfoMsg <| sprintf "Err050C %s" err
                                                                      closeItBaby msg16   

                                                     let param = next ()
                                                     interpret param

            | Free (EndProcessFM _)                 ->
                                                     try                                                         
                                                        let processEndTime = 
                                                             try    
                                                                 sprintf "Konec procesu: %s" <| DateTime.Now.ToString("HH:mm:ss")  
                                                             with
                                                             | ex ->       
                                                                   logInfoMsg <| sprintf "Err502 %s" (string ex.Message)
                                                                   "Konec procesu nemohl býti ustanoven."   
                                                             in msgParam7 processEndTime |> Ok
                                                     with 
                                                     | ex -> Error <| string ex.Message 
                                                     
                                                     |> function
                                                         | Ok value  -> 
                                                                      value  
                                                         | Error err ->
                                                                      logInfoMsg <| sprintf "Err050D %s" err
                                                                      closeItBaby msg16   
                                                                      
        cmdBuilder
            {
                let! _ = Free (StartProcessFM Pure)
                let! _ = Free (DownloadAndSaveJsonFM Pure)
                let! _ = Free (DownloadSelectedVariantFM Pure)

                return! Free (EndProcessFM Pure)
            } |> interpret  
        
        let endProcess = DateTime.Now

        try
            let connection = Database2.Connection.getConnection2 ()

            try
                insertLogEntries connection
                insertProcessTime connection [startProcess; endProcess]
            finally
                closeConnection connection 
        with ex -> () //zapis do log file neprovaden, to bych to mel za chvili plne....     