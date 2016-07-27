using Disa.Framework.Mobile;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(PhoneEntry), typeof(PhoneEntryRenderer))]
namespace Disa.Framework.Mobile
{
    public class PhoneEntryRenderer : EntryRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement == null)
            {
                var editText = Control;

                editText.InputType = Android.Text.InputTypes.ClassPhone;
            }
        }
    }
}

