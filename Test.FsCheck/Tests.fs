namespace Test.FsCheck

open System

open FsCheck
open FsCheck.Xunit

module TestInputs =

    let missingKeyList = 
        [
            1; 10; 11; 14; 15; 16; 17; 18; 20; 21; 22; 23; 24; 25; 28; 29; 30; 31; 
            40; 41; 42; 43; 44; 45; 46; 47; 48; 49; 50; 51; 52; 53; 54; 55; 56; 57; 
            58; 59; 60; 61; 62; 63; 64; 65; 66; 67; 68; 69; 70; 71; 72; 73; 74; 75; 
            76; 77; 78; 79; 80; 81; 82; 83; 84; 85; 86; 87; 88; 89; 90; 91; 92; 93; 
            94; 95; 96; 97; 98; 99; 100; 101; 102; 103; 104; 105; 106; 107; 108; 109; 
            110; 111; 112; 113; 114; 115; 116; 117; 118; 119; 120; 121; 122; 123; 124; 
            125; 126; 127; 128; 129; 130; 131; 132; 133; 134; 135; 136; 137; 138; 139; 
            140; 141; 142; 143; 144; 145; 146; 147; 148; 149; 150; 151; 152; 153; 154; 
            155; 156; 157; 158; 159; 161; 162; 163; 164; 165; 166; 167; 168; 169; 170; 
            171; 172; 173; 174; 175; 179; 180; 181; 182; 183; 184; 185; 187; 188; 190; 
            191; 192; 193; 195; 196; 197; 198; 199; 200; 201; 202; 203; 204; 205; 206; 
            207; 208; 209; 210; 211; 212; 213; 214; 215; 216; 217; 218; 219; 220; 221; 
            222; 223; 224; 225; 227; 228
        ]

    let keyList = //keyboard
        [
            0; 2; 3; 4; 5; 6; 7; 8; 9; 12; 13; 19; 26; 27; 32; 33; 34; 35; 36; 37; 38; 
            39; 157; 160; 176; 177; 178; 186; 189; 194; 224; 225; 226; 230; 231; 232; 233; 
            234; 235; 236; 237; 238; 239; 240; 241; 242; 243; 244; 245; 246; 247; 248; 249; 
            250; 251; 252; 253; 254
        ]        
     
    let rec variant (param : int) =   //just arbitrary educational code

        let randomIndex = (new Random()).Next(0, List.length keyList)
        let param1 = List.item randomIndex keyList        

        param
        |> function 
            | 49  -> true    //"1"                                         
            | 50  -> true    //"2"                                         
            | 51  -> true    //"3"  
            | 27  -> true    //Esc
            | _   -> variant param1  

//FsCheck with xUnit.NET 
module FsCheckTests =

    open TestInputs

    let private rndArbGen = //just arbitrary educational code
        Gen.choose (0, 254) 
        |> Gen.map 
            (fun item ->   
                       let l = List.length keyList   

                       match item < l with
                       | true  -> 
                                let randomIndex = (new Random()).Next(0, List.length keyList)
                                List.item randomIndex keyList  
                       | false ->  
                                0
            ) |> Arb.fromGen
        
    [<Property>]
    let private ``201 Property based test (FsCheckTest)`` (item : int) = //parametr nesmi byt generics
                                  
        Prop.forAll rndArbGen //just arbitrary educational code
            (fun item ->                             
                       let result =                                 
                           try                                 
                               variant item                                    
                           with
                           | ex -> failwith (sprintf "FsCheckError: %s" ex.Message)                                                     

                       lazy (result = true)  
            )  
            
//FsCheck will use values from a custom generator (rndArbGen in my case) for most of the test, but it can also introduce
//unexpected values such as nulls, empty strings, or other edge cases to ensure that the code handles a wide range of inputs correctly.
//So even if my custom generator creates 100 test string values, FsCheck may decide to use, let's say, 95 of them, 
//and will create 5 other values (nulls, empty strings, etc.) using them at its pleasure. 