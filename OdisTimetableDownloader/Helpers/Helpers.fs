namespace Helpers
   
module ConsoleFixers = 
   
    open System

    let internal consoleAppProblemFixer () = 

        try            
            Ok <| System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance)         
        with
        | ex -> Error (string ex.Message) 
        
        |> function
            | Ok value -> value
            | Error err -> printfn "Err123 %s" err         
      
    let internal consoleWindowSettings () =
        
        //let primaryScreen = Screen.PrimaryScreen 
        //let screenWidth = primaryScreen.Bounds.Width //px
        //let screenHeight = primaryScreen.Bounds.Height //px

        let screenWidth = float32 Console.LargestWindowWidth //number of character columns
        let screenHeight = float32 Console.LargestWindowHeight //number of character rows

        //Console window settings
        Console.BackgroundColor <- ConsoleColor.Blue 
        Console.ForegroundColor <- ConsoleColor.White 
        Console.InputEncoding   <- System.Text.Encoding.Unicode
        Console.OutputEncoding  <- System.Text.Encoding.Unicode
        Console.WindowWidth     <- int (screenWidth / 1.8F)
        Console.WindowHeight    <- int (screenHeight / 1.8F)

module LogicalAliases =      

    let internal xor a b = (a && not b) || (not a && b)   
        
    (*
    let rec internal nXor operands =
        match operands with
        | []    -> false  
        | x::xs -> (x && not (nXor xs)) || ((not x) && (nXor xs))
    *)

    [<TailCall>]
    let internal nXor operands =
        let rec nXor_tail_recursive acc operands =
            match operands with
            | []    -> acc
            | x::xs -> nXor_tail_recursive ((x && not acc) || ((not x) && acc)) xs
        nXor_tail_recursive false operands

module CopyingOrMovingFiles = //output in Result type 

    open System.IO
    
    open Helpers
    open Helpers.Builders
         
    let private processFile source destination action =
                         
        pyramidOfDoom 
            {
                let! sourceFilepath = Path.GetFullPath(source) |> Option.ofNullEmpty, Error <| sprintf "Chyba při čtení cesty k %s" source
                let! destinFilepath = Path.GetFullPath(destination) |> Option.ofNullEmpty, Error <| sprintf "Chyba při čtení cesty k %s" destination
                let fInfodat: FileInfo = new FileInfo(sourceFilepath)  
                let! _ = fInfodat.Exists |> Option.ofBool, Error <| sprintf "Zdrojový soubor %s neexistuje" sourceFilepath 
                let dInfodat: DirectoryInfo = new DirectoryInfo(destinFilepath) //Overit vhodnost pred pouzitim
                let! _ = dInfodat.Exists |> Option.ofBool, Error <| sprintf "Destinační adresář %s neexistuje" destinFilepath  //Overit vhodnost pred pouzitim
                                    
                return Ok <| action sourceFilepath destinFilepath
            }           

    let internal copyFiles source destination overwrite =
        try
            let action sourceFilepath destinFilepath = 
                File.Copy(sourceFilepath, destinFilepath, overwrite) 
                in 
                processFile source destination action           
        with
        | ex -> Error (string ex.Message) 
        
        |> function
            | Ok value -> value
            | Error _  -> printfn "Err022 Chyba při kopírování souboru %s do %s" source destination     

   
            
    let internal moveFiles source destination overwrite =
        try
            let action sourceFilepath destinFilepath = File.Move(sourceFilepath, destinFilepath, overwrite) 
                in 
                processFile source destination action
        with
        | ex -> Error (string ex.Message) 
        
        |> function
            | Ok value -> value
            | Error _  -> printfn "Err023 Chyba při přemísťování souboru %s do %s" source destination 
    
module CopyingOrMovingFilesFreeMonad =   //not used yet  

    open System
    open System.IO
    
    open CloseApp    
    
    open Helpers
    open Helpers.Builders
    open Helpers.FreeMonadsCM

    //Free monads are just a general way of turning functors into monads.
    //A free monad is a sequence of actions where subsequent actions can depend on the result of previous ones.
        
    [<Struct>]
    type private Config =
        {
            source: string
            destination: string
            fileName: string
        }

    [<Struct>]
    type private IO = 
        | Copy
        | Move 
    
    [<TailCall>]
    let rec private interpret config io clp = //[<TailCall>] for testing purposes
    //let rec private interpret config io = //[<TailCall>] for testing purposes

        let source = config.source
        let destination = config.destination

        let msg = sprintf "Chyba %s při čtení cesty " 
        
        let result path1 path2 = 
            match path1 with
            | Ok path1  -> 
                         path1
            | Error err -> 
                         closeItBaby (sprintf "%s%s" err path2) 
                         String.Empty

        let f = 
            match io with
            | Copy -> fun p1 p2 -> File.Copy(p1, p2, true) //(fun _ _ -> ())           
            | Move -> fun p1 p2 -> File.Move(p1, p2, true) //(fun _ _ -> ())      
        
        match clp with 
        //function //CommandLineProgram<unit> -> unit //warning FS3569
        | Pure x                     ->
                                      x

        | Free (SourceFilepath next) ->
                                      let sourceFilepath source =                                        
                                          pyramidOfDoom
                                              {
                                                  let! value = Path.GetFullPath(source) |> Option.ofNullEmpty, Error <| msg "č.2"   
                                                  let! value = 
                                                      (
                                                          let fInfodat: FileInfo = new FileInfo(value)   
                                                          Option.fromBool value fInfodat.Exists
                                                      ), Error <| msg "č.1"
                                                  return Ok value
                                              }    
                                              
                                      let param = next (result (sourceFilepath source) source) 
                                      interpret config io param 

        | Free (DestinFilepath next) ->
                                      let destinFilepath destination =                                        
                                          pyramidOfDoom
                                              {
                                                  let! value = Path.GetFullPath(destination) |> Option.ofNullEmpty, Error <| msg "č.4"    
                                                  let! value = 
                                                      (
                                                          let dInfodat: DirectoryInfo = new DirectoryInfo(value)   
                                                          Option.fromBool value dInfodat.Exists
                                                      ), Error <| msg "č.3"
                                                  return Ok value
                                              }   

                                      let param = next (result (destinFilepath destination) destination)    
                                      interpret config io param
                                      //next (result (destinFilepath destination) destination) |> interpret config io  //error FS0251 expected 

        | Free (CopyOrMove (s, _))   -> 
                                      let (sourceFilepath, destinFilepath) = s
                                      f sourceFilepath destinFilepath 
                                      //interpret config next 
    
    let private config = 
        {
            source = @"e:\UVstarterLog\log.txt" //kontrola s FileInfo
            destination = @"e:\UVstarterLog\test\" //kontrola s DirectoryInfo
            fileName = "test.txt"
        }   

    let private copyOrMoveFiles config io =
        
        cmdBuilder 
            {
                let! sourceFilepath = Free (SourceFilepath Pure)                
                let! destinFilepath = Free (DestinFilepath Pure) 

                return! Free (CopyOrMove ((sourceFilepath, sprintf "%s%s" (destinFilepath) config.fileName), Pure ()))
            } |> interpret config io

    let copyFiles () = copyOrMoveFiles config Copy
    let moveFiles () = copyOrMoveFiles config Move
       
module MyString = //priklad pouziti: createStringSeq(8, "0")//tuple a compiled nazev velkym kvuli DLL pro C#        
        
    open System
    
    [<CompiledName "CreateStringAcc">]      
    let internal createStringAcc (strSeqNum: int, stringToAdd: string): string = 
        
        let initialString = String.Empty   //initial value of the string
        let listRange = [ 1 .. strSeqNum ] 

        //[<TailCall>] Ok
        let rec loop list acc =
            match list with 
            | []        ->
                         acc
            | _ :: tail -> 
                         let finalString = (+) acc stringToAdd  
                         loop tail finalString  //Tail-recursive function calls that have their parameters passed by the pipe operator are not optimized as loops #6984
    
        loop listRange initialString

    //Continuation-Passing Style (CPS)
    [<CompiledName "CreateStringCps">]
    let internal createStringCps (strSeqNum: int, stringToAdd: string): string =

        let listRange = [ 1 .. strSeqNum ]
        
        //[<TailCall>] Ok
        let rec loop list cont =
            match list with
            | []        -> cont String.Empty
            | _ :: tail -> loop tail (fun acc -> cont (stringToAdd + acc))
    
        loop listRange id
                  
    //List.reduce nelze, tam musi byt stejny typ acc a range      
    [<CompiledName "CreateStringSeqFold">] 
    let internal createStringSeqFold (strSeqNum: int, stringToAdd: string): string =

        [1 .. strSeqNum]
        |> List.fold (fun acc i -> (+) acc stringToAdd) String.Empty
                  
module CheckNetConnection =  

    open System.Net.NetworkInformation
   
    open Helpers
      
    let internal checkNetConn (timeout : int) =                 
       
        try
            use myPing = new Ping()      
                
            let host: string = "8.8.4.4" //IP google.com
            let buffer: byte[] = Array.zeroCreate <| 32
            
            let pingOptions: PingOptions = new PingOptions()                
     
            myPing.Send(host, timeout, buffer, pingOptions)
            |> (Option.ofNull >> Option.bind 
                    (fun pingReply -> 
                                    Option.fromBool
                                        (pingReply |> ignore) 
                                        ((=) pingReply.Status IPStatus.Success)                                           
                    )
               ) 
        with
        | ex -> None   