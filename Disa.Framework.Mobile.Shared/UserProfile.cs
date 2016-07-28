using System;
using Xamarin.Forms;
using System.Threading.Tasks;

namespace Disa.Framework.Mobile
{
    public class UserProfile : View
    {
        public UserProfile()
        {
        }

        public EventHandler<string> StatusChanged;
        public EventHandler<object> ThumbnailRemoved;
        public EventHandler<byte[]> ThumbnailChanged;

        public static readonly BindableProperty ThumbnailProperty = 
            BindableProperty.Create<UserProfile,ImageSource> (
                p => p.Thumbnail, null);

        public void SetThumbnail(DisaThumbnail thumbnail)
        {
            Thumbnail = thumbnail == null ? null : thumbnail.Location;
        }

        public ImageSource Thumbnail 
        {
            get
            {
                return (ImageSource)GetValue(ThumbnailProperty);
            }
            set
            {
                SetValue(ThumbnailProperty, value);
            }
        }

        public static readonly BindableProperty TitleProperty = 
            BindableProperty.Create<UserProfile,string> (
                p => p.Title, null);

        public string Title
        {
            get
            {
                return (string)GetValue(TitleProperty);
            }
            set
            {
                SetValue(TitleProperty, value);
            }
        }

        public static readonly BindableProperty SubtitleProperty = 
            BindableProperty.Create<UserProfile,string> (
                p => p.Subtitle, null);

        public string Subtitle
        {
            get
            {
                return (string)GetValue(SubtitleProperty);
            }
            set
            {
                SetValue(SubtitleProperty, value);
            }
        }

        public static readonly BindableProperty StatusProperty = 
            BindableProperty.Create<UserProfile,string> (
                p => p.Status, null);

        public string Status
        {
            get
            {
                return (string)GetValue(StatusProperty);
            }
            set
            {
                SetValue(StatusProperty, value);
            }
        }

        public static readonly BindableProperty StatusIsVisibleProperty = 
            BindableProperty.Create<UserProfile,bool> (
                p => p.StatusIsVisible, false);

        public bool StatusIsVisible
        {
            get
            { 
                return (bool)GetValue(StatusIsVisibleProperty);
            }
            set
            { 
                SetValue(StatusIsVisibleProperty, value);
            }
        }

        public static readonly BindableProperty CanSetEmptyStatusProperty = BindableProperty.Create<UserProfile,bool> (
            p => p.CanSetEmptyStatus, true);

        public bool CanSetEmptyStatus
        {
            get
            { 
                return (bool)GetValue(CanSetEmptyStatusProperty);
            }
            set
            { 
                SetValue(CanSetEmptyStatusProperty, value);
            }
        }

        public static readonly BindableProperty CanEditThumbnailProperty = BindableProperty.Create<UserProfile,bool> (
            p => p.CanEditThumbnail, true);

        public bool CanEditThumbnail
        {
            get
            {
                return (bool)GetValue(CanEditThumbnailProperty);
            }
            set
            {
                SetValue(CanEditThumbnailProperty, value);
            }
        }

        public static readonly BindableProperty CanRemoveThumbnailProperty = BindableProperty.Create<UserProfile,bool> (
            p => p.CanRemoveThumbnail, true);

        public bool CanRemoveThumbnail
        {
            get
            {
                return (bool)GetValue(CanRemoveThumbnailProperty);
            }
            set
            {
                SetValue(CanRemoveThumbnailProperty, value);
            }
        }

        public static readonly BindableProperty CanViewThumbnailProperty = BindableProperty.Create<UserProfile,bool> (
            p => p.CanViewThumbnail, true);

        public bool CanViewThumbnail
        {
            get
            {
                return (bool)GetValue(CanViewThumbnailProperty);
            }
            set
            {
                SetValue(CanViewThumbnailProperty, value);
            }
        }

        public Func<Task<DisaThumbnail>> FetchThumbnail { get; set; } 
    }
}

