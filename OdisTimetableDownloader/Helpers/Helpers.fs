namespace Helpers

module CloseApp = 

    open System
    open Settings.Messages

    let internal closeItBaby err = 

        msgParam1 err      
        Console.ReadKey() |> ignore 
        System.Environment.Exit 1 

module FileInfoHelper = 

    open System
    open System.IO
    
    open Logging.Logging
    open Settings.Messages

    open MyFsToolkit
    open MyFsToolkit.Builders 

    let internal readAllText path msg = 

        pyramidOfDoom
            {
                //path je sice casto pod kontrolou a filepath nebude null, nicmene pro jistotu...  
                let! filepath = Path.GetFullPath path |> Option.ofNullEmpty, Error <| sprintf "Chyba při čtení cesty k %s" path
                                                
                let fInfoDat = FileInfo filepath
                let! _ = fInfoDat.Exists |> Option.ofBool, Error <| sprintf "Soubor %s neexistuje" filepath

                return Ok <| File.ReadAllText filepath                                           
            }    

        |> function
            | Ok value  -> 
                         value
            | Error err -> 
                         sprintf "Err777 %s" >> logInfoMsg <| msg
                         CloseApp.closeItBaby msg
                         String.Empty  

    let internal readAllTextAsync path msg : Async<string> = 

        pyramidOfDoom
            {   
                //path je sice casto pod kontrolou a filepath nebude null, nicmene pro jistotu...  
                let! filepath = Path.GetFullPath path |> Option.ofNullEmpty, Error <| sprintf "Chyba při čtení cesty k %s" path

                let fInfoDat = FileInfo filepath
                let! _ = fInfoDat.Exists |> Option.ofBool, Error <| sprintf "Soubor %s neexistuje" filepath

                return Ok (File.ReadAllTextAsync filepath |> Async.AwaitTask)                                          
            }  
            
        |> function
            | Ok value  -> 
                         value
            | Error err -> 
                         sprintf "Err777A %s" >> logInfoMsg <| msg
                         CloseApp.closeItBaby msg
                         async { return String.Empty }   

module ConsoleFixers =

    open System

    let internal consoleAppProblemFixer () = //tady je jistejsi try-with, neni mi jasne, ktery provider moze byt null

        try 
            Ok <| System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance)         
        with
        | ex -> Error (string ex.Message) 
        
        |> function
            | Ok value  -> value
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
        | []      -> false  
        | x :: xs -> (x && not (nXor xs)) || ((not x) && (nXor xs))
    *)

    //[<TailCall>]
    let internal nXor operands =
        let rec nXor_tail_recursive acc operands =
            match operands with
            | []      -> acc
            | x :: xs -> nXor_tail_recursive ((x && not acc) || ((not x) && acc)) xs
        nXor_tail_recursive false operands
       
module MyString = //priklad pouziti: createStringSeq(8, "0")//tuple a compiled nazev velkym kvuli DLL pro C#        
        
    open System
    
    [<CompiledName "CreateStringAcc">]      
    let internal createStringAcc (strSeqNum : int, stringToAdd : string) : string = 
        
        let initialString = String.Empty   //initial value of the string
        let listRange = [ 1 .. strSeqNum ] 

        //[<TailCall>] Ok
        let rec loop list acc =
            match list with 
            | []        ->
                         acc
            | _ :: tail -> 
                         let finalString = (+) 
                         loop tail (finalString acc stringToAdd)       //Tail-recursive function calls that have their parameters passed by the pipe operator are not optimized as loops #6984
    
        loop listRange initialString
    
    [<CompiledName "CreateStringCps">]
    let internal createStringCps (strSeqNum : int, stringToAdd : string): string =

        let listRange = [ 1 .. strSeqNum ]
        
        //[<TailCall>] Ok
        let rec loop list cont =
            match list with
            | []        -> cont String.Empty
            | _ :: tail -> loop tail (fun acc -> cont (stringToAdd + acc)) //Continuation-Passing Style (CPS)
    
        loop listRange id
                  
    //List.reduce nelze, tam musi byt stejny typ acc a range      
    [<CompiledName "CreateStringSeqFold">] 
    let internal createStringSeqFold (strSeqNum : int, stringToAdd : string) : string =

        [1 .. strSeqNum]
        |> List.fold (fun acc i -> (+) acc stringToAdd) String.Empty
                  
module CheckNetConnection =  

    open System.Net.NetworkInformation
   
    open MyFsToolkit
      
    let internal checkNetConn (timeout : int) =                 
       
        try
            use myPing = new Ping()      
                
            let host : string = "8.8.4.4" //IP google.com
            let buffer : byte[] = Array.zeroCreate <| 32
            
            let pingOptions: PingOptions = PingOptions ()                
     
            myPing.Send(host, timeout, buffer, pingOptions)
            |> (Option.ofNull >> Option.bind 
                    (fun pingReply 
                        -> 
                         Option.fromBool
                         <| (pingReply |> ignore) 
                         <| (=) pingReply.Status IPStatus.Success                                           
                    )
               ) 
        with
        | ex -> None   

module CopyOrMoveFiles = //output -> Result type 

    open System.IO
    
    open MyFsToolkit
    open MyFsToolkit.Builders
         
    let private processFile source destination action =
                         
        pyramidOfDoom 
            {
                let! sourceFilepath = Path.GetFullPath source |> Option.ofNullEmpty, Error <| sprintf "Chyba při čtení cesty k %s" source
                let! destinFilepath = Path.GetFullPath destination |> Option.ofNullEmpty, Error <| sprintf "Chyba při čtení cesty k %s" destination

                let fInfodat : FileInfo = FileInfo sourceFilepath  
                let! _ = fInfodat.Exists |> Option.ofBool, Error <| sprintf "Zdrojový soubor %s neexistuje" sourceFilepath 

                let dInfodat : DirectoryInfo = DirectoryInfo destinFilepath //Overit vhodnost pred pouzitim
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
    
module CopyOrMoveFilesFM = 

    open System.IO
    
    open MyFsToolkit
    open MyFsToolkit.Builders
           
    type private CommandLineInstruction<'a> =
        | SourceFilepath of (Result<string, string> -> 'a)
        | DestinFilepath of (Result<string, string> -> 'a)
        | CopyOrMove of (Result<string, string> * Result<string, string>)

    type private CommandLineProgram<'a> =
        | Pure of 'a 
        | Free of CommandLineInstruction<CommandLineProgram<'a>>

    let private mapI f = 
        function
        | SourceFilepath next -> SourceFilepath (next >> f)
        | DestinFilepath next -> DestinFilepath (next >> f)
        | CopyOrMove s        -> CopyOrMove s 

    let rec private bind f = 
        function
        | Free x -> x |> mapI (bind f) |> Free
        | Pure x -> f x

    type private CommandLineProgramBuilder = CommandLineProgramBuilder with
        member this.Bind(p, f) = //x |> mapI (bind f) |> Free
            match p with
            | Pure x     -> f x
            | Free instr -> Free (mapI (fun p' -> this.Bind(p', f)) instr)
        member _.Return x = Pure x
        member _.ReturnFrom p = p

    let private cmdBuilder = CommandLineProgramBuilder

    [<Struct>]
    type internal Config =
        {
            source : string
            destination : string
            fileName : string
        }

    [<Struct>]
    type internal IO = 
        | Copy
        | Move     

    let rec private interpret config io clp =
               
        let f (source : Result<string, string>) (destination : Result<string, string>) : Result<unit, string> =
            match source, destination with
            | Ok s, Ok d ->
                          try
                              match io with
                              | Copy -> Ok (File.Copy(s, Path.Combine(d, config.fileName), true))
                              | Move -> Ok (File.Move(s, Path.Combine(d, config.fileName), true))
                          with
                          | ex -> Error ex.Message
            | Error e, _ ->
                          Error e
            | _, Error e ->
                          Error e

        match clp with 
        | Pure x                     ->
                                      x

        | Free (SourceFilepath next) ->
                                      let sourceFilepath source =                                        
                                          pyramidOfDoom
                                              {
                                                  let! value = Path.GetFullPath source |> Option.ofNullEmpty, Error <| sprintf "Chyba při čtení cesty k %s" source   
                                                  let! value = 
                                                      (
                                                          let fInfodat: FileInfo = FileInfo value   
                                                          Option.fromBool value fInfodat.Exists
                                                      ), Error <| sprintf "Zdrojový soubor %s neexistuje" value
                                                  return Ok value
                                              }

                                      interpret config io (next (sourceFilepath config.source))

        | Free (DestinFilepath next) ->
                                      let destinFilepath destination =                                        
                                          pyramidOfDoom
                                              {
                                                  let! value = Path.GetFullPath destination |> Option.ofNullEmpty, Error <| sprintf "Chyba při čtení cesty k %s" destination
                                                  (*
                                                      let! value = 
                                                          (
                                                              let dInfodat: DirectoryInfo = DirectoryInfo value   
                                                              Option.fromBool value dInfodat.Exists
                                                          ), Error <| sprintf "Chyba při čtení cesty k %s" value
                                                  *) 
                                                  return Ok value
                                              }

                                      interpret config io (next (destinFilepath config.destination))

        | Free (CopyOrMove (s, d))  ->
                                     try
                                         f s d 
                                     with
                                     | ex ->
                                           match s, d with
                                           | Ok s, Ok d -> Error <| sprintf "Chyba při kopírování nebo přemísťování souboru %s do %s. %s." s d (string ex.Message)
                                           | Error e, _ -> Error e
                                           | _, Error e -> Error e      

    let internal copyOrMoveFiles config io =  //v pripade pouziti hodit do try-with bloku

        cmdBuilder
            {
                let! sourceFilepath = Free (SourceFilepath Pure)                
                let! destinFilepath = Free (DestinFilepath Pure)

                return! Free (CopyOrMove (sourceFilepath, destinFilepath))
            }

        |> interpret config io  
