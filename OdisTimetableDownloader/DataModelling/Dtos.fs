namespace DataModelling

open System
open System.Data

module Dto = 

    type internal DbDtoGet = 
        {
            CompleteLink : string option             
            FileToBeSaved : string option  
        }

    type internal DtDtoGet = 
        {           
            NewPrefix : string option  
            StartDate : DateTime option 
            EndDate : DateTime option  
            CompleteLink : string option  
            FileToBeSaved : string option  
            PartialLink : string option 
        } 

    type internal DbDtoSend = 
        {
            OldPrefix : string 
            NewPrefix : string 
            StartDate : DateTime  
            EndDate : DateTime   
            TotalDateInterval : string 
            Suffix : string 
            JsGeneratedString : string 
            CompleteLink : string 
            FileToBeSaved : string 
            PartialLink : string
        }

    type internal DtDtoSend = 
        {
            OldPrefix : string 
            NewPrefix : string 
            StartDate : DateTime  
            EndDate : DateTime   
            TotalDateInterval : string 
            Suffix : string 
            JsGeneratedString : string 
            CompleteLink : string 
            FileToBeSaved : string  
            PartialLink : string  
        }