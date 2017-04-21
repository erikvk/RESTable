using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTar
{
    public interface IValidatable
    {
        bool Validate(out string reason);
    }
}
