namespace Database

open System
open System.Data
open System.Globalization
open Microsoft.Data.SqlClient

open FsToolkit.ErrorHandling

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
            let connection: SqlConnection = getConnection () 
            
            try                 
                use cmdDeleteAll = new SqlCommand(queryDeleteAll, connection)             
                use cmdInsert = new SqlCommand(queryInsert, connection)   
                
                let parameterStart = new SqlParameter()                 
                parameterStart.ParameterName <- "@StartDate"  
                parameterStart.SqlDbType <- SqlDbType.Date  

                let parameterEnd = new SqlParameter() 
                parameterEnd.ParameterName <- "@EndDate"  
                parameterEnd.SqlDbType <- SqlDbType.Date  

                cmdDeleteAll.ExecuteNonQuery() |> ignore //number of affected rows
                
                dataToBeInserted     
                |> List.iter
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
                           
                               cmdInsert.ExecuteNonQuery() |> ignore //number of affected rows                               
                    )                
            finally
                closeConnection connection 
        with
        | ex ->
              msgParam1 <| string ex.Message
              logInfoMsg <| sprintf "Err033 %s" (string ex.Message)
              closeItBaby (string ex.Message)