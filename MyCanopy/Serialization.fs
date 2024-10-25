namespace Serialization

open System.IO

open Thoth.Json.Net

open MyFsToolkit
open MyFsToolkit.Builders

open Serialization.Coders.ThothCoders

// Implement 'try with' block for serialization at each location in the code where it is used.
module Serialisation =
   
    //Thoth.Json.Net, Thoth.Json + StreamWriter (System.IO (File.WriteAllText) did not work)    
    let internal serializeToJsonThoth2 (list : string list) (jsonFile : string) =

        pyramidOfDoom
            {
                let filepath = Path.GetFullPath jsonFile |> Option.ofNullEmpty 
                let! filepath = filepath, Error (sprintf "%s%s" "Zadané hodnoty nebyly uloženy, chyba při čtení cesty k souboru " jsonFile)
    
                let json = Encode.toString 2 (encoder list) |> Option.ofNullEmpty // Serialize the record to JSON with indentation, 2 = the number of spaces used for indentation in the JSON structure
                let! json = json, Error (sprintf "%s%s" "Zadané hodnoty nebyly uloženy, chyba při serializaci do " jsonFile)
    
                use writer = new StreamWriter(filepath, false)                
                let! _ = writer |> Option.ofNull, Error (sprintf "%s%s" "Zadané hodnoty nebyly uloženy, chyba při serializaci do " jsonFile)

                writer.Write json

                return Ok ()
            }
    
//Implement 'try with' block for deserialization at each location in the code where it is used.
module Deserialisation =    

       //Thoth.Json.Net, Thoth.Json + StreamReader
    let internal deserializeFromJsonThoth2<'a> (jsonFile : string) =

        pyramidOfDoom
            {
                let filepath = Path.GetFullPath jsonFile |> Option.ofNullEmpty 
                let! filepath = filepath, Error (sprintf "%s%s" "Pro zobrazování navrhovaných a předchozích hodnot odkazů byly dosazeny defaultní hodnoty, chyba při čtení cesty k souboru " jsonFile)

                let fInfodat : FileInfo = FileInfo filepath
                let! _ =  fInfodat.Exists |> Option.ofBool, Error (sprintf "Pro zobrazování navrhovaných a předchozích hodnot odkazů byly dosazeny defaultní hodnoty, soubor %s nenalezen" jsonFile) 
                 
                use fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.None) 
                let! _ = fs |> Option.ofNull, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " filepath)                        
                    
                use reader = new StreamReader(fs) //For large files, StreamReader may offer better performance and memory efficiency
                let! _ = reader |> Option.ofNull, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " filepath) 
                
                let json = reader.ReadToEnd()
                let! json = json |> Option.ofNullEmpty, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " filepath)  
                    
                let result = Decode.fromString decoder json  //Thoth does not use reflection  
                                  
                return result //Thoth output is of Result type 
            }