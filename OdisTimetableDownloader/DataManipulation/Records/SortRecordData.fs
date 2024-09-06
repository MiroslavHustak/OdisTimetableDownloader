﻿namespace Records

open System
open System.Data

//******************************

open MyFsToolkit

//******************************

open Types

open Logging.Logging
open Helpers.CloseApp
open Settings.SettingsKODIS

open DataModelling.DataModel


//chyby vezme tryWith Err18
module SortRecordData =  
       
    let internal sortLinksOut (dataToBeInserted : RcData list) validity = 
               
        try            
            try                

                let condition dateValidityStart dateValidityEnd currentTime (fileToBeSaved : string) = 

                    match validity with 
                    | CurrentValidity           -> 
                                                 ((dateValidityStart <= currentTime
                                                 && 
                                                 dateValidityEnd >= currentTime)
                                                 ||
                                                 (dateValidityStart = currentTime 
                                                 && 
                                                 dateValidityEnd = currentTime))
                                                 &&
                                                 (
                                                    not <| fileToBeSaved.Contains("046_2024_01_02_2024_12_14")
                                                 )
                                                 &&
                                                 (
                                                    not <| fileToBeSaved.Contains("020_2024_01_02_2024_12_14")
                                                 )

                    | FutureValidity            ->
                                                 dateValidityStart > currentTime
                  
                    | WithoutReplacementService ->                                         
                                                 ((dateValidityStart <= currentTime 
                                                 && 
                                                 dateValidityEnd >= currentTime)
                                                 ||
                                                 (dateValidityStart = currentTime 
                                                 && 
                                                 dateValidityEnd = currentTime))
                                                 &&
                                                 (not <| fileToBeSaved.Contains("_v") 
                                                 && not <| fileToBeSaved.Contains("X")
                                                 && not <| fileToBeSaved.Contains("NAD")) 
                                                 &&
                                                 (dateValidityEnd <> summerHolidayEnd1
                                                 && 
                                                 dateValidityEnd <> summerHolidayEnd2)
                                                 &&
                                                 (
                                                    not <| fileToBeSaved.Contains("020_2024_01_02_2024_12_14")
                                                 )
                                        
                let currentTime = DateTime.Now.Date
                let dataToBeInserted = dataToBeInserted |> List.toSeq |> Seq.distinct   
                
                validity 
                |> function
                    | FutureValidity ->  
                                      dataToBeInserted                                                                           
                                      |> Seq.groupBy (fun row -> row.PartialLinkRc)
                                      |> Seq.map (fun (partialLink, group) -> group |> Seq.head)
                                      |> Seq.filter
                                          (fun row ->
                                                    let startDate = 
                                                        row.StartDateRc
                                                        |> function StartDateRcOpt value -> value
                                                        |> function Some value -> value | None -> DateTime.MinValue
                                                    let endDate = 
                                                        row.EndDateRc                                                         
                                                        |> function EndDateRcOpt value -> value
                                                        |> function Some value -> value | None -> DateTime.MinValue
                                                    let fileToBeSaved = row.FileToBeSavedRc |> function FileToBeSaved value -> value                         
                                        
                                                    condition startDate endDate currentTime fileToBeSaved
                                          )     
                                      |> Seq.map
                                          (fun row ->
                                                    row.CompleteLinkRc,
                                                    row.FileToBeSavedRc
                                          )
                                      |> Seq.distinct //na rozdil od ITVF v SQL se musi pouzit distinct                                     
                                      |> List.ofSeq
                                      |> Ok

                    | _              -> 
                                      dataToBeInserted  
                                      |> Seq.groupBy (fun row -> row.PartialLinkRc)
                                      |> Seq.map (fun (partialLink, group) -> group |> Seq.head)
                                      |> Seq.filter
                                          (fun row ->
                                                    let startDate = 
                                                        row.StartDateRc
                                                        |> function StartDateRcOpt value -> value
                                                        |> function Some value -> value | None -> DateTime.MinValue
                                                    let endDate = 
                                                        row.EndDateRc                                                         
                                                        |> function EndDateRcOpt value -> value
                                                        |> function Some value -> value | None -> DateTime.MinValue
                                                    let fileToBeSaved = row.FileToBeSavedRc  |> function FileToBeSaved value -> value                      
                                        
                                                    condition startDate endDate currentTime fileToBeSaved
                                          )           
                                      |> Seq.sortByDescending (fun row -> row.StartDateRc)
                                      |> Seq.groupBy (fun row -> row.NewPrefixRc)
                                      |> Seq.map
                                          (fun (newPrefix, group)
                                              ->
                                               newPrefix, 
                                               group |> Seq.head
                                          )
                                      |> Seq.map
                                          (fun (newPrefix, row) 
                                              ->
                                               row.CompleteLinkRc,
                                               row.FileToBeSavedRc
                                          )
                                      |> Seq.distinct 
                                      |> List.ofSeq
                                      |> Ok
            finally
               ()               
    
        with ex -> Error <| string ex.Message
        
        |> function
            | Ok value  -> 
                         value  
            | Error err ->
                         logInfoMsg <| sprintf "Err901B %s" err 
                         closeItBaby err
                         []