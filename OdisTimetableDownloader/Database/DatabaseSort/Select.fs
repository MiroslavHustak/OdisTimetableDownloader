namespace Database

open System
open Microsoft.Data.SqlClient

//*****************************

open MyFsToolkit

//*****************************

open Types

open Logging.Logging
open Helpers.CloseApp
open Settings.Messages

open DataModelling.Dto
open DataModelling.DataModel
open TransformationLayers.TransformationLayerGet

module Select =

    let internal select (connection : SqlConnection) pathToDir itvfCall =
        
        try  
            //query je tady volani ITVF
            let query = sprintf "SELECT * FROM %s" itvfCall

            use cmdCallITVFunction = new SqlCommand(query, connection)          
            
            use reader = cmdCallITVFunction.ExecuteReader() 

            try   

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
                                            CompleteLink = reader.GetString(indexCompleteLink) |> Option.ofNullEmpty
                                            FileToBeSaved = reader.GetString(indexFileToBeSaved) |> Option.ofNullEmpty
                                        }

                                    yield record    
                                }
                    ) 
                |> List.ofSeq  
                |> List.map 
                    (fun record -> 
                                 let result = dbDataTransformLayerGet record

                                 let link = result.CompleteLink |> function CompleteLinkOpt value -> value
                                 let file = result.FileToBeSaved |> function FileToBeSavedOpt value -> value

                                 (link, file)
                                 |> function
                                     | Some link, Some file 
                                         -> 
                                          Ok (CompleteLink link, FileToBeSaved file) //TDD provedeno takto slozite pouze quli kompatabilite se zmenenym DataTable for testing purposes 
                                     | _                   
                                         ->
                                          //failwith msg18 
                                          Error msg18
                    )
                |> Result.sequence
               
            finally
                ()
                // reader.Dispose()

        with ex -> Error <| string ex.Message
                            
        |> function
            | Ok value  -> 
                         value  
            | Error err ->
                         logInfoMsg <| sprintf "Err020 %s" err
                         closeItBaby msg18             
                         []  