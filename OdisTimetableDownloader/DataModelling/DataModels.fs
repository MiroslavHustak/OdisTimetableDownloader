namespace DataModelling

open System

//*************************

open Types

//Type-driven design

module DataModel = 

    type internal DbDataGet = 
        {            
            CompleteLink : CompleteLinkOpt
            FileToBeSaved : FileToBeSavedOpt
        }

    type internal DtDataGet = 
        {           
            NewPrefix : NewPrefix  
            StartDate : StartDateDt
            EndDate : EndDateDt 
            CompleteLink : CompleteLink 
            FileToBeSaved : FileToBeSaved  
            PartialLink : PartialLink
        } 

    type internal DbDataSend = 
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

    type internal DtDataSend = 
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

    type internal RcData = 
        {
            OldPrefixRc : OldPrefix 
            NewPrefixRc : NewPrefix 
            StartDateRc : StartDateRcOpt 
            EndDateRc : EndDateRcOpt 
            TotalDateIntervalRc : TotalDateInterval 
            SuffixRc : Suffix 
            JsGeneratedStringRc : JsGeneratedString 
            CompleteLinkRc : CompleteLink 
            FileToBeSavedRc : FileToBeSaved 
            PartialLinkRc : PartialLink
        }