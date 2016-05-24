using System;
using IRemoteProcedureCaller = SharpMTProto.IRemoteProcedureCaller;

namespace SharpTelegram.Schema
{
    public partial class TLSchemaAsyncMethods
    {
        partial void SetupRemoteProcedureCaller(IRemoteProcedureCaller remoteProcedureCaller)
        {
            remoteProcedureCaller.PrepareSerializersForAllTLObjectsInAssembly(
                typeof (ITLSchemaAsyncMethods).Assembly);
        }
    }
}

