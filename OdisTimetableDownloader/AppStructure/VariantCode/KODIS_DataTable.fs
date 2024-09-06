namespace MainFunctions

open System

//****************************

open Types
open Types.Types

open Settings.Messages
open Settings.SettingsKODIS
open Settings.SettingsGeneral

open Logging.Logging
    
open Helpers.CloseApp  
open Helpers.CommandLineWorkflow 

open SubmainFunctions
open SubmainFunctions.KODIS_SubmainDataTable

module WebScraping_KODISFMDataTable = 
    
    //FREE MONAD 
    //Free monads are just a general way of turning functors into monads.
    //A free monad is a sequence of actions where subsequent actions can depend on the result of previous ones.

    let internal webscraping_KODISFMDataTable pathToDir (variantList: Validity list) = 
            
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
                                                                 Ok (sprintf "Začátek procesu: %s" <| DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"))  
                                                             with
                                                             | ex -> Error <| string ex.Message
                                                                             
                                                             |> function
                                                                 | Ok value  -> 
                                                                              value  
                                                                 | Error err ->
                                                                              logInfoMsg <| sprintf "Err503 %s" err
                                                                              "Začátek procesu nemohl býti ustanoven."      
                                                             in msgParam7 processStartTime |> Ok
                                                     with
                                                     | ex -> Error <| string ex.Message
                                                                     
                                                     |> function
                                                         | Ok value  -> 
                                                                      value  
                                                         | Error err ->
                                                                      logInfoMsg <| sprintf "Err049A %s" err
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

                                                         KODIS_SubmainDataTable.downloadAndSaveJson (jsonLinkList @ jsonLinkList2) (pathToJsonList @ pathToJsonList2) 
                                                         ////KODIS_SubmainDataTable.downloadAndSaveJson jsonLinkList2 pathToJsonList2 
                                                         
                                                         msg3 ()   
                                                         msg11 () 
                                                         
                                                         Ok ()
                                                         
                                                     with
                                                     | ex -> Error <| string ex.Message
                                                                     
                                                     |> function
                                                         | Ok value  -> 
                                                                      value  
                                                         | Error err ->
                                                                      logInfoMsg <| sprintf "Err049B %s" err
                                                                      closeItBaby msg16

                                                     let param = next ()
                                                     interpret param                                                
                                                
            | Free (DownloadSelectedVariantFM next) -> 
                                                     try
                                                        let dt = DataTable.CreateDt.dt()   
                                                        
                                                        try
                                                            match variantList |> List.length with
                                                            //SingleVariantDownload
                                                            | 1 -> 
                                                                 let variant = variantList |> List.head

                                                                 //IO operation
                                                                 KODIS_SubmainDataTable.deleteOneODISDirectory variant pathToDir 
                                                                                                                           
                                                                 //operation on data 
                                                                 let dirList =                                                                    
                                                                     KODIS_SubmainDataTable.createOneNewDirectoryPath  //list -> aby bylo mozno pouzit funkci createFolders bez uprav  
                                                                     <| pathToDir 
                                                                     <| KODIS_SubmainDataTable.createDirName variant listODISDefault4 

                                                                 //IO operation 
                                                                 KODIS_SubmainDataTable.createFolders dirList

                                                                 msg10 () 

                                                                 let dir = (dirList |> List.head)

                                                                 //operation on data 
                                                                 //input from saved json files -> change of input data -> output into seq -> input from seq -> change of input data -> output into datatable -> data filtering (link*path)  
                                                                 let list = KODIS_SubmainDataTable.operationOnDataFromJson () dt variant dir 

                                                                 let context listMappingFunction = 
                                                                     {
                                                                         listMappingFunction = listMappingFunction
                                                                         dir = dir
                                                                         list = list 
                                                                     }                                                                
                                                               
                                                                 match list.Length >= 8 with //eqv of 8 threads
                                                                 | true  -> context List.Parallel.map2
                                                                 | false -> context List.map2  

                                                                 //IO operation (data filtering (link*path) -> http request -> saving pdf files on HD)
                                                                 |> KODIS_SubmainDataTable.downloadAndSave 

                                                             //BulkVariantDownload       
                                                            | _ ->
                                                                 //IO operation
                                                                 KODIS_SubmainDataTable.deleteAllODISDirectories pathToDir                                                              
                                                              
                                                                 //operation on data 
                                                                 let dirList = KODIS_SubmainDataTable.createNewDirectoryPaths pathToDir listODISDefault4
                                                              
                                                                 //IO operation 
                                                                 KODIS_SubmainDataTable.createFolders dirList 

                                                                 msg10 ()
                                                                
                                                                 (variantList, dirList)
                                                                 ||> List.iter2 
                                                                     (fun variant dir 
                                                                         -> 
                                                                          //operation on data 
                                                                          //input from saved json files -> change of input data -> output into seq -> input from seq -> seq of input data -> output into datatable -> data filtering (link*path)  
                                                                          let list = KODIS_SubmainDataTable.operationOnDataFromJson () dt variant dir 

                                                                          let context listMappingFunction = 
                                                                              {
                                                                                  listMappingFunction = listMappingFunction
                                                                                  dir = dir
                                                                                  list = list
                                                                              }                                                 
                                                  
                                                                          match variant with
                                                                          | FutureValidity -> context List.map2 
                                                                          | _              -> context List.Parallel.map2 

                                                                          //IO operation (data filtering (link*path) -> http request -> saving pdf files on HD)
                                                                          |> KODIS_SubmainDataTable.downloadAndSave 
                                                                     )  
                                                            Ok ()       
                                                        finally
                                                            dt.Dispose()
                                                     with 
                                                     | ex -> Error <| string ex.Message 
                                                     
                                                     |> function
                                                         | Ok value  -> 
                                                                      value  
                                                         | Error err ->
                                                                      logInfoMsg <| sprintf "Err049C %s" err
                                                                      closeItBaby msg16           

                                                     let param = next ()
                                                     interpret param

            | Free (EndProcessFM _)                 ->
                                                     try 
                                                         let processEndTime = 
                                                             try Ok (sprintf "Konec procesu: %s" <| DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"))                                                                    
                                                             with ex -> Error <| string ex.Message
                                                                             
                                                             |> function
                                                                 | Ok value  -> 
                                                                              value  
                                                                 | Error err ->
                                                                              logInfoMsg <| sprintf "Err504 %s" err
                                                                              sprintf "Konec procesu nemohl býti ustanoven." 
                                                             in msgParam7 processEndTime |> Ok
                                                     with
                                                     | ex -> Error <| string ex.Message
                                                                     
                                                     |> function
                                                         | Ok value  -> 
                                                                      value  
                                                         | Error err ->
                                                                      logInfoMsg <| sprintf "Err049D %s" err
                                                                      closeItBaby msg16
        cmdBuilder
            {
                let! _ = Free (StartProcessFM Pure)
                let! _ = Free (DownloadAndSaveJsonFM Pure)
                let! _ = Free (DownloadSelectedVariantFM Pure)

                return! Free (EndProcessFM Pure)
            } 
            
        |> interpret 