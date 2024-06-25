namespace Database

open System
open Microsoft.Data.SqlClient

open FsToolkit.ErrorHandling

//*****************************

open Types

open Helpers
open Helpers.CloseApp

open Logging.Logging
open Settings.Messages

open DataModelling.Dto
open DataModelling.DataModel
open TransformationLayers.TransformationLayerGet

module Select =

    let internal select getConnection closeConnection pathToDir itvfCall =
        
        try
            let connection: SqlConnection = getConnection ()
                     
            try  
                //query je tady volani ITVF
                let query = sprintf "SELECT * FROM %s" itvfCall

                use cmdCallITVFunction = new SqlCommand(query, connection)          
                
                let reader = cmdCallITVFunction.ExecuteReader() 
                
                //V pripade pouziti Oracle zkontroluj skutecny typ sloupce v .NET   
                //let columnType = reader.GetFieldType(reader.GetOrdinal("OperatorID"))
                //printfn "Column Type: %s" columnType.Name
                  
                Seq.initInfinite (fun _ -> reader.Read() && reader.HasRows = true)
                |> Seq.takeWhile ((=) true) 
                |> Seq.collect
                    (fun _ -> 
                            seq
                                {
                                    let indexCompleteLink = reader.GetOrdinal("CompleteLink")
                                    let indexFileToBeSaved = reader.GetOrdinal("FileToBeSaved")

                                    let record : DbDtoGet = 
                                        {
                                            completeLink = reader.GetString(indexCompleteLink) |> Option.ofNullEmpty
                                            fileToBeSaved = reader.GetString(indexFileToBeSaved) |> Option.ofNullEmpty
                                        }

                                    yield record    
                                }
                    ) 
                |> List.ofSeq  
                |> List.map 
                    (fun record -> 
                                 let result = dbDataTransformLayerGet record

                                 let link = result.completeLink |> function CompleteLinkOpt value -> value
                                 let file = result.fileToBeSaved |> function FileToBeSavedOpt value -> value

                                 (link, file)
                                 |> function
                                     | Some link, Some file 
                                         -> 
                                          Ok (link, file)
                                     | _                   
                                         ->
                                          //failwith msg18 
                                          Error msg18
                    )
                |> Result.sequence
                |> function   
                    | Ok value  -> 
                                 value
                    | Error err -> 
                                 logInfoMsg <| sprintf "Err019 %s" err
                                 closeItBaby err
                                 []
            finally
                closeConnection connection 
        with
        | ex -> 
              logInfoMsg <| sprintf "Err020 %s" (string ex.Message)
              closeItBaby msg18             
              []