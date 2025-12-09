using System.CommandLine;

using OpenDsc.Resource;
using OpenDsc.Resource.CommandLine;

using Temp;

#if (use-options)
var options = DscJsonSerializerSettings.Default;
var resource = new Resource(options);
var command = new CommandBuilder()
    .AddResource<Resource, Schema>(resource)
    .Build();
#else
var resource = new Resource(SourceGenerationContext.Default);
var command = new CommandBuilder()
    .AddResource<Resource, Schema>(resource)
    .Build();
#endif
return command.Parse(args).Invoke();
