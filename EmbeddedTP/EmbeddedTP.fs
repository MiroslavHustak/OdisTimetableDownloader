namespace EmbeddedTP

open FSharp.Data

module EmbeddedTP =
     
    let [<Literal>] ResolutionFolder = __SOURCE_DIRECTORY__ 

    type JsonProvider1 =
        JsonProvider<"KODISJson/kodisMHDTotal.json", EmbeddedResource = "EmbeddedTP, EmbeddedTP.KODISJson.kodisMHDTotal.json", ResolutionFolder = ResolutionFolder>

    type JsonProvider2 =
        JsonProvider<"KODISJson/kodisMHDTotal2_0.json", EmbeddedResource = "EmbeddedTP, EmbeddedTP.KODISJson.kodisMHDTotal2_0.json", ResolutionFolder = ResolutionFolder>
