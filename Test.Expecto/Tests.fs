namespace Test.Expecto

open System
open System.IO

open Expecto
open FsToolkit.ErrorHandling

open Helpers.CollectionSplitting

module TestInputs = //input data mocking 
    
    let internal testListWithLinks param = 
        List.init param (fun i -> sprintf "random string simulating a link No. %i" i)       

//vyukovy kod, testy tady nejsou zrovna dvakrat treba, snad s vyjimkou 04P ...
module ExpectoTests = 

    open TestInputs
    
    //The default is to run all of your tests in parallel 

    //[<Tests>]
    let private expectoPassingTests =

        testList "PassingTests"
            [      
                testCase "03P List Length Difference" 
                <|
                fun _ ->
                    try
                        let l = testListWithLinks 237 |> List.length
                        
                        let splittedList = splitListIntoEqualParts (numberOfThreads () l) (testListWithLinks 237)

                        let diff = 
                            abs ((-) (splittedList |> List.item 0 |> List.length) (splittedList |> List.item (Environment.ProcessorCount - 1) |> List.length)) <= 1
                            &&
                            abs ((-) (splittedList |> List.item 0 |> List.length) (splittedList |> List.item (Environment.ProcessorCount - 2) |> List.length)) <= 1                    
                        
                        Expect.isTrue diff "03P"
                    with
                    | ex -> failwith (sprintf "ExpectoTestError 03P: %s" ex.Message) 

                testCase "04P List Length Difference" 
                <|
                fun _ ->                                
                    try  
                        let numberOfThreads1 l =  
                                                                
                            let numberOfThreads = System.Random().Next(1, 101)
        
                            match numberOfThreads > 0 with //non-nullable
                            | true  ->                            
                                     match l >= numberOfThreads with
                                     | true             -> numberOfThreads
                                     | false when l > 0 -> l
                                     | _                -> 0  
                            | false ->
                                     failwith "Number of Threads Error"  
                                     -1

                        let result = 
                            [1..1000] 
                            |> List.map 
                                (fun item ->  
                                           let n = numberOfThreads1 item   
                                           let splittedList = splitListIntoEqualParts n (testListWithLinks item)

                                           let diff = 
                                               abs ((-) (splittedList |> List.item 0 |> List.length) (splittedList |> List.item (n - 1) |> List.length)) <= 1
                                               &&
                                               match n > 1 with
                                               | true  -> abs ((-) (splittedList |> List.item 0 |> List.length) (splittedList |> List.item (n - 2) |> List.length)) <= 1 
                                               | false -> true 
                                           diff
                                )    
                            |> List.forall id

                        Expect.isTrue result "04P"    
                    with
                    | ex -> failwith (sprintf "ExpectoTestError 04P: %s" ex.Message)       
            ]

    //[<Tests>]    
    let private expectoFailingTests =
            
        testList "FailingTests"
            [                                                  
                testCase "101F ExpectoTest" 
                <| 
                fun _ ->                                 
                       try
                           Expect.isTrue true String.Empty  //TODO podumat nad failing tests
                       with
                       | ex -> failwith (sprintf "ExpectoTestError 101F: %s" ex.Message)                  
            ]
   
    [<Tests>] 
    let internal all =

        testList "ExpectoTests"
            [
                expectoPassingTests
                expectoFailingTests
            ]

    
        

