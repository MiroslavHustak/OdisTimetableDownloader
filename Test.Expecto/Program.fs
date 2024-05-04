namespace Test.Expecto

open Expecto
open Test.Expecto.ExpectoTests

module Program = 
        
   //Nezapomenout na YoloDev.Expecto.TestSdk pro zobrazovani testu v Test Explorer
   
   let [<EntryPoint>] main _ = runTestsWithCLIArgs [] [||] all     

