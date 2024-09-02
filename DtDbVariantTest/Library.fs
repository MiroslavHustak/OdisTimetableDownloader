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

    //SRTPs
    let inline private result (totalDT: Result< ^a, string>) (totalDB: Result< ^a, string>) (zero: ^a) =  //viz learning material ohledne generics a SRTPs

        pyramidOfHell
            {
                let! _ = Result.isOk totalDT && Result.isOk totalDB, "Error EnumerateFiles"
                let! _ = (totalDT |> Result.toList |> List.head) - (totalDB |> Result.toList |> List.head) = zero, "Error"
                return "OK"
            }   

    //Generics 
    let inline private resultGenericsTest (totalDT: Result< 'a, string>) (totalDB: Result< 'a, string>) (zero: 'a) =  

        pyramidOfHell
            {
                let! _ = Result.isOk totalDT && Result.isOk totalDB, "Error EnumerateFiles" //neni operace na generics -> funguje 
                return "OK"
            }   
            
    let private totalFileNumber path : Result<int, string> =

        Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
        |> Option.ofNull
        |> function Some value -> value |> Seq.length |> Ok | None -> Error String.Empty

    let private totalLength_Byte path : Result<int64, string> =

        Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
        |> Option.ofNull
        |> function Some value -> value |> Seq.sumBy (fun file -> FileInfo(file).Length) |> Ok | None -> Error String.Empty

    let private totalLength_MB path : Result<float, string> = 

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

    let private fileLengthTest listDT listDB = 

        let numberOfDTFolders = listDT |> List.length
        let numberOfDBFolders = listDB |> List.length
               
        match numberOfDTFolders = numberOfDBFolders with
        | true -> 
                (listDT, listDB) 
                ||> List.iter2
                    (fun subPathDT subPathDB 
                        ->
                         printfn "\n%s" subPathDT
                         printfn "%s\n" subPathDB   

                         let totalLengthDT_Byte = totalLength_Byte subPathDT
                         let totalLengthDB_Byte = totalLength_Byte subPathDB                            

                         let resultTotal_Byte = result totalLengthDT_Byte totalLengthDB_Byte 0L                     
                                              
                         let totalLengthDT_MB = totalLength_MB subPathDT                            
                         let totalLengthDB_MB = totalLength_MB subPathDB
                                                                 
                         let resultTotal_MB = result totalLengthDT_MB totalLengthDB_MB 0.0
                                                    
                         printfn "%s (%s)" resultTotal_Byte resultTotal_MB      
                         printfn "Total length of DT files: %A bytes (%A MB)" totalLengthDT_Byte totalLengthDT_MB
                         printfn "Total length of DB files: %A bytes (%A MB)\n" totalLengthDB_Byte totalLengthDB_MB
                         
                         printfn "Test chování generics 1: %s" <| resultGenericsTest totalLengthDT_Byte totalLengthDB_Byte 0L
                         printfn "Test chování generics 2: %s" <| resultGenericsTest totalLengthDT_MB totalLengthDB_MB 0.0
                    )   
        | false -> 
                 printfn "\nUnbelievable Error"
                 printfn "Something is wrong with the list \"subPathsDT\" or the list \"subPathsDB\""

    let internal main () = //FileInfo(file) netestovano na pritomnost souboru, nestoji to za tu namahu        
        
        try               

            let totalFileNumberDT = totalFileNumber pathDT              
            let totalFileNumberDB = totalFileNumber pathDB

            let resultTotal = result totalFileNumberDT totalFileNumberDB 0
           
            printfn "%s" resultTotal 
            printfn "Total number of all DT files: %A" totalFileNumberDT
            printfn "Total number of all DB files: %A\n" totalFileNumberDB  

            printfn "Test chování generics 3: %s" <| resultGenericsTest totalFileNumberDT totalFileNumberDB 0

            //*****************************************************************************            

            printfn "%s" <| String.replicate 70 "*"
            fileLengthTest [pathDT] [pathDB]  

            printfn "%s" <| String.replicate 70 "*"
            fileLengthTest subPathsDT subPathsDB            

        with
        | ex -> printfn "%s\n" (string ex.Message)    

(*
// F#

let inline private result (totalDT: Result< ^a, string>) (totalDB: Result< ^a, string>) (zero: ^a) =  

    pyramidOfHell
        {
            let! _ = Result.isOk totalDT && Result.isOk totalDB, "Error EnumerateFiles"
            let! _ = (totalDT |> Result.toList |> List.head) - (totalDB |> Result.toList |> List.head) = zero, "Error"
            return "OK"
        }  

--Haskell

import Data.Either (isRight)

result :: (Eq a, Num a) => Either String a -> Either String a -> a -> String
result totalDT totalDB zero
    | isRight totalDT && isRight totalDB = 
        let dt = either (const zero) id totalDT
            db = either (const zero) id totalDB
        in if (dt - db) == zero 
           then "Error"
           else "OK"
    | otherwise = "Error EnumerateFiles"


-- Haskell has predefined instances:
-- Eq instances exist for types like Int, Char, Bool, String, and more.
-- Num instances exist for numeric types like Int, Integer, Float, Double, etc.

*)