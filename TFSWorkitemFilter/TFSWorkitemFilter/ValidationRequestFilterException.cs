using Microsoft.TeamFoundation.Framework.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFSWorkitemFilter
{
    class ValidationRequestFilterException : RequestFilterException
    {
        public ValidationRequestFilterException(string x, bool isWeb)
            : base(x, System.Net.HttpStatusCode.OK)
        {
        }

        public ValidationRequestFilterException(string x)
            : base(x)
        {
        }
    }
}
