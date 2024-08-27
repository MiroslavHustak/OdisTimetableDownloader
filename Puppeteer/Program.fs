namespace Puppeteer

open System
open System.Threading.Tasks
open System.Collections.Generic

open PuppeteerSharp


//TODO: Option types 
module Links = 

    let private scrapeLinks () =
   
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
                                ExecutablePath = @"c:\Program Files\AVG\Browser\Application\AVGBrowser.exe" 
                            ) 
                            //|> Option.ofObj

                    // Puppeteer.LaunchAsync(launchOptions) |> Option.ofObj                
                    use! browser = Puppeteer.LaunchAsync(launchOptions) |> Async.AwaitTask
                    //browser.NewPageAsync() |> Option.ofObj
                    use! page = browser.NewPageAsync() |> Async.AwaitTask 

                    // Collect all filtered links across all pages
                    let allFilteredLinks = 
                        urlList 
                        |> List.map 
                            (fun url -> 
                                      async 
                                          {
                                              try
                                                  // Navigate to the target URL
                                                  //page.GoToAsync(url)  |> Option.ofObj
                                                  do! page.GoToAsync(url) |> Async.AwaitTask |> Async.Ignore

                                                  // Evaluate the page's DOM to extract all anchor tags
                                                  let! links =
                                                     //page.EvaluateExpressionAsync<string list>("Array.from(document.querySelectorAll('a')).map(a => a.href)") |> Option.ofObj
                                                     page.EvaluateExpressionAsync<string list>("Array.from(document.querySelectorAll('a')).map(a => a.href)")
                                                     |> Async.AwaitTask

                                                  let filteredLinks = links |> List.filter (fun link -> link.Contains(keyword))

                                                  return filteredLinks
                                              with
                                              | ex -> 
                                                    printfn "Error scraping %s: %s" url ex.Message
                                                    return [] // Return an empty array on error
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

    let private captureNetworkRequest urlList =

        async 
            {   
                try              
                    let keyword = "linky-search"

                    let launchOptions = 
                        LaunchOptions
                            (
                                Headless = true, 
                                ExecutablePath = @"c:\Program Files\AVG\Browser\Application\AVGBrowser.exe" 
                            )
                            //|> Option.ofObj

                    // Puppeteer.LaunchAsync(launchOptions) |> Option.ofObj                
                    use! browser = Puppeteer.LaunchAsync(launchOptions) |> Async.AwaitTask
                    //browser.NewPageAsync() |> Option.ofObj
                    use! page = browser.NewPageAsync() |> Async.AwaitTask 

                    // Set up request interception to monitor all network requests
                    // page.SetRequestInterceptionAsync(true) |> Option.ofObj
                    do! page.SetRequestInterceptionAsync(true) |> Async.AwaitTask 

                    let capturedUrls = System.Collections.Concurrent.ConcurrentBag<string>() //|> Option.ofObj

                    // Event handler to intercept and analyze requests
                    page.Request.Add
                        (fun req ->                    
                                  async 
                                      {
                                          match req.Request.Url.Contains(keyword) with                            
                                          | true  -> capturedUrls.Add(req.Request.Url) 
                                          | false -> ()
                                      
                                          //req.Request.ContinueAsync() |> Option.ofObj
                                          do! req.Request.ContinueAsync() |> Async.AwaitTask
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
                                              //page.GoToAsync(url) |> Option.ofObj
                                              do! page.GoToAsync(url) |> Async.AwaitTask |> Async.Ignore
                                              // Wait for a few seconds to ensure all network requests are captured
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

    let internal capturedLinks () =        
    
        let resultingLinks = scrapeLinks () |> Async.RunSynchronously    
        resultingLinks |> List.iter (printfn "Found link: %s")

        let urlList = 
               [
                   "https://www.kodis.cz/lines/train"
                   "https://www.kodis.cz/lines/region"
                   "https://www.kodis.cz/lines/city"
               ]
                       
        let capturedLinks1 = 
            captureNetworkRequest urlList
            |> Async.RunSynchronously
            |> List.distinct
            |> List.sort

        capturedLinks1 |> List.iter (printfn "Captured link: %s")

        printfn "************************************************************************"

        let capturedLinks2 = 
            captureNetworkRequest resultingLinks
                |> Async.RunSynchronously
                |> List.distinct
                |> List.sort

        capturedLinks2 |> List.iter (printfn "Captured link: %s")
        capturedLinks1, capturedLinks2