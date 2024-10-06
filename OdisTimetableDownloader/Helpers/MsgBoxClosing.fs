namespace Helpers

open System
open System.Windows.Forms
open System.Runtime.InteropServices

open FSharp.Control

//*************************************

open Types 
open Settings.Messages

module MsgBoxClosing =  

    // Pouzivat msg boxes v konzolove app se mi nejevi jako vhodne, ale procvicil jsem si alespon async a tokens :-). 

    [<DllImport("user32.dll", CharSet = CharSet.Auto)>]
    extern int private SendMessage(IntPtr hWnd, uint msg, int wParam, IntPtr lParam)

    [<DllImport("user32.dll", SetLastError = true)>]
    extern IntPtr private FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle)

    [<DllImport("user32.dll", SetLastError = true)>]
    extern IntPtr private FindWindow(string lpClassName, string lpWindowName)    

    let [<Literal>] private WM_CLOSE = 16u
    let [<Literal>] private WM_LBUTTONDOWN = 0x0201u
    let [<Literal>] private WM_LBUTTONUP = 0x0202u
    
    let private sendMessageMethod (hwnd: IntPtr) =
            
        let hwndChild = //handle
            match (<>) hwnd IntPtr.Zero with
            | true  -> FindWindowEx(hwnd, IntPtr.Zero, "Button", "OK")
            | false -> IntPtr.Zero
           
        SendMessage(hwndChild, WM_LBUTTONDOWN, 0, IntPtr.Zero) |> ignore
        SendMessage(hwndChild, WM_LBUTTONUP, 0, IntPtr.Zero) |> ignore
        SendMessage(hwndChild, WM_LBUTTONDOWN, 0, IntPtr.Zero) |> ignore
        SendMessage(hwndChild, WM_LBUTTONUP, 0, IntPtr.Zero) |> ignore
    
    let private sendMessageMethodX (hwndX: IntPtr) =

         match (<>) hwndX IntPtr.Zero with
         | true  -> SendMessage(hwndX, WM_CLOSE, 0, IntPtr.Zero) |> ignore //WM_CLOSE to je ten krizek
         | false -> ()

    let private clickOnOKButton boxTitle =    
        
        let hwnd = FindWindow("#32770", boxTitle) //kliknuti na OK
        sendMessageMethod hwnd      
        
        let hwndX = FindWindow("#32770", boxTitle) //kliknuti na krizek (pro jistotu)
        sendMessageMethodX hwndX

    let private findMsgBox boxTitle =    
        
        let hwnd = FindWindow("#32770", boxTitle)
        hwnd <> IntPtr.Zero 

    let private boxJsonTitle = "No jéje, zase problém ..."
    let private boxPdfTitle = "No jéje, další problém ..."

    //For educational purposes
    let private agent () = 

        MailboxProcessor.Start <|
            fun inbox 
                ->          
                 let rec messageLoop () =
                     async
                         {
                             let! msg = inbox.Receive() 
                             return! messageLoop ()
                         }
                       
                 messageLoop ()             
            
    let internal processorPdf =

        MailboxProcessor.Start <|
           fun inbox 
               ->
                let rec loop n =
                    async
                        {                              
                            match! inbox.Receive() with
                            | Incr i            
                                -> 
                                 match (=) n 0 with
                                 | false -> 
                                          do! Async.Sleep 360000
                                 | true  -> 
                                          clickOnOKButton boxJsonTitle

                                          MessageBox.Show
                                              (
                                                  msg17 (), 
                                                  boxPdfTitle, 
                                                  MessageBoxButtons.OK
                                              )
                                          |> function
                                              | DialogResult.OK -> System.Environment.Exit 1                                                                               
                                              | _               -> ()   
                                          return! loop (n + i)

                            | Fetch replyChannel
                                ->
                                 replyChannel.Reply n 
                                 return! loop n
                        }
                loop 0           

    let internal processorJson () (waitingTime : int) = 

        MailboxProcessor.Start <|
            fun inbox 
                ->
                 let rec loop isFirst =
                     async
                         {                               
                             match! inbox.Receive() with
                             | First x when isFirst //flag momentalne nepotrebny, ale ponechavam pro mozne vyuziti pri zmene
                                 ->                 //parametr x ve flag momentalne nepotrebny, ale ponechavam pro mozne vyuziti pri zmene
                                  let result () =
                                      let result = 
                                          MessageBox.Show
                                              (
                                                  msg18, 
                                                  boxJsonTitle, 
                                                  MessageBoxButtons.OK
                                              ) 

                                      AsyncSeq.initInfinite (fun _ -> result <> DialogResult.OK)
                                      |> AsyncSeq.takeWhile ((<>) true) 
                                      |> AsyncSeq.iterAsync (fun _ -> async { do! Async.Sleep waitingTime }) 
                                      |> Async.StartImmediate 
                                            
                                  let rec keepOneMsgBox () = 
                                      match findMsgBox boxJsonTitle with
                                      | true  -> 
                                               clickOnOKButton boxJsonTitle
                                               keepOneMsgBox ()
                                      | false ->                                                                
                                               result ()   
                                                 
                                  keepOneMsgBox ()  
                                     
                                  return! loop false // Set isFirst to false to ignore subsequent messages

                             | _ ->                      
                                  return! loop isFirst
                         }

                 loop true // Start with isFirst set to true