using System.Threading.Tasks;
using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    public interface IMediaDownloader
    {
        Task<string> TranslatePath(string path);

        string GetUserAgent();
    }
}

