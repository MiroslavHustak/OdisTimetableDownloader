namespace Test.xUnit

open System

open Xunit

open Helpers.CollectionSplitting

module TestInputs = //input data mocking  
    
    let internal testListWithLinks param = 
        List.init param (fun i -> sprintf "random string simulating a link No. %i" i)       

module PassingTests = 

    open TestInputs

    //Passing test 1 ********************************************************************************************************

    [<Fact>] 
    let private ``01P Splitting Lists`` () =   

        try
            let l = testListWithLinks 237 |> List.length
                        
            let splittedList = splitListIntoEqualParts (numberOfThreads () l) (testListWithLinks 237)

            let equalLengths = splittedList |> List.length = Environment.ProcessorCount
                        
            Assert.True(equalLengths)
        with
        | ex -> failwith (sprintf "xUnitTestError 01P: %s" ex.Message)       

    
    //Passing test 2 ********************************************************************************************************

    [<Fact>] 
    let private ``02P Last List Length Not Zero`` () =
       try
            let l = testListWithLinks 237 |> List.length
                              
            let splittedList = splitListIntoEqualParts (numberOfThreads () l) (testListWithLinks 237)

            let lastListLengthNotZero = (<>) (splittedList |> List.item (Environment.ProcessorCount - 1) |> List.length) 0
                              
            Assert.True(lastListLengthNotZero)
        with
        | ex -> failwith (sprintf "xUnitTestError 02P: %s" ex.Message)

    
   