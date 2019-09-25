using System.Collections.Generic;

namespace ZeroLevel.Services.AsService
{
    public interface Configurator
    {
        IEnumerable<ValidateResult> Validate();
    }
}
