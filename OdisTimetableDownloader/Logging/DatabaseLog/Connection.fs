namespace Logging

open System
open Microsoft.Data.SqlClient

//******************************

open Logging.Logging
open Settings.Messages

module Connection =   
    
    let private closeItBaby err = 

        printfn "\nChyba při zápisu hodnot z logfile do DB. Zmáčkni cokoliv pro ukončení programu a mrkni se na problém. Popis chyby: %s" err     
        Console.ReadKey() |> ignore 
        System.Environment.Exit 1 

    let getConnectionAsync2 () =

        let connString2 = @"Data Source=Misa\SQLEXPRESS;Initial Catalog=Logging;Integrated Security=True;Encrypt=False"

        async
            {
                try          
                    let connection = new SqlConnection(connString2)
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
                             logInfoMsg <| sprintf "Err131B %s" err
                             closeItBaby msg16 
                             new SqlConnection(connString2)   

    let internal closeConnectionAsync2 (connection: SqlConnection) =  

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
                             logInfoMsg <| sprintf "Err132B %s" err
                             closeItBaby msg16  
       
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

    let internal closeConnection2 (connection: SqlConnection) =  
        
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