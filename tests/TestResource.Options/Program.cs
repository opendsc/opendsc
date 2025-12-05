// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Resource;
using OpenDsc.Resource.CommandLine;

using TestResource.Options;

var options = DscJsonSerializerSettings.Default;
var resource = new Resource(options);
var command = CommandBuilder<Resource, Schema>.Build(resource, options);
return command.Parse(args).Invoke();
