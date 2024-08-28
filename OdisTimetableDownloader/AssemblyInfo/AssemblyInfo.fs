namespace AssemblyInfo

open System.Runtime.CompilerServices

module AssemblyInfo =

    [<assembly : InternalsVisibleTo("Test.xUnit")>]
    [<assembly : InternalsVisibleTo("Test.Expecto")>]
    [<assembly : InternalsVisibleTo("Test.FsCheck")>]
    do ()