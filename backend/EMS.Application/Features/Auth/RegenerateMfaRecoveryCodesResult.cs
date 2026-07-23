using System.Collections.Generic;

namespace EMS.Application.Features.Auth
{
    public class RegenerateMfaRecoveryCodesResult
    {
        public List<string> RecoveryCodes { get; set; } = new();
    }
}
