using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Disa.Framework
{
    [DisaFramework]
    public interface INewGcmMessage
    {
        Task ProcessMessage(JObject jsonObject);
    }
}
