namespace Serialization.Coders

open Thoth.Json.Net

module ThothCoders =

    let internal encoder list = Encode.object [ "list", list |> List.map Encode.string |> Encode.list ]

    //Decode.field by melo byt OK, nemusi byt Decode.object
    let internal decoder : Decoder<string list> = Decode.field "list" (Decode.list Decode.string)

   