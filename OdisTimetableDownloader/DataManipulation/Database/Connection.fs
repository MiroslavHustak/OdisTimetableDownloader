namespace Database

open Microsoft.Data.SqlClient

//****************************

open Logging.Logging
open Helpers.CloseApp
open Settings.Messages

module Connection =   

    let getConnectionAsync () =

        let connString = @"Data Source=Misa\SQLEXPRESS;Initial Catalog=TimetableDownloader;Integrated Security=True;Encrypt=False"

        async
            {
                try          
                    let connection = new SqlConnection(connString)
                    do! connection.OpenAsync() |> Async.AwaitTask
                    return Ok connection   
                with 
                | ex -> return Error <| string ex.Message
            }
            |> Async.RunSynchronously
            |> function
                | Ok value  -> 
                             value  
                | Error err ->
                             logInfoMsg <| sprintf "Err031A %s" err
                             closeItBaby msg16 
                             new SqlConnection(connString)   

    let internal closeConnectionAsync (connection: SqlConnection) =  

        async
            {
                try
                    try
                        do! connection.CloseAsync() |> Async.AwaitTask
                        return Ok ()
                    finally
                        async 
                            {
                                do! connection.DisposeAsync().AsTask() |> Async.AwaitTask
                            } |> Async.StartImmediate                      
                with 
                | ex -> return Error <| string ex.Message
            }   
            |> Async.RunSynchronously            
            |> function
                | Ok value  -> 
                             value  
                | Error err ->
                             logInfoMsg <| sprintf "Err032A %s" err
                             closeItBaby msg16  

    let internal getConnection () =  
    
        let connString = @"Data Source=Misa\SQLEXPRESS;Initial Catalog=TimetableDownloader;Integrated Security=True;Encrypt=False"
    
        try
            let connection = new SqlConnection(connString)
            connection.Open()
            Ok connection             
        with 
        | ex -> Error <| string ex.Message
                            
        |> function
            | Ok value  -> 
                         value  
            | Error err ->
                         logInfoMsg <| sprintf "Err031 %s" err
                         closeItBaby msg16 
                         new SqlConnection(connString)   
    
    let internal closeConnection (connection : SqlConnection) =  
            
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
                         logInfoMsg <| sprintf "Err032 %s" err
                         closeItBaby msg16  