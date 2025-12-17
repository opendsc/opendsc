// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Resource.CommandLine;

using TestResource.Multi;

var fileResource = new FileResource(SourceGenerationContext.Default);
var userResource = new UserResource(SourceGenerationContext.Default);
var serviceResource = new ServiceResource(SourceGenerationContext.Default);

var command = new CommandBuilder()
    .AddResource<FileResource, FileSchema>(fileResource)
    .AddResource<UserResource, UserSchema>(userResource)
    .AddResource<ServiceResource, ServiceSchema>(serviceResource)
    .Build();

return command.Parse(args).Invoke();
