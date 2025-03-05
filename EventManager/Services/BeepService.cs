using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Plugin.Maui.Audio;

namespace EventManager.Services
{
    public class BeepService
    {
        private IAudioPlayer player;

        public async Task InitBeep()
        {
            if (player == null)
            {
                var stream = await FileSystem.OpenAppPackageFileAsync("barcodeSfx.mp3");
                player = AudioManager.Current.CreatePlayer(stream);
            }
        }

        public void PlayBeep()
        {
            player.Play();
        }

    }
}
