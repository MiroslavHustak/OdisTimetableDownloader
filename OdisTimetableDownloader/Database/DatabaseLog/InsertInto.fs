namespace Database2

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

    let internal insertLogEntries getConnection2 closeConnection =

        let dataToBeInserted = Database2.LogFileData.extractLogEntries () 

        match dataToBeInserted.Length with
        | 0 -> 
             ()
        | _ ->              
             //https://learn.microsoft.com/en-us/sql/t-sql/statements/merge-transact-sql?view=sql-server-ver16
             //v tabulce budou jen nove hodnoty - hodnoty, ktere tam uz jsou, se z logu nebudou znovu nacitat         
             let queryInsert =        
                 "
                 MERGE INTO LogEntries2 AS target
                 USING (VALUES (@Timestamp, @Logname, @Message))
                 AS source ([Timestamp], Logname, [Message])
                 ON target.[Timestamp] = source.[Timestamp]
                     AND target.Logname = source.Logname
                     AND target.[Message] = source.[Message]
                 WHEN MATCHED THEN
                 UPDATE SET target.[Timestamp] = source.[Timestamp],
                    target.Logname = source.Logname,
                    target.[Message] = source.[Message]
                 WHEN NOT MATCHED BY target THEN
                    INSERT ([Timestamp], Logname, [Message])
                    VALUES (source.[Timestamp], source.Logname, source.[Message]);                 
                " 

             let queryInsert1 =  //nepouzivano               
                 "
                 INSERT INTO LogEntries2 ([Timestamp], Logname, [Message])
                 VALUES (@Timestamp, @Logname, @Message)                 
                "  
                 
             try
                 let isolationLevel = System.Data.IsolationLevel.Serializable 

                 let connection: SqlConnection = getConnection2 ()
                 let transaction: SqlTransaction = connection.BeginTransaction(isolationLevel) 

                 Ok (connection, transaction)
             with
             | ex ->
                   Error ex

             |> function
                 | Ok value ->                      
                             try
                                 let connection, transaction = value

                                 use cmdInsert = new SqlCommand(queryInsert, connection, transaction)

                                 try                        
                    
                                     let parameterTimeStamp = new SqlParameter()                 
                                     parameterTimeStamp.ParameterName <- "@Timestamp"  
                                     parameterTimeStamp.SqlDbType <- SqlDbType.DateTime  
                    
                                     dataToBeInserted     
                                     |> List.map
                                         (fun item -> 
                                                    let (timestamp, logName, message) = item 
                                    
                                                    let timestamp = 
                                                        try 
                                                            DateTime.ParseExact(timestamp, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture) 
                                                        with
                                                        | :? System.ArgumentNullException as _ -> DateTime.MinValue //TODO pokud mne neco napadne, co lepsiho tady dat                                                                                                                                                                       
                                                        | :? System.FormatException as _       -> DateTime.MinValue //TODO pokud mne neco napadne, co lepsiho tady dat
                                                        | _                                    -> DateTime.MinValue //TODO pokud mne neco napadne, co lepsiho tady dat
                                    
                                                    cmdInsert.Parameters.Clear() // Clear parameters for each iteration    
                                                    parameterTimeStamp.Value <- timestamp                             
                                                    cmdInsert.Parameters.Add(parameterTimeStamp) |> ignore    

                                                    cmdInsert.Parameters.AddWithValue("@Logname", logName) |> ignore
                                                    cmdInsert.Parameters.AddWithValue("@Message", message) |> ignore
                                              
                                                    cmdInsert.ExecuteNonQuery() > 0   //number of affected rows                                                           
                                        )                                       
                                     |> List.contains false   
                                     |> function
                                         | true  -> transaction.Rollback()  
                                         | false -> transaction.Commit()  
                     
                                     msg19 ()   
                                      
                                 finally
                                     transaction.Dispose()
                                     closeConnection connection 
                             with
                             | ex ->
                                   msgParam1 <| string ex.Message
                                   logInfoMsg <| sprintf "Err101 %s" (string ex.Message)
                                   closeItBaby (string ex.Message)
                 | Error ex ->
                             msgParam1 <| string ex.Message
                             logInfoMsg <| sprintf "Err101X %s" (string ex.Message)
                             closeItBaby (string ex.Message)


    let internal insertProcessTime getConnection2 closeConnection (dataToBeInserted : DateTime list) =    
    
            match dataToBeInserted.Length with
            | 0 -> 
                 ()
            | _ -> 
                 let queryInsert =               
                     "
                     INSERT INTO ProcessTime ([Start], [End])
                     VALUES (@Start, @End)                 
                    "  

                 try
                     let isolationLevel = System.Data.IsolationLevel.Serializable 

                     let connection: SqlConnection = getConnection2 () 
                     let transaction: SqlTransaction = connection.BeginTransaction(isolationLevel) 

                     Ok (connection, transaction)
                 with
                 | ex ->
                       Error ex

                 |> function
                     | Ok value ->                      
                                 try
                                     let connection, transaction = value

                                     use cmdInsert = new SqlCommand(queryInsert, connection, transaction)   
                                     
                                     try                        
                        
                                         let parameterStart = new SqlParameter()                 
                                         parameterStart.ParameterName <- "@Start"  
                                         parameterStart.SqlDbType <- SqlDbType.DateTime  

                                         let parameterEnd = new SqlParameter()                 
                                         parameterEnd.ParameterName <- "@End"  
                                         parameterEnd.SqlDbType <- SqlDbType.DateTime  
                                         
                                         cmdInsert.Parameters.Clear() // Clear parameters for each iteration    
                        
                                         parameterStart.Value <- List.item 0 dataToBeInserted                             
                                         cmdInsert.Parameters.Add(parameterStart) |> ignore   
                                         parameterEnd.Value <- List.item 1 dataToBeInserted                             
                                         cmdInsert.Parameters.Add(parameterEnd) |> ignore    
                                                                              
                                         cmdInsert.ExecuteNonQuery() > 0 //number of affected rows   
                                         |> function                                             
                                             | true  -> transaction.Commit()  
                                             | false -> transaction.Rollback()  

                                         msg25 ()   
                                          
                                     finally
                                         transaction.Dispose()
                                         closeConnection connection 
                                 with
                                 | ex ->
                                       msgParam1 <| string ex.Message
                                       logInfoMsg <| sprintf "Err101A %s" (string ex.Message)
                                       closeItBaby (string ex.Message)

                     | Error ex ->
                                 msgParam1 <| string ex.Message
                                 logInfoMsg <| sprintf "Err101AX %s" (string ex.Message)
                                 closeItBaby (string ex.Message)