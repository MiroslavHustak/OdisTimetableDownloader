namespace DtDbVariantTest

module Settings = 

    let pathDT = @"c:\Users\User\DataDT\"

    let pathDT_CurrentValidity = @"c:\Users\User\DataDT\JR_ODIS_aktualni_vcetne_vyluk\"
    let pathDT_FutureValidity = @"c:\Users\User\DataDT\JR_ODIS_pouze_budouci_platnost\"
    let pathDT_DPO = @"c:\Users\User\DataDT\JR_ODIS_pouze_linky_dopravce_DPO\"
    let pathDT_MDPO = @"c:\Users\User\DataDT\JR_ODIS_pouze_linky_dopravce_MDPO\"
    let pathDT_WithoutReplacementService = @"c:\Users\User\DataDT\JR_ODIS_teoreticky_dlouhodobe_platne_bez_vyluk\"

    let subPathsDT = 
        [
            pathDT_CurrentValidity
            pathDT_FutureValidity
            pathDT_WithoutReplacementService
            pathDT_DPO
            pathDT_MDPO          
        ]

    let pathDB = @"c:\Users\User\DataDB\"
        
    let pathDB_CurrentValidity = @"c:\Users\User\DataDB\JR_ODIS_aktualni_vcetne_vyluk\"
    let pathDB_FutureValidity = @"c:\Users\User\DataDB\JR_ODIS_pouze_budouci_platnost\"
    let pathDB_DPO = @"c:\Users\User\DataDB\JR_ODIS_pouze_linky_dopravce_DPO\"
    let pathDB_MDPO = @"c:\Users\User\DataDB\JR_ODIS_pouze_linky_dopravce_MDPO\"
    let pathDB_WithoutReplacementService = @"c:\Users\User\DataDB\JR_ODIS_teoreticky_dlouhodobe_platne_bez_vyluk\"  
        
    let subPathsDB = 
        [
            pathDB_CurrentValidity
            pathDB_FutureValidity
            pathDB_WithoutReplacementService
            pathDB_DPO
            pathDB_MDPO
        ] 

module Test = 

    open System
    open System.IO

    open Settings

    let main () = //FileInfo(file) netestovano na pritomnost souboru, nestoji to za tu namahu
        
        try 
            let totalFilesDT = 
                Directory.EnumerateFiles(pathDT, "*", SearchOption.AllDirectories)
                |> Option.ofObj   
                |> function Some value -> value |> Seq.length | None -> -1

            let totalFilesDB = 
                Directory.EnumerateFiles(pathDB, "*", SearchOption.AllDirectories) 
                |> Option.ofObj
                |> function | Some value -> value |> Seq.length | None -> 0

            let resultTotal =              
                match totalFilesDT - totalFilesDB with
                | 0  -> "OK"
                | -1 -> "Error EnumerateFiles" 
                | _  -> "Error"           

            printfn "%s" resultTotal 
            printfn "Total number of all DT files: %d" totalFilesDT
            printfn "Total number of all DB files: %d\n" totalFilesDB  

            //*****************************************************************************
            
            let fileLengthTest listDT listDB = 
                (listDT, listDB) 
                ||> List.iter2
                    (fun subPathDT subPathDB 
                        ->
                         printfn "\n%s" subPathDT
                         printfn "%s\n" subPathDB              

                         let totalLengthDT =
                             Directory.EnumerateFiles(subPathDT, "*", SearchOption.AllDirectories)
                             |> Option.ofObj
                             |> function Some value -> value |> Seq.sumBy (fun file -> FileInfo(file).Length) |> Ok | None -> Error String.Empty

                         let totalLengthDB = 
                             Directory.EnumerateFiles(subPathDB, "*", SearchOption.AllDirectories)
                             |> Option.ofObj
                             |> function Some value -> value |> Seq.sumBy (fun file -> FileInfo(file).Length) |> Ok | None -> Error String.Empty

                         let resultTotal = 
                             match Result.isOk totalLengthDT && Result.isOk totalLengthDB with
                             | true  ->
                                      match (totalLengthDT |> Result.toList |> List.head) - (totalLengthDB |> Result.toList |> List.head) with
                                      | 0L  -> "OK"
                                      | _   -> "Error"  
                             | false -> 
                                     "Error EnumerateFiles"                             
                                              
                         let totalLengthDT_DT = 
                             Directory.EnumerateFiles(subPathDT, "*", SearchOption.AllDirectories)
                             |> Option.ofObj
                             |> function 
                                 | Some value -> 
                                               value
                                               |> Seq.sumBy (fun file -> FileInfo(file).Length)
                                               |> fun totalBytes -> float totalBytes / (1024.0 * 1024.0)
                                               |> Ok
                                 | None       ->
                                               Error String.Empty    

                         let totalLengthDB_MB = 
                             Directory.EnumerateFiles(subPathDB, "*", SearchOption.AllDirectories)
                             |> Option.ofObj
                             |> function 
                                 | Some value -> 
                                               value
                                               |> Seq.sumBy (fun file -> FileInfo(file).Length)
                                               |> fun totalBytes -> float totalBytes / (1024.0 * 1024.0)
                                               |> Ok
                                 | None       ->
                                               Error String.Empty                       

                         let resultTotal_MB = 
                             match Result.isOk totalLengthDT_DT && Result.isOk totalLengthDB_MB with
                             | true  ->
                                      match (totalLengthDT_DT |> Result.toList |> List.head) - (totalLengthDB_MB |> Result.toList |> List.head) with
                                      | 0.0 -> "OK"
                                      | _   -> "Error"  
                             | false -> 
                                     "Error EnumerateFiles"        
                         
                         printfn "%s (%s)" resultTotal resultTotal_MB      
                         printfn "Total length of DT files: %A bytes (%A MB)" totalLengthDT totalLengthDT_DT
                         printfn "Total length of DB files: %A bytes (%A MB)\n" totalLengthDB totalLengthDB_MB
                    )

            printfn "************************************************************************" 
            fileLengthTest [pathDT] [pathDB]  

            printfn "************************************************************************" 
            fileLengthTest subPathsDT subPathsDB            

        with
        | ex -> printfn "%s\n" (string ex.Message)    