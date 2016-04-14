using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using SpeedwayClientWpf.ViewModels;

namespace SpeedwayClientWpf
{
    /// <summary>
    /// Provides sound playing capabilities
    /// </summary>
    public static class SoundHelper
    {
        public static void PlayReadSound()
        {
            var path = ConfigurationManager.AppSettings["ReadSoundFilePath"];
            Play(path, SystemSounds.Beep);
        }

        public static void PlayFilteredReadSound()
        {
            var path = ConfigurationManager.AppSettings["FilteredReadSoundFilePath"];
            Play(path, SystemSounds.Exclamation);
        }

        private static void Play(string filePath, SystemSound alternativeSound)
        {
            // if .wav file cannot be played, the alternative system sound will be played
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try
                {
                    var player = new SoundPlayer(filePath);
                    player.Play();
                }
                catch (Exception exception)
                {
                    var error = string.Format("Cannot play file {0}. {1}", filePath, exception.Message);
                    MainWindowViewModel.Instance.PushMessage(new LogMessage(LogMessageType.Error, error));
                    Trace.TraceError(error + exception.StackTrace);

                    alternativeSound.Play();
                }
            }
            else
            {
                alternativeSound.Play();
            }
        }
    }
}
