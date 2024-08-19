namespace DataModelling

open System
open System.Data

module Dto = 

    type DbDtoGet = 
        {
            CompleteLink : string option             
            FileToBeSaved : string option  
        }

    type DtDtoGet = 
        {           
            NewPrefix : string option  
            StartDate : DateTime option 
            EndDate : DateTime option  
            CompleteLink : string option  
            FileToBeSaved : string option  
            PartialLink : string option 
        } 

    type DbDtoSend = 
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

    type DtDtoSend = 
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