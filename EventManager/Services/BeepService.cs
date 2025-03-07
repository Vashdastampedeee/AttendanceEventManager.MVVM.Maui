using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Plugin.Maui.Audio;

namespace EventManager.Services
{
    public class BeepService
    {
        private IAudioPlayer player;
        private bool isInitialized = false;

        public async Task InitializeBeepSound()
        {

            if (!isInitialized)
            {
                Debug.WriteLine("[BeepService] Player is initialized");
                var stream = await FileSystem.OpenAppPackageFileAsync("barcodeSfx.mp3");
                player = AudioManager.Current.CreatePlayer(stream);
                isInitialized = true;
            }
            
        }

        public void PlayBeep()
        {
            if (player != null && isInitialized)
            {
                Debug.WriteLine("[BeepService] Beep sound is playing");
                player.Play();
            }
        }

    }
}
