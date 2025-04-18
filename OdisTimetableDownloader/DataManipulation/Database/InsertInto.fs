﻿namespace Database

open System
open System.Data
open System.Globalization
open Microsoft.Data.SqlClient

//*******************************************

open MyFsToolkit.Casting
open MyFsToolkit.Builders
open MyFsToolkit.TryParserDate

//*******************************************

open Logging.Logging
open Helpers.CloseApp
open Settings.Messages

open DataModelling.Dto
open DataModelling.DataModel

module InsertInto =     

    //******************************** Sync variant ********************************
    //Template pro async je v mem SAFE Stack template, funkce insertOrUpdateAsync (connection: Async<Result<SqlConnection, string>>) ...

    let internal insert (connection : SqlConnection) (dataToBeInserted : DbDtoSend list) =
    
        let queryDeleteAll = "DELETE FROM TimetableLinks"

        let queryCount = "SELECT COUNT(*) FROM TimetableLinks"
   
        let queryInsert = 
            "           
            INSERT INTO TimetableLinks 
            (
                OldPrefix, NewPrefix, StartDate, EndDate, 
                TotalDateInterval,VT_Suffix, JS_GeneratedString, 
                CompleteLink, FileToBeSaved, PartialLink
            ) 
            VALUES
            (
                @OldPrefix, @NewPrefix, @StartDate, @EndDate, 
                @TotalDateInterval, @VT_Suffix, @JS_GeneratedString, 
                @CompleteLink, @FileToBeSaved, @PartialLink
            );
            "   
            
        try
            let isolationLevel = System.Data.IsolationLevel.Serializable //Transaction locking behaviour
            //System.Data.IsolationLevel.ReadCommitted
            //System.Data.IsolationLevel.RepeatableRead
            //System.Data.IsolationLevel.ReadUncommitted
                               
            let transaction : SqlTransaction = connection.BeginTransaction isolationLevel //Transaction to be implemented for all commands linked to the connection
                
            try 
                use cmdDeleteAll = new SqlCommand(queryDeleteAll, connection, transaction) 
                use cmdCount = new SqlCommand(queryCount, connection, transaction)

                cmdDeleteAll.ExecuteNonQuery() |> ignore
                                
                let rowCount = castAs<int> <| cmdCount.ExecuteScalar() 
                                                   
                let parameterStart = SqlParameter ()                 
                parameterStart.ParameterName <- "@StartDate"  
                parameterStart.SqlDbType <- SqlDbType.Date  
    
                let parameterEnd = SqlParameter () 
                parameterEnd.ParameterName <- "@EndDate"  
                parameterEnd.SqlDbType <- SqlDbType.Date  
    
                match rowCount with 
                | Some 0 ->                              
                          use cmdInsert = new SqlCommand(queryInsert, connection, transaction) 
                             
                          dataToBeInserted     
                          |> List.map
                              (fun item -> 
                                         (*   
                                         let (startDate, endDate) =   
    
                                             pyramidOfDoom
                                                 {
                                                     let! startDate = item.startDate, (DateTime.MinValue, DateTime.MinValue)                                                      
                                                     let! endDate = item.endDate, (DateTime.MinValue, DateTime.MinValue)                             
                                              
                                                     return (startDate, endDate)
                                                }
                                         *)
                                         cmdInsert.Parameters.Clear() // Clear parameters for each iteration     
                                         cmdInsert.Parameters.AddWithValue("@OldPrefix", item.OldPrefix) |> ignore
                                         cmdInsert.Parameters.AddWithValue("@NewPrefix", item.NewPrefix) |> ignore
    
                                         parameterStart.Value <- item.StartDate
                                         cmdInsert.Parameters.Add parameterStart |> ignore
    
                                         parameterEnd.Value <- item.EndDate                                
                                         cmdInsert.Parameters.Add parameterEnd |> ignore
    
                                         cmdInsert.Parameters.AddWithValue("@TotalDateInterval", item.TotalDateInterval) |> ignore
                                         cmdInsert.Parameters.AddWithValue("@VT_Suffix", item.Suffix) |> ignore
                                         cmdInsert.Parameters.AddWithValue("@JS_GeneratedString", item.JsGeneratedString) |> ignore
                                         cmdInsert.Parameters.AddWithValue("@CompleteLink", item.CompleteLink) |> ignore
                                         cmdInsert.Parameters.AddWithValue("@FileToBeSaved", item.FileToBeSaved) |> ignore 
                                         cmdInsert.Parameters.AddWithValue("@PartialLink", item.PartialLink) |> ignore 
                                                               
                                         let executeNonQuery = cmdInsert.ExecuteNonQuery() //kdyby nahodou, tak ... async { return! cmdInsert.ExecuteNonQueryAsync() |> Async.AwaitTask } 
                                         
                                         (>) executeNonQuery 0   //number of affected rows     
                              ) 
                          |> List.contains false
                          |> function
                              | true  -> Ok <| transaction.Rollback() 
                              | false -> Ok <| transaction.Commit() 
                | _      -> 
                          Error <| sprintf "Err033A %s" "Databázová tabulka není prázdná."          

            finally                              
                transaction.Dispose()     
                
        with
        | ex -> Error (string ex.Message)

        |> function   
            | Ok value  ->
                         value
            | Error err -> 
                         msgParam1 err
                         logInfoMsg <| sprintf "Err033 %s" err
                         closeItBaby err