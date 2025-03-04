using System.Runtime.CompilerServices;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace EventManager.Controls
{
    public class RemoveVisualEntry : Entry
    {
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            RemoveVisual();
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == nameof(BackgroundColor))
            {
                RemoveVisual();
            }
        }

        private void RemoveVisual()
        {
#if ANDROID
        if (Handler is IEntryHandler entryHandler) 
        {
            if(BackgroundColor == null)
            {
             entryHandler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Colors.Transparent.ToPlatform());    
            }
            else
            {
             entryHandler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(BackgroundColor.ToPlatform());    
            }
                        
        }
#endif
        }
    }
}
