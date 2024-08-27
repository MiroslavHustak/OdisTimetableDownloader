namespace Puppeteer

open System
open System.Threading.Tasks
open System.Collections.Generic

open PuppeteerSharp

//Toto je pouze kod pro overeni odkazu na JSON, pri normalnim behu aplikace se nepouziva
module Links = 

    let private scrapeLinks executablePath =
   
       async
           {
                try
                    // Define the URL and the keyword to filter the network requests
                    let urlList = 
                        [
                            "https://www.kodis.cz/lines/train"
                            "https://www.kodis.cz/lines/region"
                            "https://www.kodis.cz/lines/city"
                        ]

                    let keyword = "tab"

                    let launchOptions =                       
                        LaunchOptions
                            (
                                Headless = true,
                                ExecutablePath = executablePath
                            )   
                        |> Option.ofObj
                        |> function
                            | Some value ->
                                          value
                            | None       -> 
                                          printfn "Error in scrapeLinks: %s" "LaunchOptions -> null"
                                          printfn "Press any key to close this app"
                                          Console.ReadKey() |> ignore 
                                          System.Environment.Exit(1)  
                                          LaunchOptions
                                                (
                                                    Headless = true,
                                                    ExecutablePath = executablePath
                                                )                       
                   
                    use! browser =
                        Puppeteer.LaunchAsync(launchOptions)       
                        |> Option.ofObj
                        |> function
                            | Some value ->
                                          value
                            | None       -> 
                                          printfn "Error in scrapeLinks: %s" "browser -> null"
                                          printfn "Press any key to close this app"
                                          Console.ReadKey() |> ignore 
                                          System.Environment.Exit(1)  
                                          Puppeteer.LaunchAsync(launchOptions)   
                        |> Async.AwaitTask   
                   
                    use! page =
                        browser.NewPageAsync()       
                        |> Option.ofObj
                        |> function
                            | Some value ->
                                          value
                            | None       -> 
                                          printfn "Error in scrapeLinks: %s" "page -> null"
                                          printfn "Press any key to close this app"
                                          Console.ReadKey() |> ignore 
                                          System.Environment.Exit(1)  
                                          browser.NewPageAsync()   
                        |> Async.AwaitTask                      

                    let allFilteredLinks = 
                        urlList 
                        |> List.map 
                            (fun url -> 
                                      async 
                                          {
                                              try
                                                  // Navigate to the target URL                                                 
                                                  do! page.GoToAsync(url)      
                                                      |> Option.ofObj
                                                      |> function
                                                          | Some value ->
                                                                        value
                                                          | None       -> 
                                                                        printfn "Error in scrapeLinks: %s" "response -> null"
                                                                        printfn "Press any key to close this app"
                                                                        Console.ReadKey() |> ignore 
                                                                        System.Environment.Exit(1)  
                                                                        page.GoToAsync(url)
                                                      |> Async.AwaitTask      
                                                      |> Async.Ignore
                                                  
                                                  // Evaluate the page's DOM to extract all anchor tags
                                                  let! links =                                                                                                          
                                                      page.EvaluateExpressionAsync<string list>("Array.from(document.querySelectorAll('a')).map(a => a.href)")      
                                                      |> Option.ofObj
                                                      |> function
                                                          | Some value ->
                                                                        value
                                                          | None       -> 
                                                                        printfn "Error in scrapeLinks: %s" "task -> null"
                                                                        printfn "Press any key to close this app"
                                                                        Console.ReadKey() |> ignore 
                                                                        System.Environment.Exit(1)  
                                                                        page.EvaluateExpressionAsync<string list>("Array.from(document.querySelectorAll('a')).map(a => a.href)")
                                                      |> Async.AwaitTask   

                                                  let filteredLinks = links |> List.filter (fun link -> link.Contains(keyword))

                                                  return filteredLinks
                                              with
                                              | ex -> 
                                                    printfn "Error scraping %s: %s" url ex.Message
                                                    return [] 
                                          }
                            )
                        |> Async.Sequential 
                        |> Async.RunSynchronously

                    // Combine all filtered links from all pages
                    let combinedLinks = List.concat allFilteredLinks

                    do! browser.CloseAsync() |> Async.AwaitTask //use! only disposes, browser se musi separatne zavrit

                    return combinedLinks
                with
                | ex ->
                      printfn "Error in scrapeLinks: %s" (string ex.Message)
                      printfn "Press any key to close this app"
                      Console.ReadKey() |> ignore 
                      System.Environment.Exit(1) 
                      
                      return [] 
            }
    
    let private captureNetworkRequest urlList executablePath =

        async 
            {   
                try              
                    let keyword = "linky-search"

                    let launchOptions =                       
                        LaunchOptions
                            (
                                Headless = true,
                                ExecutablePath = executablePath 
                            )   
                        |> Option.ofObj
                        |> function
                            | Some value ->
                                          value
                            | None       -> 
                                          printfn "Error in scrapeLinks: %s" "LaunchOptions -> null"
                                          printfn "Press any key to close this app"
                                          Console.ReadKey() |> ignore 
                                          System.Environment.Exit(1)  
                                          LaunchOptions
                                                (
                                                    Headless = true,
                                                    ExecutablePath = executablePath
                                                )  
                   
                    use! browser =
                        Puppeteer.LaunchAsync(launchOptions)       
                        |> Option.ofObj
                        |> function
                            | Some value ->
                                          value
                            | None       -> 
                                          printfn "Error in scrapeLinks: %s" "browser -> null"
                                          printfn "Press any key to close this app"
                                          Console.ReadKey() |> ignore 
                                          System.Environment.Exit(1)  
                                          Puppeteer.LaunchAsync(launchOptions)   
                        |> Async.AwaitTask   
                   
                    use! page =
                        browser.NewPageAsync()       
                        |> Option.ofObj
                        |> function
                            | Some value ->
                                          value
                            | None       -> 
                                          printfn "Error in scrapeLinks: %s" "page -> null"
                                          printfn "Press any key to close this app"
                                          Console.ReadKey() |> ignore 
                                          System.Environment.Exit(1)  
                                          browser.NewPageAsync()   
                        |> Async.AwaitTask 

                    // Set up request interception to monitor all network requests                    
                    do! page.SetRequestInterceptionAsync(true)                                                    
                        |> Option.ofObj
                        |> function
                            | Some value ->
                                          value
                            | None       -> 
                                          printfn "Error in scrapeLinks: %s" "task -> null"
                                          printfn "Press any key to close this app"
                                          Console.ReadKey() |> ignore 
                                          System.Environment.Exit(1)  
                                          page.SetRequestInterceptionAsync(true)  
                        |> Async.AwaitTask
                                            
                    let capturedUrls = 
                        System.Collections.Concurrent.ConcurrentBag<string>()      
                        |> Option.ofObj
                        |> function
                            | Some value ->
                                          value
                            | None       -> 
                                          printfn "Error in scrapeLinks: %s" "capturedUrls -> null"
                                          printfn "Press any key to close this app"
                                          Console.ReadKey() |> ignore 
                                          System.Environment.Exit(1)  
                                          System.Collections.Concurrent.ConcurrentBag<string>()  

                    // Event handler to intercept and analyze requests
                    page.Request.Add
                        (fun req ->                    
                                  async 
                                      {
                                          match req.Request.Url.Contains(keyword) with                            
                                          | true  -> capturedUrls.Add(req.Request.Url) 
                                          | false -> ()

                                          do! req.Request.ContinueAsync()                                                     
                                              |> Option.ofObj
                                              |> function
                                                  | Some value ->
                                                                value
                                                  | None       -> 
                                                                printfn "Error in scrapeLinks: %s" "task -> null"
                                                                printfn "Press any key to close this app"
                                                                Console.ReadKey() |> ignore 
                                                                System.Environment.Exit(1)  
                                                                req.Request.ContinueAsync()   
                                              |> Async.AwaitTask
                                      } 
                                      |> Async.Start
                        )          

                    urlList
                    |> List.iter
                        (fun url -> 
                                  async
                                      {
                                          try
                                              // Navigate to the target URL                                             
                                              do! page.GoToAsync(url)      
                                                  |> Option.ofObj
                                                  |> function
                                                      | Some value ->
                                                                    value
                                                      | None       -> 
                                                                    printfn "Error in scrapeLinks: %s" "response -> null"
                                                                    printfn "Press any key to close this app"
                                                                    Console.ReadKey() |> ignore 
                                                                    System.Environment.Exit(1)  
                                                                    page.GoToAsync(url)  
                                                  |> Async.AwaitTask      
                                                  |> Async.Ignore
                                              do! Task.Delay(5000) |> Async.AwaitTask
                                          with
                                          | ex ->
                                                printfn "Error navigating to %s: %s" url ex.Message
                                      } 
                                      |> Async.RunSynchronously
                        )   
                    // Close the browser
                    do! browser.CloseAsync() |> Async.AwaitTask

                    // Return the list of captured URLs as an array
                    return capturedUrls.ToArray() |> Array.toList
                with
                | ex ->
                      printfn "Error in captureNetworkRequest: %s" (string ex.Message)
                      printfn "Press any key to close this app"
                      Console.ReadKey() |> ignore 
                      System.Environment.Exit(1)  

                      return [] 
            }    
   

    let internal capturedLinks executablePath =        
    
        let resultingLinks = scrapeLinks executablePath |> Async.RunSynchronously 
        
        resultingLinks |> List.iter (printfn "Found link: %s")

        let urlList = 
               [
                   "https://www.kodis.cz/lines/train"
                   "https://www.kodis.cz/lines/region"
                   "https://www.kodis.cz/lines/city"
               ]
                       
        let capturedLinks1 = 
            captureNetworkRequest urlList executablePath
            |> Async.RunSynchronously
            |> List.distinct
            |> List.sort

        capturedLinks1 |> List.iter (printfn "Captured link: %s")

        printfn "************************************************************************"

        let capturedLinks2 = 
            captureNetworkRequest resultingLinks executablePath
            |> Async.RunSynchronously
            |> List.distinct
            |> List.sort

        capturedLinks2 |> List.iter (printfn "Captured link: %s")
        capturedLinks1, capturedLinks2