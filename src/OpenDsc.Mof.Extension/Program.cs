// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.CommandLine;
using System.Text.Json;

using OpenDsc.Mof;
using OpenDsc.Schema;

var fileOption = new Option<string>("--file")
{
    Description = "Path to the DSC v1 compiled MOF file to import.",
    Required = true
};

var rootCommand = new RootCommand("DSC v3 import extension for DSC v1 MOF configuration files.")
{
    fileOption
};

rootCommand.SetAction(parseResult =>
{
    var filePath = parseResult.GetValue(fileOption)!;

    try
    {
        var document = MofConverter.Convert(filePath);
        Console.WriteLine(JsonSerializer.Serialize(document, SourceGenerationContext.Default.DscConfigDocument));
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
});

return rootCommand.Parse(args).Invoke();
