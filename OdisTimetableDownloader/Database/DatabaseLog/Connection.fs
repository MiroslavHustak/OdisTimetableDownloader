namespace Database2

open Microsoft.Data.SqlClient

//******************************

open Logging.Logging
open Helpers.CloseApp
open Settings.Messages

module Connection =    
              
    let internal getConnection2 () =  

        let connString2 = @"Data Source=Misa\SQLEXPRESS;Initial Catalog=Logging;Integrated Security=True;Encrypt=False"

        try
            let connection = new SqlConnection(connString2)
            connection.Open()
            Ok connection   
        with
        | ex -> Error <| string ex.Message
                        
        |> function
            | Ok value  -> 
                         value  
            | Error err ->
                         logInfoMsg <| sprintf "Err131A %s" err
                         closeItBaby msg16 
                         new SqlConnection(connString2)   

    let internal closeConnection (connection: SqlConnection) =  
        
        try
            try
                Ok <| connection.Close()                
            finally
                connection.Dispose()
        with
        | ex -> Error <| string ex.Message
                        
        |> function
            | Ok value  -> 
                         value  
            | Error err ->
                         logInfoMsg <| sprintf "Err132A %s" err
                         closeItBaby msg16  