using System.Collections.Generic;

namespace EMS.Application.Features.Auth
{
    public class EnableMfaResult
    {
        // Shown to the caller exactly once — only a hash of each code is persisted.
        public List<string> RecoveryCodes { get; set; } = new();
    }
}
