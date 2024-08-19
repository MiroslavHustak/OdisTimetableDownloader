namespace DataModelling

open System

//*************************

open Types

//Type-driven design

module DataModel = 

    type DbDataGet = 
        {            
            CompleteLink : CompleteLinkOpt
            FileToBeSaved : FileToBeSavedOpt
        }

    type DtDataGet = 
        {           
            NewPrefix : NewPrefix  
            StartDate : StartDateDt
            EndDate : EndDateDt 
            CompleteLink : CompleteLink 
            FileToBeSaved : FileToBeSaved  
            PartialLink : PartialLink
        } 

    type DbDataSend = 
        {
            OldPrefix : OldPrefix 
            NewPrefix : NewPrefix 
            StartDate : StartDate 
            EndDate : EndDate 
            TotalDateInterval : TotalDateInterval 
            Suffix : Suffix 
            JsGeneratedString : JsGeneratedString 
            CompleteLink : CompleteLink 
            FileToBeSaved : FileToBeSaved
            PartialLink : PartialLink
        }

    type DtDataSend = 
        {
            OldPrefix : OldPrefix 
            NewPrefix : NewPrefix 
            StartDate : StartDateDtOpt 
            EndDate : EndDateDtOpt 
            TotalDateInterval : TotalDateInterval 
            Suffix : Suffix 
            JsGeneratedString : JsGeneratedString 
            CompleteLink : CompleteLink 
            FileToBeSaved : FileToBeSaved 
            PartialLink : PartialLink
        }