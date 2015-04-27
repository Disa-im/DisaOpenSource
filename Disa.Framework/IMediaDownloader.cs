using System.Threading.Tasks;

namespace Disa.Framework
{
    public interface IMediaDownloader
    {
        Task<string> TranslatePath(string path);

        string GetUserAgent();
    }
}

