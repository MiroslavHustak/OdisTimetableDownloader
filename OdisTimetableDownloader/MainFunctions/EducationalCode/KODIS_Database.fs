namespace MainFunctions

open System
         
open Types

open Settings.Messages
open Settings.SettingsGeneral
    
open Logging.Logging

open Helpers.CloseApp  
open Helpers.FreeMonads  
        
open Database2.InsertInto
open Database2.Connection

open SubmainFunctions
open SubmainFunctions.KODIS_Submain

//******************************************************************************
//Do you review my SQL skills? If so, you are at the right place :-).
//If not, please direct your attention to code contained in KODIS_DataTable.fs.
//******************************************************************************

module WebScraping_KODISFM = 
        
    //FREE MONAD 

    let internal webscraping_KODISFM pathToDir (variantList: Validity list) = 

        let startProcess = DateTime.Now
            
        let rec interpret clp  = 

            let errorHandling fn = 
                try
                    fn
                with
                | ex ->
                      logInfoMsg <| sprintf "Err050 %s" (string ex.Message)
                      closeItBaby msg16           

            //function //CommandLineProgram<unit> -> unit
            match clp with
            | Pure x                                -> 
                                                     x //nevyuzito

            | Free (StartProcessFM next)            -> 
                                                     let processStartTime =    
                                                         Console.Clear()
                                                         let processStartTime = 
                                                             try   
                                                                 startNetChecking ()
                                                                 sprintf "Začátek procesu: %s" <| DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") 
                                                             with
                                                             | ex ->       
                                                                   logInfoMsg <| sprintf "Err503 %s" (string ex.Message)
                                                                   sprintf "Začátek procesu nemohl býti ustanoven."   
                                                             in msgParam7 processStartTime 
                                                         in errorHandling processStartTime

                                                     let param = next ()
                                                     interpret param

            | Free (DownloadAndSaveJsonFM next)     ->      
                                                     //Http request and IO operation (data from settings -> http request -> IO operation -> saving json files on HD)
                                                     //let downloadAndSaveJson =  
                                                        // startNetChecking ()
                                                        // KODIS_Submain.downloadAndSaveJson ()  
                                                        // in errorHandling downloadAndSaveJson

                                                     let param = next ()
                                                     interpret param                                                
                                                
            | Free (DownloadSelectedVariantFM next) -> 
                                                     let downloadSelectedVariant =                                                          
                                                         match variantList |> List.length with
                                                         //SingleVariantDownload
                                                         | 1 -> 
                                                              let variant = variantList |> List.head

                                                              //IO operation
                                                              KODIS_Submain.deleteOneODISDirectory variant pathToDir 
                                                              
                                                              //operation on data
                                                              let dirList =                                                                    
                                                                  KODIS_Submain.createOneNewDirectory  //list -> aby bylo mozno pouzit funkci createFolders bez uprav  
                                                                  <| pathToDir 
                                                                  <| KODIS_Submain.createDirName variant listODISDefault4 

                                                              //IO operation 
                                                              KODIS_Submain.createFolders dirList

                                                              //operation on data 
                                                              //input from saved json files -> change of input data -> output into array -> input from array -> change of input data -> output into database -> data filtering (link*path) 
                                                              KODIS_Submain.operationOnDataFromJson variant (dirList |> List.head) 

                                                              //IO operation (data filtering (link*path) -> http request -> saving pdf files on HD)
                                                              |> KODIS_Submain.downloadAndSave (dirList |> List.head) 

                                                         //BulkVariantDownload       
                                                         | _ ->  
                                                              //IO operation
                                                              KODIS_Submain.deleteAllODISDirectories pathToDir
                                                              
                                                              //operation on data 
                                                              let dirList = KODIS_Submain.createNewDirectories pathToDir listODISDefault4
                                                              
                                                              //IO operation 
                                                              KODIS_Submain.createFolders dirList 
                                                              
                                                              (variantList, dirList)
                                                              ||> List.iter2 
                                                                  (fun variant dir 
                                                                      -> 
                                                                       //operation on data 
                                                                       //input from saved json files -> change of input data -> output into array -> input from array -> change of input data -> output into database -> data filtering (link*path) 
                                                                       KODIS_Submain.operationOnDataFromJson variant dir 

                                                                       //IO operation (data filtering (link*path) -> http request -> saving pdf files on HD)
                                                                       |> KODIS_Submain.downloadAndSave dir   
                                                                  )     
                                                                                                             
                                                         in errorHandling downloadSelectedVariant  

                                                     let param = next ()
                                                     interpret param

            | Free (EndProcessFM _)                 ->
                                                     let processEndTime =                                                          
                                                        let processEndTime = 
                                                             try    
                                                                 sprintf "Konec procesu: %s" <| DateTime.Now.ToString("HH:mm:ss")  
                                                             with
                                                             | ex ->       
                                                                   logInfoMsg <| sprintf "Err502 %s" (string ex.Message)
                                                                   sprintf "Konec procesu nemohl býti ustanoven."   
                                                             in msgParam7 processEndTime
                                                         in errorHandling processEndTime
        cmdBuilder
            {
                let! _ = Free (StartProcessFM Pure)
                let! _ = Free (DownloadAndSaveJsonFM Pure)
                let! _ = Free (DownloadSelectedVariantFM Pure)

                return! Free (EndProcessFM Pure)
            } |> interpret  
        
        let endProcess = DateTime.Now

        insertLogEntries getConnection2 closeConnection
        insertProcessTime getConnection2 closeConnection [startProcess; endProcess]
        //*****************************************************************************************************************************************

        //CurrentValidity = JR striktne platne k danemu dni, tj. pokud je napr. na dany den vylukovy JR, stahne se tento JR, ne JR platny dalsi den
        //FutureValidity = JR platne v budouci dobe, ktere se uz vyskytuji na webu KODISu
        //ReplacementService = pouze vylukove JR, JR NAD a JR X linek
        //WithoutReplacementService = JR dlouhodobe platne bez jakykoliv vyluk. Tento vyber neobsahuje ani dlouhodobe nekolikamesicni vyluky, muze se ale hodit v pripade, ze zakladni slozka s JR obsahuje jedno ci dvoudenni vylukove JR.     