namespace Disa.Framework.Mobile
{
    public interface IPluginPageControls
    {
        void Finish();

        void LaunchWebBrowser(string url);

        bool BackPressEnabled { get; set; }

        void LaunchPrivacyList();
    }
}

