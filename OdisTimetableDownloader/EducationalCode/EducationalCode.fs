namespace EducationalCode

(*
type Option<'T> =
    | Some of 'T
    | None
*)

module OptionModule =

    let map f option =
        match option with
        | Some value -> Some (f value)
        | None       -> None

    let bind f option =
        match option with
        | Some value -> f value
        | None       -> None
   
    let ofObj (value: 'T when 'T : null) : 'T option =  //lze bez obav nahradit Option.ofNull
        match value with
        | null -> None
        | _    -> Some value   
     
    //pro nullable value type (typicky z knihovny psane v C#) 
    let ofNullable (nullableValue: System.Nullable<'T>) : Option<'T> = //lze bez obav nahradit Option.ofNull
        match nullableValue.HasValue with
        | true  -> Some nullableValue.Value
        | false -> None

    let internal ofNull (value : 'nullableValue) =
        match System.Object.ReferenceEquals(value, null) with //The "value" type can be even non-nullable, and ReferenceEquals will still work.
        | true  -> None
        | false -> Some value     

    (*
    The Option.ofNull function uses System.Object.ReferenceEquals(objA, null) to check for nullity.
    This method is capable of checking for null regardless of the type of value, including both reference types and value types (like Nullable<T>).
    If objA is a value type, it is boxed before it is passed to the ReferenceEquals method. 
    
    Boxing is the process of converting a value type to the type object.
    Unboxing extracts the value type from the object. 
    *)   

    (*
    let internal ofNullEmpty (value : 'nullableValue) = //NullOrEmpty

        pyramidOfHell
            {
                let!_ = not <| System.Object.ReferenceEquals(value, null), None 
                let value = string value 
                let! _ = not <| String.IsNullOrEmpty(value), None 

                return Some value
            }

    let internal ofNullEmptySpace (value : 'nullableValue) = //NullOrEmpty, NullOrWhiteSpace
    
        pyramidOfHell
            {
                let!_ = not <| System.Object.ReferenceEquals(value, null), None 
                let value = string value 
                let! _ = not <| (String.IsNullOrEmpty(value) || String.IsNullOrWhiteSpace(value)), None
    
                return Some value
            } 
    *)

module ResultModule =

    let map f result =
        match result with
        | Ok value    -> Ok (f value)
        | Error error -> Error error

    let bind f result =
        match result with
        | Ok value    -> f value
        | Error error -> Error error

type ReaderEC<'env,'a> = ReaderEC of action:('env -> 'a)
        
module ReaderEC =
    /// Run a ReaderEC with a given environment
    let run env (ReaderEC action)  =
        action env  // simply call the inner function

    /// Create a ReaderEC which returns the environment itself
    let ask = ReaderEC id

    /// Map a function over a ReaderEC
    let map f reader =
        ReaderEC (fun env -> f (run env reader))

    /// flatMap a function over a Reader
    let bind f reader =
        let newAction env =
            let x = run env reader
            run env (f x)
        ReaderEC newAction

(*
    let list1 = [1; 2; 3; 4; 5]
    let list2 = [3; 4; 5; 6; 7]

    let intersectedList = List.filter (fun item -> List.contains item list2) list1
*)


(*
namespace ClassLibrary1
{
    public class Class1
    {
        public int? GetNullableInt(bool returnNull)
        {
            if (returnNull)
            {
                return null; // Return null if the condition is true
            }
            else
            {
                return 42; // Return a non-nullable int value
            }
        }

        public string GetEmptyString(bool returnStringEmpty)
        {
            if (returnStringEmpty)
            {
                return String.Empty; 
            }
            else
            {
                return "42"; 
            }
        }
    }
}

open ClassLibrary1

[<EntryPoint>] 
let main argv =  
    
    let nullableValueTypeInstance = new Class1()
    let nullableValueTrue = nullableValueTypeInstance.GetNullableInt(true)
    printfn "true %A" nullableValueTrue

    let nullableValueFalse =  nullableValueTypeInstance.GetNullableInt(false)
    printfn "false %A" nullableValueFalse

    let testOfNull = nullableValueTrue |> Option.ofNull

    match testOfNull with
    | Some value -> printfn "Some %A" value
    | None       -> printfn "None %s" "Funguje to"

    let testOfNull = nullableValueFalse |> Option.ofNull

    match testOfNull with
    | Some value -> printfn "Some %A" value
    | None       -> printfn "None %s" "Nefunguje to"

    let stringEmptyTestInstance = new Class1()
    let stringEmptyTestTrue = stringEmptyTestInstance.GetEmptyString(true)

    printfn "string true %A" stringEmptyTestTrue

    let stringEmptyTestFalse = stringEmptyTestInstance.GetEmptyString(false)

    printfn "string false %A" stringEmptyTestFalse

    let testOfEmptiness = stringEmptyTestTrue |> Option.ofNull

    match testOfEmptiness with
    | Some value -> printfn "string Some %A" "Nefunguje to"
    | None       -> printfn "string None %s" "Funguje to"

    let testOfEmptiness = stringEmptyTestTrue |> Option.ofNullEmpty

    match testOfEmptiness with
    | Some value -> printfn "string Some %A" "Nefunguje to"
    | None       -> printfn "string None %s" "Funguje to"
    0     
*)