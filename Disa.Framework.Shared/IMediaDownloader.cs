using System.Threading.Tasks;
using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    [DisaFramework]
    public interface IMediaDownloader
    {
        Task<string> TranslatePath(string path);

        string GetUserAgent();
    }
}

