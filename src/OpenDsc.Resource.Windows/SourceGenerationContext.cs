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
[JsonSerializable(typeof(OpenDsc.Schema.DscScope), TypeInfoPropertyName = "DscScope")]
[JsonSerializable(typeof(Shortcut.Schema), TypeInfoPropertyName = "ShortcutSchema")]
[JsonSerializable(typeof(OptionalFeature.Schema), TypeInfoPropertyName = "OptionalFeatureSchema")]
[JsonSerializable(typeof(FileSystem.Acl.Schema), TypeInfoPropertyName = "FileSystemAclSchema")]
[JsonSerializable(typeof(ScheduledTask.Schema), TypeInfoPropertyName = "ScheduledTaskSchema")]
[JsonSerializable(typeof(ScheduledTask.Trigger), TypeInfoPropertyName = "ScheduledTaskTrigger")]
[JsonSerializable(typeof(ScheduledTask.Trigger[]), TypeInfoPropertyName = "ScheduledTaskTriggerArray")]
[JsonSerializable(typeof(ScheduledTask.Action), TypeInfoPropertyName = "ScheduledTaskAction")]
[JsonSerializable(typeof(ScheduledTask.Action[]), TypeInfoPropertyName = "ScheduledTaskActionArray")]
[JsonSerializable(typeof(ScheduledTask.TimeTriggerConfig), TypeInfoPropertyName = "TimeTriggerConfig")]
[JsonSerializable(typeof(ScheduledTask.DailyTriggerConfig), TypeInfoPropertyName = "DailyTriggerConfig")]
[JsonSerializable(typeof(ScheduledTask.WeeklyTriggerConfig), TypeInfoPropertyName = "WeeklyTriggerConfig")]
[JsonSerializable(typeof(ScheduledTask.BootTriggerConfig), TypeInfoPropertyName = "BootTriggerConfig")]
[JsonSerializable(typeof(ScheduledTask.LogonTriggerConfig), TypeInfoPropertyName = "LogonTriggerConfig")]
[JsonSerializable(typeof(DayOfWeek), TypeInfoPropertyName = "DayOfWeek")]
[JsonSerializable(typeof(TaskInstancesPolicy), TypeInfoPropertyName = "TaskInstancesPolicy")]
[JsonSerializable(typeof(TaskCompatibility), TypeInfoPropertyName = "TaskCompatibility")]
[JsonSerializable(typeof(TaskLogonType), TypeInfoPropertyName = "TaskLogonType")]
[JsonSerializable(typeof(ProcessPriorityClass), TypeInfoPropertyName = "ProcessPriorityClass")]
[JsonSerializable(typeof(UserRight.Schema), TypeInfoPropertyName = "UserRightSchema")]
[JsonSerializable(typeof(PasswordPolicy.Schema), TypeInfoPropertyName = "PasswordPolicySchema")]
public partial class SourceGenerationContext : JsonSerializerContext
{
}
