﻿namespace Settings

open System

module SettingsKODIS =

    //************************Constants and types**********************************************************************

    //tu a tam zkontrolovat json, zdali KODIS nezmenil jeho strukturu 
    //pro type provider musi byt konstanta (nemozu pouzit sprintf partialPathJson) a musi byt forward slash"

    //Tohle nefunguje s json type provider
    let internal pathJsonNotWorkingForTypeProviders = 
        try
            let path = AppDomain.CurrentDomain.BaseDirectory + "KODISJson" + @"/kodisMHDTotal.json" //Copy Always     
            path
        with
        | ex -> sprintf "Some mysterious exception: %s" ex.Message  

    //let [<Literal>] internal pathJson = @"KODISJson/kodisMHDTotal.json"      // nahrazeno dll EmbeddedTP
    //let [<Literal>] internal pathJson2 = @"KODISJson/kodisMHDTotal2_0.json"  // nahrazeno dll EmbeddedTP

    let [<Literal>] internal partialPathJson = @"KODISJson/" //v binu //tohle je pro stahovane json, ne pro type provider

    let [<Literal>] internal pathKodisWeb = @"https://kodisweb-backend.herokuapp.com/"
    let [<Literal>] internal pathKodisWeb2 = @"https://kodis-backend-staging-85d01eccf627.herokuapp.com/api/linky-search?"
    let [<Literal>] internal pathKodisAmazonLink = @"https://kodis-files.s3.eu-central-1.amazonaws.com/" 
    let [<Literal>] internal lineNumberLength = 3 //3 je delka retezce pouze pro linky 001 az 999

    let internal sortedLines =
        [ 
            "linky 001-199"; "linky 200-299"; "linky 300-399"; 
            "linky 400-499"; "linky 500-599"; "linky 600-699"; 
            "linky 700-799"; "linky 800-899"; "linky 900-999"; 
            "vlakové linky S"; "vlakové linky R"; "linky X, NAD, AE, A, B"
        ]
    
    let internal jsonLinkList = //pri zmene jsonu na strankach KODISu zmenit aji nazev souboru, napr. kodisRegion3001.json
        [
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=12&group_in%5B0%5D=MHD%20Bruntál&group_in%5B1%5D=MHD%20Český%20Těšín&group_in%5B2%5D=MHD%20Frýdek-Místek&group_in%5B3%5D=MHD%20Havířov&group_in%5B4%5D=MHD%20Karviná&group_in%5B5%5D=MHD%20Krnov&group_in%5B6%5D=MHD%20Nový%20Jičín&group_in%5B7%5D=MHD%20Opava&group_in%5B8%5D=MHD%20Orlová&group_in%5B9%5D=MHD%20Ostrava&group_in%5B10%5D=MHD%20Studénka&group_in%5B11%5D=MHD%20Třinec&group_in%5B12%5D=NAD%20MHD&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=MHD%20Bruntál&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=MHD%20Český%20Těšín&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=MHD%20Frýdek-Místek&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=MHD%20Havířov&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=MHD%20Karviná&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=MHD%20Krnov&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=MHD%20Nový%20Jičín&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=MHD%20Opava&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=MHD%20Orlová&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=12&group_in%5B0%5D=MHD%20Ostrava&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=24&group_in%5B0%5D=MHD%20Ostrava&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=36&group_in%5B0%5D=MHD%20Ostrava&_sort=numeric_label" 
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=48&group_in%5B0%5D=MHD%20Ostrava&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=60&group_in%5B0%5D=MHD%20Ostrava&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=72&group_in%5B0%5D=MHD%20Ostrava&_sort=numeric_label" 
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=84&group_in%5B0%5D=MHD%20Ostrava&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=MHD%20Studénka&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=MHD%20Třinec&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=NAD%20MHD&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=75&group_in%5B1%5D=232-293&group_in%5B2%5D=331-392&group_in%5B3%5D=440-465&group_in%5B4%5D=531-583&group_in%5B5%5D=613-699&group_in%5B6%5D=731-788&group_in%5B7%5D=811-885&group_in%5B8%5D=901-990&group_in%5B9%5D=NAD&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=75&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=232-293&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=331-392&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=440-465&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=531-583&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=12&group_in%5B0%5D=613-699&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=731-788&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=811-885&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=901-990&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=NAD&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=12&group_in%5B0%5D=S1-S34&group_in%5B1%5D=R8-R61&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=S1-S34&group_in%5B1%5D=R8-R62&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=S1-S34&_sort=numeric_label"
            sprintf "%s%s" pathKodisWeb @"linky?_limit=12&_start=0&group_in%5B0%5D=NAD&_sort=numeric_label" 
        ]  
        
    let internal pathToJsonList =     
        [
            sprintf "%s%s" partialPathJson @"kodisMHDTotal1.json"
            sprintf "%s%s" partialPathJson @"kodisMHDBruntal.json"
            sprintf "%s%s" partialPathJson @"kodisMHDCT.json"
            sprintf "%s%s" partialPathJson @"kodisMHDFM.json"
            sprintf "%s%s" partialPathJson @"kodisMHDHavirov.json"
            sprintf "%s%s" partialPathJson @"kodisMHDKarvina.json"
            sprintf "%s%s" partialPathJson @"kodisMHDBKrnov.json"
            sprintf "%s%s" partialPathJson @"kodisMHDNJ.json"
            sprintf "%s%s" partialPathJson @"kodisMHDOpava.json"
            sprintf "%s%s" partialPathJson @"kodisMHDOrlova.json"
            sprintf "%s%s" partialPathJson @"kodisMHDOstrava0.json"
            sprintf "%s%s" partialPathJson @"kodisMHDOstrava1.json"
            sprintf "%s%s" partialPathJson @"kodisMHDOstrava2.json"
            sprintf "%s%s" partialPathJson @"kodisMHDOstrava3.json"
            sprintf "%s%s" partialPathJson @"kodisMHDOstrava4.json"
            sprintf "%s%s" partialPathJson @"kodisMHDOstrava5.json"
            sprintf "%s%s" partialPathJson @"kodisMHDOstrava6.json"
            sprintf "%s%s" partialPathJson @"kodisMHDStudenka.json"
            sprintf "%s%s" partialPathJson @"kodisMHDTrinec.json"
            sprintf "%s%s" partialPathJson @"kodisMHDNAD.json"
            sprintf "%s%s" partialPathJson @"kodisRegionTotal.json"
            sprintf "%s%s" partialPathJson @"kodisRegion75.json"
            sprintf "%s%s" partialPathJson @"kodisRegion200.json"
            sprintf "%s%s" partialPathJson @"kodisRegion3001.json"
            sprintf "%s%s" partialPathJson @"kodisRegion400.json"
            sprintf "%s%s" partialPathJson @"kodisRegion500.json"
            sprintf "%s%s" partialPathJson @"kodisRegion600.json"
            sprintf "%s%s" partialPathJson @"kodisRegion700.json"
            sprintf "%s%s" partialPathJson @"kodisRegion800.json"
            sprintf "%s%s" partialPathJson @"kodisRegion900.json"
            sprintf "%s%s" partialPathJson @"kodisRegionNAD.json"
            sprintf "%s%s" partialPathJson @"kodisTrainTotal1.json"
            sprintf "%s%s" partialPathJson @"kodisTrainTotal2.json"
            sprintf "%s%s" partialPathJson @"kodisTrainPomaliky.json"
            sprintf "%s%s" partialPathJson @"kodisNAD.json"
        ]      

    let internal jsonLinkList2 =
        [
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=MHD%20Bruntál&groups%5B1%5D=MHD%20Český%20Těšín&groups%5B2%5D=MHD%20Frýdek-Místek&groups%5B3%5D=MHD%20Havířov&groups%5B4%5D=MHD%20Karviná&groups%5B5%5D=MHD%20Krnov&groups%5B6%5D=MHD%20Nový%20Jičín&groups%5B7%5D=MHD%20Opava&groups%5B8%5D=MHD%20Orlová&groups%5B9%5D=MHD%20Ostrava&groups%5B10%5D=MHD%20Studénka&groups%5B11%5D=MHD%20Třinec&groups%5B12%5D=NAD%20MHD&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=MHD%20Bruntál&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=MHD%20Český%20Těšín&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=MHD%20Frýdek-Místek&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=MHD%20Havířov&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=MHD%20Karviná&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=MHD%20Krnov&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=MHD%20Nový%20Jičín&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=MHD%20Opava&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=MHD%20Orlová&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=MHD%20Ostrava&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=MHD%20Studénka&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=MHD%20Třinec&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=NAD%20MHD&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=75&groups%5B1%5D=232-293&groups%5B2%5D=331-392&groups%5B3%5D=440-465&groups%5B4%5D=531-583&groups%5B5%5D=613-699&groups%5B6%5D=731-788&groups%5B7%5D=811-885&groups%5B8%5D=901-990&groups%5B9%5D=NAD&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=75&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=232-293&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=331-392&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=440-465&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=531-583&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=613-699&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=731-788&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=811-885&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=901-990&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=S1-S34&groups%5B1%5D=R8-R62&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=S1-S34&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=R8-R62&start=0&limit=12"            
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=NAD&start=0&limit=12"
            sprintf "%s%s" pathKodisWeb2 "groups%5B0%5D=613-699&start=48&limit=12"
        ]
    
    let internal pathToJsonList2 =     
        [
            sprintf "%s%s" partialPathJson @"kodisMHDTotal2_0.json"
            sprintf "%s%s" partialPathJson @"kodisMHDBruntal2_0.json"
            sprintf "%s%s" partialPathJson @"kodisMHDCT2_0.json"
            sprintf "%s%s" partialPathJson @"kodisMHDFM2_0.json"
            sprintf "%s%s" partialPathJson @"kodisMHDHavirov2_0.json"
            sprintf "%s%s" partialPathJson @"kodisMHDKarvina2_0.json"
            sprintf "%s%s" partialPathJson @"kodisMHDBKrnov2_0.json"
            sprintf "%s%s" partialPathJson @"kodisMHDNJ2_0.json"
            sprintf "%s%s" partialPathJson @"kodisMHDOpava2_0.json"
            sprintf "%s%s" partialPathJson @"kodisMHDOrlova2_0.json"
            sprintf "%s%s" partialPathJson @"kodisMHDOstrava2_0.json"            
            sprintf "%s%s" partialPathJson @"kodisMHDStudenka2_0.json"
            sprintf "%s%s" partialPathJson @"kodisMHDTrinec2_0.json"
            sprintf "%s%s" partialPathJson @"kodisMHDNAD2_0.json"
            sprintf "%s%s" partialPathJson @"kodisRegionTotal2_0.json"
            sprintf "%s%s" partialPathJson @"kodisRegion752_0.json"
            sprintf "%s%s" partialPathJson @"kodisRegion2002_0.json"
            sprintf "%s%s" partialPathJson @"kodisRegion3002_0.json"
            sprintf "%s%s" partialPathJson @"kodisRegion4002_0.json"
            sprintf "%s%s" partialPathJson @"kodisRegion5002_0.json"
            sprintf "%s%s" partialPathJson @"kodisRegion6002_0.json"
            sprintf "%s%s" partialPathJson @"kodisRegion7002_0.json"
            sprintf "%s%s" partialPathJson @"kodisRegion8002_0.json"
            sprintf "%s%s" partialPathJson @"kodisRegion9002_0.json"
            sprintf "%s%s" partialPathJson @"kodisTrainTotal2_0.json"
            sprintf "%s%s" partialPathJson @"kodisTrainPomaliky2_0.json"
            sprintf "%s%s" partialPathJson @"kodisTrainRychliky2_0.json"
            sprintf "%s%s" partialPathJson @"kodisNAD2_0.json"
            sprintf "%s%s" partialPathJson @"kodisRegion6002_1.json"
        ]      