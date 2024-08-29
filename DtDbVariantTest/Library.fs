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
    
    //***************************************

    open MyFsToolkit
    open MyFsToolkit.Builders  

    //***************************************
    
    open Settings

    let internal main () = //FileInfo(file) netestovano na pritomnost souboru, nestoji to za tu namahu
        
        try   
            let totalFileNumber path =
                Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                |> Option.ofNull
                |> function Some value -> value |> Seq.length |> Ok | None -> Error String.Empty

            let totalFileNumberDT = totalFileNumber pathDT              
            let totalFileNumberDB = totalFileNumber pathDB

            let resultTotal = 
                pyramidOfHell
                    {
                        let! _ = Result.isOk totalFileNumberDT && Result.isOk totalFileNumberDB, "Error EnumerateFiles"
                        let! _ = (totalFileNumberDT |> Result.toList |> List.head) - (totalFileNumberDB |> Result.toList |> List.head) = 0, "Error"
                        return "OK"
                    }

            printfn "%s" resultTotal 
            printfn "Total number of all DT files: %A" totalFileNumberDT
            printfn "Total number of all DB files: %A\n" totalFileNumberDB  

            //*****************************************************************************
            
            let fileLengthTest listDT listDB = 
                (listDT, listDB) 
                ||> List.iter2
                    (fun subPathDT subPathDB 
                        ->
                         printfn "\n%s" subPathDT
                         printfn "%s\n" subPathDB      
                         
                         let totalLength_Byte path =
                             Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                             |> Option.ofNull
                             |> function Some value -> value |> Seq.sumBy (fun file -> FileInfo(file).Length) |> Ok | None -> Error String.Empty

                         let totalLengthDT_Byte = totalLength_Byte subPathDT
                         let totalLengthDB_Byte = totalLength_Byte subPathDB                            

                         let resultTotal = 
                             pyramidOfHell
                                 {
                                     let! _ = Result.isOk totalLengthDT_Byte && Result.isOk totalLengthDB_Byte, "Error EnumerateFiles"
                                     let! _ = (totalLengthDT_Byte |> Result.toList |> List.head) - (totalLengthDB_Byte |> Result.toList |> List.head) = 0L, "Error"
                                     return "OK"
                                 }
                         
                         let totalLength_MB path = 
                             Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                             |> Option.ofNull
                             |> function 
                                 | Some value -> 
                                               value
                                               |> Seq.sumBy (fun file -> FileInfo(file).Length)
                                               |> fun totalBytes -> float totalBytes / (1024.0 * 1024.0)
                                               |> Ok
                                 | None       ->
                                               Error String.Empty    
                                              
                         let totalLengthDT_MB = totalLength_MB subPathDT                            
                         let totalLengthDB_MB = totalLength_MB subPathDB
                                                                 
                         let resultTotal_MB = 
                             pyramidOfHell
                                 {
                                     let! _ = Result.isOk totalLengthDT_MB && Result.isOk totalLengthDB_MB, "Error EnumerateFiles"
                                     let! _ = (totalLengthDT_MB |> Result.toList |> List.head) - (totalLengthDB_MB |> Result.toList |> List.head) = 0.0, "Error"
                                     return "OK"
                                 } 
                         
                         printfn "%s (%s)" resultTotal resultTotal_MB      
                         printfn "Total length of DT files: %A bytes (%A MB)" totalLengthDT_Byte totalLengthDT_MB
                         printfn "Total length of DB files: %A bytes (%A MB)\n" totalLengthDB_Byte totalLengthDB_MB
                    )

            printfn "************************************************************************" 
            fileLengthTest [pathDT] [pathDB]  

            printfn "************************************************************************" 
            fileLengthTest subPathsDT subPathsDB            

        with
        | ex -> printfn "%s\n" (string ex.Message)    