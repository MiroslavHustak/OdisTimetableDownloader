namespace Database

open System
open System.Data
open System.Globalization
open Microsoft.Data.SqlClient

open FsToolkit.ErrorHandling

//******************************

open Settings.Messages

open Helpers.Builders
open Helpers.CloseApp
open Helpers.TryParserDate

open Logging.Logging

open DataModelling.Dto
open DataModelling.DataModel

module InsertInto = 

    let internal insert getConnection closeConnection (dataToBeInserted : DbDtoSend list) =
    
        let queryDeleteAll = "DELETE FROM TimetableLinks"
             
        let queryInsert = 
            "           
            INSERT INTO TimetableLinks 
            (
                OldPrefix, NewPrefix, StartDate, EndDate, 
                TotalDateInterval,VT_Suffix, JS_GeneratedString, 
                CompleteLink, FileToBeSaved
            ) 
            VALUES
            (
                @OldPrefix, @NewPrefix, @StartDate, @EndDate, 
                @TotalDateInterval, @VT_Suffix, @JS_GeneratedString, 
                @CompleteLink, @FileToBeSaved
            );
            "   
            
        try
            let isolationLevel = System.Data.IsolationLevel.Serializable //Transaction locking behaviour
            //System.Data.IsolationLevel.ReadCommitted
            //System.Data.IsolationLevel.RepeatableRead
            //System.Data.IsolationLevel.ReadUncommitted
                               
            let connection: SqlConnection = getConnection()
            let transaction: SqlTransaction = connection.BeginTransaction(isolationLevel) //Transaction to be implemented for all commands linked to the connection
                
            try 
                use cmdDeleteAll = new SqlCommand(queryDeleteAll, connection, transaction)                                
                                    
                let parameterStart = new SqlParameter()                 
                parameterStart.ParameterName <- "@StartDate"  
                parameterStart.SqlDbType <- SqlDbType.Date  
    
                let parameterEnd = new SqlParameter() 
                parameterEnd.ParameterName <- "@EndDate"  
                parameterEnd.SqlDbType <- SqlDbType.Date  
    
                match cmdDeleteAll.ExecuteNonQuery() > 0 with
                | false -> 
                         Ok <| transaction.Rollback() 
                | true  ->                                             
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
                                        cmdInsert.Parameters.AddWithValue("@OldPrefix", item.oldPrefix) |> ignore
                                        cmdInsert.Parameters.AddWithValue("@NewPrefix", item.newPrefix) |> ignore
    
                                        parameterStart.Value <- item.startDate
                                        cmdInsert.Parameters.Add(parameterStart) |> ignore
    
                                        parameterEnd.Value <- item.endDate                                
                                        cmdInsert.Parameters.Add(parameterEnd) |> ignore
    
                                        cmdInsert.Parameters.AddWithValue("@TotalDateInterval", item.totalDateInterval) |> ignore
                                        cmdInsert.Parameters.AddWithValue("@VT_Suffix", item.suffix) |> ignore
                                        cmdInsert.Parameters.AddWithValue("@JS_GeneratedString", item.jsGeneratedString) |> ignore
                                        cmdInsert.Parameters.AddWithValue("@CompleteLink", item.completeLink) |> ignore
                                        cmdInsert.Parameters.AddWithValue("@FileToBeSaved", item.fileToBeSaved) |> ignore   
                                                               
                                        cmdInsert.ExecuteNonQuery() > 0
                             ) 
                         |> List.contains false
                         |> function
                             | true  -> Ok <| transaction.Rollback() 
                             | false -> Ok <| transaction.Commit()  

            finally                              
                transaction.Dispose()
                closeConnection connection 
        with
        | ex -> Error (string ex.Message)

        |> function   
            | Ok value  ->
                         value
            | Error err -> 
                         msgParam1 err
                         logInfoMsg <| sprintf "Err033 %s" err
                         closeItBaby err