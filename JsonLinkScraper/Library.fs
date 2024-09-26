namespace Puppeteer

open System
open System.Threading.Tasks
open System.Collections.Generic

//*****************************

open MyFsToolkit
open PuppeteerSharp

//*****************************

module Settings = 

    let internal urlList = 
        [
            "https://www.kodis.cz/lines/train"
            "https://www.kodis.cz/lines/region"
            "https://www.kodis.cz/lines/city"           
        ]

    let internal keyword1 = "tab"
    let internal keyword2 = "linky-search"

//Toto je pouze kod pro overeni odkazu na JSON, pri normalnim behu aplikace se nepouziva
module Links = 

    open Settings

    let private closeTest () = 

        printfn "Press any key to close this app"
        Console.ReadKey() |> ignore 
        System.Environment.Exit(1)  

    let private scrapeLinks executablePath =
   
       async
           {
                //try with quli Async.AwaitTask, coz "snad" moze vyhodit exn
                try
                    // Define the URL and the keyword to filter the network requests   
                    let launchOptions =                       
                        LaunchOptions
                            (
                                Headless = true,
                                ExecutablePath = executablePath
                            )   
                        |> Option.ofNull
                        |> function
                            | Some value ->
                                          value
                            | None       -> 
                                          printfn "Error in scrapeLinks: %s" "LaunchOptions -> null"
                                          closeTest ()
                                          LaunchOptions
                                              (
                                                  Headless = true,
                                                  ExecutablePath = executablePath
                                              )                       
                   
                    use! browser =
                        Puppeteer.LaunchAsync(launchOptions)       
                        |> Option.ofNull
                        |> function
                            | Some value ->
                                          value
                            | None       -> 
                                          printfn "Error in scrapeLinks: %s" "browser -> null"
                                          closeTest () 
                                          Puppeteer.LaunchAsync(launchOptions)   
                        |> Async.AwaitTask   
                   
                    use! page =
                        browser.NewPageAsync()       
                        |> Option.ofNull
                        |> function
                            | Some value ->
                                          value
                            | None       -> 
                                          printfn "Error in scrapeLinks: %s" "page -> null"
                                          closeTest ()  
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
                                                      |> Option.ofNull
                                                      |> function
                                                          | Some value ->
                                                                        value
                                                          | None       -> 
                                                                        printfn "Error in scrapeLinks: %s" "response -> null"
                                                                        closeTest ()
                                                                        page.GoToAsync(url)
                                                      |> Async.AwaitTask      
                                                      |> Async.Ignore
                                                  
                                                  // Evaluate the page's DOM to extract all anchor tags
                                                  let! links =                                                                                                          
                                                      page.EvaluateExpressionAsync<string list>("Array.from(document.querySelectorAll('a')).map(a => a.href)")      
                                                      |> Option.ofNull
                                                      |> function
                                                          | Some value ->
                                                                        value
                                                          | None       -> 
                                                                        printfn "Error in scrapeLinks: %s" "task -> null"
                                                                        closeTest ()
                                                                        page.EvaluateExpressionAsync<string list>("Array.from(document.querySelectorAll('a')).map(a => a.href)")
                                                      |> Async.AwaitTask   

                                                  let filteredLinks = links |> List.filter _.Contains(keyword1)

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
                                          printfn "Error in captureNetworkRequest: %s" "LaunchOptions -> null"
                                          closeTest ()
                                          LaunchOptions
                                              (
                                                  Headless = true,
                                                  ExecutablePath = executablePath
                                              )  
                   
                    use! browser =
                        Puppeteer.LaunchAsync(launchOptions)       
                        |> Option.ofNull
                        |> function
                            | Some value ->
                                          value
                            | None       -> 
                                          printfn "Error in captureNetworkRequest: %s" "browser -> null"
                                          closeTest ()
                                          Puppeteer.LaunchAsync(launchOptions)   
                        |> Async.AwaitTask   
                   
                    use! page =
                        browser.NewPageAsync()       
                        |> Option.ofNull
                        |> function
                            | Some value ->
                                          value
                            | None       -> 
                                          printfn "Error in captureNetworkRequest: %s" "page -> null"
                                          closeTest ()
                                          browser.NewPageAsync()   
                        |> Async.AwaitTask 

                    // Set up request interception to monitor all network requests                    
                    do! page.SetRequestInterceptionAsync(true)                                                    
                        |> Option.ofNull
                        |> function
                            | Some value ->
                                          value
                            | None       -> 
                                          printfn "Error in captureNetworkRequest: %s" "task -> null"
                                          closeTest ()
                                          page.SetRequestInterceptionAsync(true)  
                        |> Async.AwaitTask
                                            
                    let capturedUrls = 
                        System.Collections.Concurrent.ConcurrentBag<string>()      
                        |> Option.ofNull
                        |> function
                            | Some value ->
                                          value
                            | None       -> 
                                          printfn "Error in captureNetworkRequest: %s" "capturedUrls -> null"
                                          closeTest ()
                                          System.Collections.Concurrent.ConcurrentBag<string>()  

                    // Event handler to intercept and analyze requests
                    page.Request.Add
                        (fun req ->                    
                                  async 
                                      {
                                          match req.Request.Url.Contains(keyword2) with                            
                                          | true  -> capturedUrls.Add(req.Request.Url) 
                                          | false -> ()

                                          do! req.Request.ContinueAsync()                                                     
                                              |> Option.ofNull
                                              |> function
                                                  | Some value ->
                                                                value
                                                  | None       -> 
                                                                printfn "Error in scrapeLinks: %s" "task -> null"
                                                                closeTest ()
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
                                                  |> Option.ofNull
                                                  |> function
                                                      | Some value ->
                                                                    value
                                                      | None       -> 
                                                                    printfn "Error in scrapeLinks: %s" "response -> null"
                                                                    closeTest ()
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
                      closeTest ()
                      return [] 
            }       

    let internal capturedLinks executablePath =        
    
        let resultingLinks = scrapeLinks executablePath |> Async.RunSynchronously |> List.distinct
        
        resultingLinks |> List.iter (printfn "Found link: %s")    
        printfn "%s" <| String.replicate 70 "*"
        
        let capturedLinks links executablePath = 

            captureNetworkRequest links executablePath
            |> Async.RunSynchronously
            |> List.distinct
            |> List.sort
                       
        let capturedLinks1 = capturedLinks urlList executablePath
          
        capturedLinks1 |> List.iter (printfn "Captured link 1 : %s")
        printfn "%s" <| String.replicate 70 "*"

        let resultingLinks = List.append capturedLinks1 resultingLinks |> List.distinct
        let capturedLinks2 = capturedLinks resultingLinks executablePath
          
        capturedLinks2 |> List.iter (printfn "Captured link 2 : %s")

        capturedLinks2