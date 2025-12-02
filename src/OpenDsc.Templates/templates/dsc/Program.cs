using System.CommandLine;

using OpenDsc.Resource;
using OpenDsc.Resource.CommandLine;

using Temp;

#if (use-options)
var options = DscJsonSerializerSettings.Default;
var resource = new Resource(options);
var command = CommandBuilder<Resource, Schema>.Build(resource, options);
#else
var resource = new Resource(SourceGenerationContext.Default);
var command = CommandBuilder<Resource, Schema>.Build(resource, SourceGenerationContext.Default);
#endif
return command.Parse(args).Invoke();
