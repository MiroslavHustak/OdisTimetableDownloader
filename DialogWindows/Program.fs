module BrowserDialogWindow

open System

let internal openFolderBrowserDialog() = 

    try 
        let folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog() 

        folderBrowserDialog.SelectedPath <- Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        folderBrowserDialog.Description <- "Select a folder"

        let result = folderBrowserDialog.ShowDialog()
        
        match result = System.Windows.Forms.DialogResult.OK with
        | true  -> Ok (folderBrowserDialog.SelectedPath, false)
        | false -> Ok (String.Empty, true)         
    with
    | _ -> Error ("Chyba při pokusu o vybrání adresáře.", true)

    |> function 
        | Ok value  -> value
        | Error err -> err
         
               

    
