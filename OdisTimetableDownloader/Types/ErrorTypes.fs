namespace Types

module ErrorTypes =

    open System

    type internal ConnErrorCode = 
        {
            BadRequest : string
            InternalServerError : string
            NotImplemented : string
            ServiceUnavailable : string        
            NotFound : string
            CofeeMakerUnavailable : string
        }

    type internal TryWithErrors = 
        | IOExnErr of string
        | UnauthorizedAccessExnErr of string
        | AllOtherErrors of string