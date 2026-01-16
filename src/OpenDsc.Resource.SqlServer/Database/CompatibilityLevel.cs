// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Json.Schema.Generation;

namespace OpenDsc.Resource.SqlServer.Database;

[Description("Specifies the database compatibility level.")]
public enum CompatibilityLevel
{
    [Description("SQL Server 2008 (100)")]
    Version100 = 100,

    [Description("SQL Server 2008 R2 (100)")]
    Version100R2 = 100,

    [Description("SQL Server 2012 (110)")]
    Version110 = 110,

    [Description("SQL Server 2014 (120)")]
    Version120 = 120,

    [Description("SQL Server 2016 (130)")]
    Version130 = 130,

    [Description("SQL Server 2017 (140)")]
    Version140 = 140,

    [Description("SQL Server 2019 (150)")]
    Version150 = 150,

    [Description("SQL Server 2022 (160)")]
    Version160 = 160
}
