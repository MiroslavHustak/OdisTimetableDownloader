namespace TransformationLayers

open System

//******************************************

open MyFsToolkit
open MyFsToolkit.Builders
open MyFsToolkit.TryParserDate

//******************************************

open Types

open DataModelling.Dto
open DataModelling.DataModel

//Type-driven design

module TransformationLayerGet =
        
    let internal dbDataTransformLayerGet (dbDtoGet : DbDtoGet) : DbDataGet =
        {      
            CompleteLink = CompleteLinkOpt dbDtoGet.CompleteLink
            FileToBeSaved = FileToBeSavedOpt dbDtoGet.FileToBeSaved
        }

    let private dtDataTransformLayerGetDefault : DtDataGet = 
        {      
            NewPrefix = NewPrefix String.Empty
            StartDate = StartDateDt DateTime.MinValue
            EndDate = EndDateDt DateTime.MinValue
            CompleteLink = CompleteLink String.Empty
            FileToBeSaved = FileToBeSaved String.Empty
            PartialLink = PartialLink String.Empty
        } 

    let internal dtDataTransformLayerGet (dtDtoGet : DtDtoGet) : DtDataGet =  
        
        pyramidOfDoom
           {
               let! newPrefix = dtDtoGet.NewPrefix, dtDataTransformLayerGetDefault //pri nesouladu se vraci vse jako default bez ohledu na ostatni vysledky
               let! startDate = dtDtoGet.StartDate, dtDataTransformLayerGetDefault 
               let! endDate = dtDtoGet.EndDate, dtDataTransformLayerGetDefault 
               let! completeLink = dtDtoGet.CompleteLink, dtDataTransformLayerGetDefault 
               let! fileToBeSaved = dtDtoGet.FileToBeSaved, dtDataTransformLayerGetDefault 
               let! partialLink = dtDtoGet.PartialLink, dtDataTransformLayerGetDefault

               return //vraci pouze pokud je vse spravne
                   {      
                       NewPrefix = NewPrefix newPrefix
                       StartDate = StartDateDt startDate
                       EndDate = EndDateDt endDate
                       CompleteLink = CompleteLink completeLink
                       FileToBeSaved = FileToBeSaved fileToBeSaved
                       PartialLink = PartialLink partialLink
                   } 
           }

module TransformationLayerSend =
        
    let internal dbDataTransformLayerSend (dbDataSend : DbDataSend) : DbDtoSend =
        {
            OldPrefix = dbDataSend.OldPrefix |> function OldPrefix value -> value
            NewPrefix = dbDataSend.NewPrefix |> function NewPrefix value -> value
            StartDate =
                dbDataSend.StartDate
                |> function StartDate value -> value
                |> parseDate () 
                |> function Some value -> value | None -> DateTime.MinValue
            EndDate = 
                dbDataSend.EndDate
                |> function EndDate value -> value
                |> parseDate ()  
                |> function Some value -> value | None -> DateTime.MinValue               
            TotalDateInterval = dbDataSend.TotalDateInterval |> function TotalDateInterval value -> value
            Suffix = dbDataSend.Suffix |> function Suffix value -> value
            JsGeneratedString = dbDataSend.JsGeneratedString |> function JsGeneratedString value -> value
            CompleteLink = dbDataSend.CompleteLink |> function CompleteLink value -> value
            FileToBeSaved = dbDataSend.FileToBeSaved |> function FileToBeSaved value -> value
            PartialLink = dbDataSend.PartialLink |> function PartialLink value -> value
        } 

    let internal dtDataTransformLayerSend (dtDataSend : DtDataSend) : DtDtoSend =
        {
            OldPrefix = dtDataSend.OldPrefix |> function OldPrefix value -> value
            NewPrefix = dtDataSend.NewPrefix |> function NewPrefix value -> value
            StartDate =
                dtDataSend.StartDate
                |> function StartDateDtOpt value -> value
                |> function Some value -> value | None -> DateTime.MinValue
            EndDate = 
                dtDataSend.EndDate
                |> function EndDateDtOpt value -> value
                |> function Some value -> value | None -> DateTime.MinValue
            TotalDateInterval = dtDataSend.TotalDateInterval |> function TotalDateInterval value -> value
            Suffix = dtDataSend.Suffix |> function Suffix value -> value
            JsGeneratedString = dtDataSend.JsGeneratedString |> function JsGeneratedString value -> value
            CompleteLink = dtDataSend.CompleteLink |> function CompleteLink value -> value
            FileToBeSaved = dtDataSend.FileToBeSaved |> function FileToBeSaved value -> value
            PartialLink = dtDataSend.PartialLink |> function PartialLink value -> value
        }