namespace DtDbVariantTest

module Settings = 

    let internal pathDT = @"c:\Users\User\DataDT\"

    let private pathDT_CurrentValidity = @"c:\Users\User\DataDT\JR_ODIS_aktualni_vcetne_vyluk\"
    let private pathDT_FutureValidity = @"c:\Users\User\DataDT\JR_ODIS_pouze_budouci_platnost\"
    let private pathDT_DPO = @"c:\Users\User\DataDT\JR_ODIS_pouze_linky_dopravce_DPO\"
    let private pathDT_MDPO = @"c:\Users\User\DataDT\JR_ODIS_pouze_linky_dopravce_MDPO\"
    let private pathDT_WithoutReplacementService = @"c:\Users\User\DataDT\JR_ODIS_teoreticky_dlouhodobe_platne_bez_vyluk\"

    let internal subPathsDT = 
        [
            pathDT_CurrentValidity
            pathDT_FutureValidity
            pathDT_WithoutReplacementService
            pathDT_DPO
            pathDT_MDPO          
        ]

    let internal pathDB = @"c:\Users\User\DataDB\"
        
    let private pathDB_CurrentValidity = @"c:\Users\User\DataDB\JR_ODIS_aktualni_vcetne_vyluk\"
    let private pathDB_FutureValidity = @"c:\Users\User\DataDB\JR_ODIS_pouze_budouci_platnost\"
    let private pathDB_DPO = @"c:\Users\User\DataDB\JR_ODIS_pouze_linky_dopravce_DPO\"
    let private pathDB_MDPO = @"c:\Users\User\DataDB\JR_ODIS_pouze_linky_dopravce_MDPO\"
    let private pathDB_WithoutReplacementService = @"c:\Users\User\DataDB\JR_ODIS_teoreticky_dlouhodobe_platne_bez_vyluk\"  
        
    let internal subPathsDB = 
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

    let internal main () = //FileInfo(file) netestovano na pritomnost souboru, nestoji to za tu namahu
        
        try   
            let totalFilesDT =
                Directory.EnumerateFiles(pathDT, "*", SearchOption.AllDirectories)
                |> Option.ofObj
                |> function Some value -> value |> Seq.length |> Ok | None -> Error String.Empty

            let totalFilesDB = 
                Directory.EnumerateFiles(pathDB, "*", SearchOption.AllDirectories)
                |> Option.ofObj
                |> function Some value -> value |> Seq.length |> Ok | None -> Error String.Empty

            let resultTotal = 
                match Result.isOk totalFilesDT && Result.isOk totalFilesDB with
                | true  ->
                         match (totalFilesDT |> Result.toList |> List.head) - (totalFilesDB |> Result.toList |> List.head) with
                         | 0  -> "OK"
                         | _  -> "Error"  
                | false -> 
                         "Error EnumerateFiles"     

            printfn "%s" resultTotal 
            printfn "Total number of all DT files: %A" totalFilesDT
            printfn "Total number of all DB files: %A\n" totalFilesDB  

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