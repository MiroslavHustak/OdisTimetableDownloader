namespace MyCanopy

open System
open System.Threading

open Serialization.Serialisation

module MyCanopy =

    //testing
    
    let internal canopyResult () = 
    
        try
            canopy.configuration.edgeDir <- @"c:/temp/driver" 
            canopy.classic.start canopy.classic.edgeBETA
    
            canopy.configuration.compareTimeout <- 100.0 
    
            //In CSS selectors, the . before a word or identifier specifies a class.    
            //<li class="Card_wrapper__ZQ5Fp">
            let linksShown () = (canopy.classic.elements ".Card_wrapper__ZQ5Fp").Length >= 1 // //".Card_wrapper__ZQ5Fp" //.Card_actions__HhB_f
            
            let urls = 
                [
                    "https://www.kodis.cz/lines/city?tab=MHD+Ostrava"
                    "https://www.kodis.cz/lines/region?tab=75" 
                    "https://www.kodis.cz/lines/city?tab=MHD+Opava"
                    "https://www.kodis.cz/lines/region?tab=232-293"
                    "https://www.kodis.cz/lines/city?tab=MHD+Frýdek-Místek"
                    "https://www.kodis.cz/lines/region?tab=331-392"
                    "https://www.kodis.cz/lines/city?tab=MHD+Havířov"
                    "https://www.kodis.cz/lines/region?tab=440-465"
                    "https://www.kodis.cz/lines/city?tab=MHD+Karviná"
                    "https://www.kodis.cz/lines/city?tab=MHD+Orlová"
                    "https://www.kodis.cz/lines/region?tab=531-583"
                    "https://www.kodis.cz/lines/city?tab=MHD+Nový+Jičín"
                    "https://www.kodis.cz/lines/city?tab=MHD+Studénka"
                    "https://www.kodis.cz/lines/region?tab=613-699"
                    "https://www.kodis.cz/lines/city?tab=MHD+Třinec"
                    "https://www.kodis.cz/lines/city?tab=MHD+Český+Těšín"
                    "https://www.kodis.cz/lines/region?tab=731-788"
                    "https://www.kodis.cz/lines/city?tab=MHD+Krnov"
                    "https://www.kodis.cz/lines/city?tab=MHD+Bruntál"
                    "https://www.kodis.cz/lines/region?tab=811-885"
                    "https://www.kodis.cz/lines/region?tab=901-990"
                    "https://www.kodis.cz/lines/train?tab=S1-S34"
                    "https://www.kodis.cz/lines/train?tab=R8-R62"
                    "https://www.kodis.cz/lines/city?tab=NAD+MHD"
                    "https://www.kodis.cz/lines/region?tab=NAD"   
                ]

            let scrapeUrl (url: string) =

                canopy.classic.url url

                let pdfLinkSeq () =

                    Thread.Sleep 10000                     
                    canopy.classic.waitFor linksShown 
                    
                    canopy.classic.elements "a[title='Aktuální jízdní řád']" 
                    |> Seq.map 
                        (fun item -> 
                                   let href = string <| item.GetAttribute("href")
                                   match href.EndsWith("pdf") with
                                   | true  -> 
                                            printfn "%s" href
                                            Some href
                                   | false -> 
                                            None
                        )
                    |> Seq.distinct

                let clickCondition () =
                    try
                        let nextButton = canopy.classic.elementWithText "a" "Další"
                        nextButton.Displayed && nextButton.Enabled
                    with
                    | _ -> false
    
                let pdfLinkList1 = pdfLinkSeq () |> Seq.distinct |> List.ofSeq

                let pdfLinkList2 = 
                    Seq.initInfinite (fun _ -> clickCondition())
                    |> Seq.takeWhile ((=) true) 
                    |> Seq.collect
                        (fun _ -> 
                                canopy.classic.click (canopy.classic.elementWithText "a" "Další")
                                pdfLinkSeq ()
                        )
                    |> Seq.distinct
                    |> Seq.toList                  

                (pdfLinkList1 @ pdfLinkList2) |> List.choose id
            
            let list = 
                urls 
                |> List.collect scrapeUrl
                |> List.filter (fun item -> not <| item.Contains "2022")
           
            serializeToJsonThoth2 list "CanopyResults/canopy_results.json" 

        with
        | ex -> Error (string ex.Message)
