namespace Database

open Microsoft.Data.SqlClient

open Logging.Logging
open Helpers.CloseApp
open Settings.Messages

module Connection =    

    let internal getConnection () =  

        let connString = @"Data Source=Misa\SQLEXPRESS;Initial Catalog=TimetableDownloader;Integrated Security=True;Encrypt=False"

        try            
            let connection = new SqlConnection(connString)
            connection.Open()
            connection
        with
        | ex ->
              logInfoMsg <| sprintf "Err031 %s" (string ex.Message) 
              closeItBaby msg16 
              new SqlConnection(connString)     

    let internal closeConnection (connection: SqlConnection) =  
        
        try
            try
                connection.Close()                
            finally
                connection.Dispose()
        with
        | ex -> 
              logInfoMsg <| sprintf "Err032 %s" (string ex.Message) 
              closeItBaby msg16  