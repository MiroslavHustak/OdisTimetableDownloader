namespace Database2

open Microsoft.Data.SqlClient

open Logging.Logging
open Helpers.CloseApp
open Settings.Messages

module Connection =    
              
    let internal getConnection2 () =  

        let connString2 = @"Data Source=Misa\SQLEXPRESS;Initial Catalog=Logging;Integrated Security=True;Encrypt=False"

        try
            let connection = new SqlConnection(connString2)
            connection.Open()
            connection
        with
        | ex ->
              logInfoMsg <| sprintf "Err131A %s" (string ex.Message) 
              closeItBaby msg16 
              new SqlConnection(connString2)     

    let internal closeConnection (connection: SqlConnection) =  
        
        try
            try
                connection.Close()                
            finally
                connection.Dispose()
        with
        | ex -> 
              logInfoMsg <| sprintf "Err132A %s" (string ex.Message) 
              closeItBaby msg16  