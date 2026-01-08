// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.Extensions.Options;

namespace OpenDsc.Lcm;

[OptionsValidator]
public partial class LcmConfigValidator : IValidateOptions<LcmConfig>
{
}
