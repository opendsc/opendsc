// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;

using Microsoft.Win32.TaskScheduler;

namespace OpenDsc.Resource.Windows;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Group.Schema), TypeInfoPropertyName = "GroupSchema")]
[JsonSerializable(typeof(User.Schema), TypeInfoPropertyName = "UserSchema")]
[JsonSerializable(typeof(Service.Schema), TypeInfoPropertyName = "ServiceSchema")]
[JsonSerializable(typeof(Environment.Schema), TypeInfoPropertyName = "EnvironmentSchema")]
[JsonSerializable(typeof(Shortcut.Schema), TypeInfoPropertyName = "ShortcutSchema")]
[JsonSerializable(typeof(OptionalFeature.Schema), TypeInfoPropertyName = "OptionalFeatureSchema")]
[JsonSerializable(typeof(FileSystem.Acl.Schema), TypeInfoPropertyName = "FileSystemAclSchema")]
[JsonSerializable(typeof(ScheduledTask.Schema), TypeInfoPropertyName = "ScheduledTaskSchema")]
[JsonSerializable(typeof(DayOfWeek), TypeInfoPropertyName = "DayOfWeek")]
[JsonSerializable(typeof(ScheduledTask.TriggerType), TypeInfoPropertyName = "TriggerType")]
[JsonSerializable(typeof(TaskInstancesPolicy), TypeInfoPropertyName = "TaskInstancesPolicy")]
[JsonSerializable(typeof(TaskCompatibility), TypeInfoPropertyName = "TaskCompatibility")]
[JsonSerializable(typeof(TaskLogonType), TypeInfoPropertyName = "TaskLogonType")]
[JsonSerializable(typeof(ProcessPriorityClass), TypeInfoPropertyName = "ProcessPriorityClass")]
public partial class SourceGenerationContext : JsonSerializerContext
{
}
