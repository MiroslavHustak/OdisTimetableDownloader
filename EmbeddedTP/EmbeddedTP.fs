namespace EmbeddedTP

open System
open FSharp.Data

module EmbeddedTP =
     
    let [<Literal>] ResolutionFolder = __SOURCE_DIRECTORY__ 

    type JsonProvider1 =
        JsonProvider<"KODISJson/kodisMHDTotal.json", EmbeddedResource = "EmbeddedTP, EmbeddedTP.KODISJson.kodisMHDTotal.json", ResolutionFolder = ResolutionFolder>

    type JsonProvider2 =
        JsonProvider<"KODISJson/kodisMHDTotal2_0.json", EmbeddedResource = "EmbeddedTP, EmbeddedTP.KODISJson.kodisMHDTotal2_0.json", ResolutionFolder = ResolutionFolder>
        
    let pathkodisMHDTotal = 
        try
            System.IO.Path.Combine (ResolutionFolder, @"KODISJson/kodisMHDTotal.json")
        with
        | _ -> String.Empty  //vypada to, ze se nic nestane, kdyz bude String.empty v JsonProvider1.Parse(tempJson1), v dokumentaci bohuzel o tom nic neni 

    let pathkodisMHDTotal2_0 = 
        try
            System.IO.Path.Combine(ResolutionFolder, @"KODISJson/kodisMHDTotal2_0.json")
        with
        | _ -> String.Empty
                   
    (*
    //pripominka, co je treba dat navic do fsproj nebo nastavit v properties (EmbeddedResource)
    <Project Sdk="Microsoft.NET.Sdk">
    
    	<PropertyGroup>
    		<TargetFramework>net8.0-windows7.0</TargetFramework>
    		<GenerateDocumentationFile>true</GenerateDocumentationFile>
    	</PropertyGroup>
    
    	<ItemGroup>
    		<EmbeddedResource Include="KODISJson\kodisMHDTotal.json">
    			<CopyToOutputDirectory>Always</CopyToOutputDirectory>
    		</EmbeddedResource>
    		<EmbeddedResource Include="KODISJson\kodisMHDTotal2_0.json">
    			<CopyToOutputDirectory>Always</CopyToOutputDirectory>
    		</EmbeddedResource>
    		<Compile Include="EmbeddedTP.fs" />
    	</ItemGroup>
    
    	<ItemGroup />
    
    	<ItemGroup>
    		<PackageReference Include="FSharp.Data" Version="6.4.0" />
    	</ItemGroup>
    
    </Project>    
    *)
