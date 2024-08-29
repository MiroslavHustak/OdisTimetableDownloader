namespace AssemblyInfo

module AssemblyInfo =

    open System.Runtime.CompilerServices

    [<assembly : InternalsVisibleTo("OdisTimetableDownloader")>]
    [<assembly : InternalsVisibleTo("DtDbVariantTest")>]
    [<assembly : InternalsVisibleTo("JsonLinkScraper")>]
    do ()