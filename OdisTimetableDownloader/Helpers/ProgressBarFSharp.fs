namespace Helpers

open System

//*********************

open Helpers.CloseApp
open Settings.Messages

module ProgressBarFSharp =

    // ************************************************************************************************************
    // Adapted from C# code under MIT License, Copyright (c) 2017 Daniel Wolf, https://gist.github.com/DanielSWolf
    // ************************************************************************************************************
    
    let inline private updateProgressBar (currentProgress : int) (totalProgress : int) : unit =

        try
            //437 je tzv. Extended ASCII   
            let bytes = System.Text.Encoding.GetEncoding(437).GetBytes("█")    
                   
            let output = System.Text.Encoding.GetEncoding(852).GetChars(bytes)
                      
            let progressBar = 

                let barWidth = 50 //nastavit delku dle potreby            
                let percentComplete = (currentProgress * 101) / (totalProgress + 1) //101 proto, ze pri deleni 100 to po zaokrouhleni dalo jen 99%                    
                let barFill = (currentProgress * barWidth) / totalProgress 
               
                let characterToFill = string (Array.item 0 output) //moze byt baj aji "#" //Option.ofNullEmpty tady nestoji za tu namahu
            
                let bar = string <| String.replicate barFill characterToFill //Option.ofNullEmpty tady nestoji za tu namahu
                                                 
                let remaining = string <| String.replicate (barWidth - (barFill + 1)) "*" //Option.ofNullEmpty tady nestoji za tu namahu
                                  
                sprintf "<%s%s> %d%%" bar remaining percentComplete 

            match (=) currentProgress totalProgress with
            | true  -> Ok <| msgParam8 progressBar
            | false -> Ok <| msgParam9 progressBar

        with ex -> Error <| string ex.Message
                               
        |> function
            | Ok value -> value  
            | Error _  -> () //nestoji to za to neco s tim delat   
                                 
    let internal progressBarContinuous (currentProgress : int) (totalProgress : int) : unit =

        match currentProgress < (-) totalProgress 1 with
        | true  -> 
                 updateProgressBar currentProgress totalProgress
        | false ->              
                 Console.Write("\r" + new string(' ', (-) Console.WindowWidth 1) + "\r")
                 Console.CursorLeft <- 0 