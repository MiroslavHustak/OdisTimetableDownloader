namespace AssemblyInfo

module AssemblyInfo =

    open System.Runtime.CompilerServices

    [<assembly : InternalsVisibleTo("OdisTimetableDownloader")>]
    [<assembly : InternalsVisibleTo("DtDbMVariantTest")>]
    [<assembly : InternalsVisibleTo("JsonLinkScraper")>]
    do ()