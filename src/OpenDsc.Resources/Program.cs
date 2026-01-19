// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Resource.CommandLine;

#if WINDOWS
using GroupNs = OpenDsc.Resource.Windows.Group;
using UserNs = OpenDsc.Resource.Windows.User;
using ServiceNs = OpenDsc.Resource.Windows.Service;
using EnvironmentNs = OpenDsc.Resource.Windows.Environment;
using ShortcutNs = OpenDsc.Resource.Windows.Shortcut;
using OptionalFeatureNs = OpenDsc.Resource.Windows.OptionalFeature;
using FileSystemAclNs = OpenDsc.Resource.Windows.FileSystem.Acl;
using ScheduledTaskNs = OpenDsc.Resource.Windows.ScheduledTask;
#endif

using SqlServerLoginNs = OpenDsc.Resource.SqlServer.Login;
using SqlServerDatabaseNs = OpenDsc.Resource.SqlServer.Database;
using SqlServerDatabasePermissionNs = OpenDsc.Resource.SqlServer.DatabasePermission;

#if !WINDOWS
using PosixPermissionNs = OpenDsc.Resource.Posix.FileSystem.Permission;
#endif

using FileNs = OpenDsc.Resource.FileSystem.File;
using DirectoryNs = OpenDsc.Resource.FileSystem.Directory;
using SymbolicLinkNs = OpenDsc.Resource.FileSystem.SymbolicLink;
using XmlElementNs = OpenDsc.Resource.Xml.Element;
using ZipCompressNs = OpenDsc.Resource.Archive.Zip.Compress;
using ZipExpandNs = OpenDsc.Resource.Archive.Zip.Expand;

#if WINDOWS
var groupResource = new GroupNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
var userResource = new UserNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
var serviceResource = new ServiceNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
var environmentResource = new EnvironmentNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
var shortcutResource = new ShortcutNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
var optionalFeatureResource = new OptionalFeatureNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
var fileSystemAclResource = new FileSystemAclNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
var scheduledTaskResource = new ScheduledTaskNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
#endif

var sqlServerLoginResource = new SqlServerLoginNs.Resource(OpenDsc.Resource.SqlServer.SourceGenerationContext.Default);
var sqlServerDatabaseResource = new SqlServerDatabaseNs.Resource(OpenDsc.Resource.SqlServer.SourceGenerationContext.Default);
var sqlServerDatabasePermissionResource = new SqlServerDatabasePermissionNs.Resource(OpenDsc.Resource.SqlServer.SourceGenerationContext.Default);

#if !WINDOWS
#pragma warning disable CA1416 // 'Resource' is only supported on: 'linux', 'macOS'
PosixPermissionNs.Resource? posixPermissionResource = null;
if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
{
    posixPermissionResource = new PosixPermissionNs.Resource(OpenDsc.Resource.Posix.SourceGenerationContext.Default);
}
#pragma warning restore CA1416
#endif

var fileResource = new FileNs.Resource(OpenDsc.Resource.FileSystem.SourceGenerationContext.Default);
var directoryResource = new DirectoryNs.Resource(OpenDsc.Resource.FileSystem.SourceGenerationContext.Default);
var symbolicLinkResource = new SymbolicLinkNs.Resource(OpenDsc.Resource.FileSystem.SourceGenerationContext.Default);
var xmlElementResource = new XmlElementNs.Resource(OpenDsc.Resource.Xml.SourceGenerationContext.Default);
var zipCompressResource = new ZipCompressNs.Resource(OpenDsc.Resource.Archive.SourceGenerationContext.Default);
var zipExpandResource = new ZipExpandNs.Resource(OpenDsc.Resource.Archive.SourceGenerationContext.Default);

var command = new CommandBuilder();

#if WINDOWS
command
    .AddResource<GroupNs.Resource, GroupNs.Schema>(groupResource)
    .AddResource<UserNs.Resource, UserNs.Schema>(userResource)
    .AddResource<ServiceNs.Resource, ServiceNs.Schema>(serviceResource)
    .AddResource<EnvironmentNs.Resource, EnvironmentNs.Schema>(environmentResource)
    .AddResource<ShortcutNs.Resource, ShortcutNs.Schema>(shortcutResource)
    .AddResource<OptionalFeatureNs.Resource, OptionalFeatureNs.Schema>(optionalFeatureResource)
    .AddResource<FileSystemAclNs.Resource, FileSystemAclNs.Schema>(fileSystemAclResource)
    .AddResource<ScheduledTaskNs.Resource, ScheduledTaskNs.Schema>(scheduledTaskResource);
#endif

command.AddResource<SqlServerLoginNs.Resource, SqlServerLoginNs.Schema>(sqlServerLoginResource);
command.AddResource<SqlServerDatabaseNs.Resource, SqlServerDatabaseNs.Schema>(sqlServerDatabaseResource);
command.AddResource<SqlServerDatabasePermissionNs.Resource, SqlServerDatabasePermissionNs.Schema>(sqlServerDatabasePermissionResource);

#if !WINDOWS
#pragma warning disable CA1416 // 'Resource' is only supported on: 'linux', 'macOS'
if (posixPermissionResource is not null)
{
    command.AddResource<PosixPermissionNs.Resource, PosixPermissionNs.Schema>(posixPermissionResource);
}
#pragma warning restore CA1416
#endif

command
    .AddResource<FileNs.Resource, FileNs.Schema>(fileResource)
    .AddResource<DirectoryNs.Resource, DirectoryNs.Schema>(directoryResource)
    .AddResource<SymbolicLinkNs.Resource, SymbolicLinkNs.Schema>(symbolicLinkResource)
    .AddResource<XmlElementNs.Resource, XmlElementNs.Schema>(xmlElementResource)
    .AddResource<ZipCompressNs.Resource, ZipCompressNs.Schema>(zipCompressResource)
    .AddResource<ZipExpandNs.Resource, ZipExpandNs.Schema>(zipExpandResource);

return command.Build().Parse(args).Invoke();
