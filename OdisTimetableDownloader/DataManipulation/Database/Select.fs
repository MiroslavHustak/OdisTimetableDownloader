namespace Database

open System
open FSharp.Control
open FsToolkit.ErrorHandling
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

    let internal selectAsync (connection : SqlConnection) pathToDir itvfCall = //jen jako template pro jine app, v konzolove aplikaci to nema zrejme vyznam
        
        async
            {
                try
                    //In SQL Server, table names (including inline table-valued function calls like dbo.ITVF_GetLinksCurrentValidity()) cannot be parameterized.
                    //To prevent SQL injection, itvfCall shall be a trusted, hard-coded string (not user input or data that can be manipulated by an attacker).
                    
                    let query = sprintf "SELECT * FROM %s" itvfCall  
                    use cmdCallITVFunction = new SqlCommand(query, connection)
    
                    let! reader = cmdCallITVFunction.ExecuteReaderAsync() |> Async.AwaitTask
    
                    try
                        let records = 
                            ()
                            |> AsyncSeq.unfoldAsync //The generator is repeatedly called to build the list until it returns None.
                                (fun () -> 
                                         async 
                                             {
                                                 let! successfullyRead = reader.ReadAsync() |> Async.AwaitTask

                                                 match successfullyRead with
                                                 | true  ->
                                                          let indexCompleteLink = reader.GetOrdinal "CompleteLink"
                                                          let indexFileToBeSaved = reader.GetOrdinal "FileToBeSaved"
                                    
                                                          let record : DbDtoGet = 
                                                              {
                                                                  CompleteLink = reader.GetString indexCompleteLink |> Option.ofNullEmpty
                                                                  FileToBeSaved = reader.GetString indexFileToBeSaved |> Option.ofNullEmpty
                                                              }
                                                          return Some (record, ())
                                                 | false ->
                                                          return None
                                             }
                                ) 
    
                        let! results = 
                            records
                            |> AsyncSeq.map 
                                (fun record -> 
                                             let result = dbDataTransformLayerGet record
                                             let link = result.CompleteLink |> function CompleteLinkOpt value -> value
                                             let file = result.FileToBeSaved |> function FileToBeSavedOpt value -> value
    
                                             match link, file with
                                             | Some link, Some file
                                                  -> 
                                                   Ok (CompleteLink link, FileToBeSaved file) //TDD provedeno takto slozite pouze quli kompatabilite se zmenenym DataTable for testing purposes 
                                             | _  -> 
                                                   Error msg18
                                )
                            |> AsyncSeq.toListAsync // Accumulate results asynchronously
    
                        return results |> Result.sequence
    
                    finally
                        reader.Dispose() 
    
                with
                | ex -> return Error <| string ex.Message
            }
            |> Async.Catch
            |> Async.RunSynchronously  //musi byt, takze async tady nema vyznam, ale jakozto template se to hodi
            |> Result.ofChoice    
            |> Result.map
                (fun value -> 
                            value 
                            |> Result.defaultWith (fun err ->
                                logInfoMsg <| sprintf "Err020A %s" err
                                closeItBaby msg18
                                []  
                            )
                )
            |> Result.defaultWith 
                (fun ex ->
                         logInfoMsg <| sprintf "Err020B %s" (string ex.Message)
                         closeItBaby msg18
                         []  
                )

    let internal select (connection : SqlConnection) pathToDir itvfCall =
        
        //jeste jeden try-with blok je treba quli connection (viz moje SAFE STACK app), ale tady to nestoji za tu namahu
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
                                    let indexCompleteLink = reader.GetOrdinal "CompleteLink"
                                    let indexFileToBeSaved = reader.GetOrdinal "FileToBeSaved"

                                    let record : DbDtoGet = 
                                        {
                                            CompleteLink = reader.GetString indexCompleteLink |> Option.ofNullEmpty
                                            FileToBeSaved = reader.GetString indexFileToBeSaved |> Option.ofNullEmpty
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

        with
        | ex -> Error <| string ex.Message
                            
        |> function
            | Ok value  -> 
                         value  
            | Error err ->
                         logInfoMsg <| sprintf "Err020 %s" err
                         closeItBaby msg18             
                         []       