namespace Helpers

open System
open FsToolkit.ErrorHandling

open Logging.Logging

open Helpers.CloseApp

open Settings.Messages
open Settings.SettingsKODIS

module CollectionSplitting =

    //tady pouze pro educational code 
    let internal splitListByPrefix message (list : string list) : string list list = 
                
        let prefix = fun (item : string) -> item.Substring(0, lineNumberLength)

        try
            (prefix, list)
            ||> List.groupBy //List.groupBy automatically uses the first element of the tuple as the key 
            |> List.map snd
        with
        | ex ->    
              logInfoMsg <| sprintf "Err024C %s" (string ex.Message) 
              closeItBaby msg16 
              [ [] ]   
    
    //tady pouze pro educational code 
    let internal splitListByPrefixExplanation message (list : string list) : string list list = 
                
        let prefix = fun (item: string) -> item.Substring(0, lineNumberLength)
        try
            list 
            |> List.groupBy (fun item -> prefix item)
            |> List.map (fun item -> snd item)        
        with
        | ex ->    
              logInfoMsg <| sprintf "Err025C %s" (string ex.Message) 
              closeItBaby msg16
              [ [] ]   
    
    //tady pouze pro educational code a z toho vyplyvajici testy
    let internal splitListIntoEqualParts (numParts : int) (originalList : 'a list) =   //almost equal parts :-)    
            
        //[<TailCall>] vyzkouseno separatne, bez varovnych hlasek
        let rec splitAccumulator remainingList partsAccumulator acc =
    
            match remainingList with
            | [] -> 
                    partsAccumulator |> List.rev 
            | _  ->                     
                    let currentPartLength =
    
                        let partLength list n = 

                            let totalLength = list |> List.length 
                            let partLength = totalLength / n    
                              
                            totalLength % n > 0
                            |> function
                                | true  -> partLength + 1
                                | false -> partLength 
    
                        match (=) acc numParts with
                        | true  -> partLength originalList numParts    
                        | false -> partLength remainingList acc                                 
        
                    let (part, rest) = remainingList |> List.splitAt currentPartLength 

                    splitAccumulator rest (part :: partsAccumulator) (acc - 1)
                      
        splitAccumulator originalList [] numParts
    
    //tady pouze pro educational code a z toho vyplyvajici testy
    let internal numberOfThreads () l =  
        
        let numberOfThreads = Environment.ProcessorCount //nesu exceptions
        
        match (>) numberOfThreads 0 with //non-nullable
        | true  ->                            
                 match (>=) l numberOfThreads with
                 | true               -> numberOfThreads
                 | false when (>) l 0 -> l
                 | _                  -> 1  
        | false ->
                 logInfoMsg <| sprintf "Err026C %s" "Cannot count the number of processors available to the current process" 
                 1