namespace EducationalCode

module OptionModule =

    let map f option =
        match option with
        | Some value -> Some (f value)
        | None       -> None

    let bind f option =
        match option with
        | Some value -> f value
        | None       -> None
   
    let ofObj (value: 'T when 'T : null) : 'T option = 
        match value with
        | null -> None
        | _    -> Some value    

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


