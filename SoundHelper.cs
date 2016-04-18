using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpeedwayClientWpf.ViewModels;

namespace SpeedwayClientWpf
{
    /// <summary>
    /// Provides sound playing capabilities
    /// </summary>
    public static class SoundHelper
    {
        private static Timer _timer;
        private static IList<String> _playList;
        private static string ReadSoundFilePath;
        private static string FilteredReadSoundFilePath;
        static SoundHelper()
        {
            _timer = new Timer(TimerCallback, null, 1000, 200);
            _playList = new List<string>();
            ReadSoundFilePath = ConfigurationManager.AppSettings["ReadSoundFilePath"];
            FilteredReadSoundFilePath = ConfigurationManager.AppSettings["FilteredReadSoundFilePath"];
        }

        private static void TimerCallback(object state)
        {
            lock (_playList)
            {
                foreach (var file in _playList)
                {
                    Task.Factory.StartNew(() => Play2(file));
                }
                _playList.Clear();
            }
        }

        public static void PlayReadSound()
        {
            Task.Factory.StartNew(() =>
            {
                lock (_playList)
                    if (!_playList.Contains(ReadSoundFilePath))
                        _playList.Add(ReadSoundFilePath);
            });
        }

        public static void PlayFilteredReadSound()
        {
            Task.Factory.StartNew(() =>
            {
                lock (_playList)
                    if (!_playList.Contains(FilteredReadSoundFilePath))
                        _playList.Add(FilteredReadSoundFilePath);
            });
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

        [DllImport("winmm.dll")]
        private static extern Int32 mciSendString(string command, StringBuilder buffer, int bufferSize,
            IntPtr hwndCallback);

        private static void Play2(string filePath)
        {
            var track = Path.GetFileName(filePath);
            StringBuilder sb = new StringBuilder();
            mciSendString("open \"" + filePath + "\" alias " + track, sb, 0, IntPtr.Zero);
            mciSendString("play " + track, sb, 0, IntPtr.Zero);
            var isBeingPlayed = true;
            //loop
            sb = new StringBuilder();
            mciSendString("status " + track + " length", sb, 255, IntPtr.Zero);
            int length = Convert.ToInt32(sb.ToString());
            int pos = 0;
            while (isBeingPlayed)
            {
                sb = new StringBuilder();
                mciSendString("status " + track + " position", sb, 255, IntPtr.Zero);
                pos = Convert.ToInt32(sb.ToString());
                if (pos >= length)
                {
                    isBeingPlayed = false;
                    break;
                }
          //      Player.openPlayer( AppDomain.CurrentDomain.BaseDirectory + "local.wav");
          //      Thread.Sleep(500);
            }
            mciSendString("stop " + track, sb, 0, IntPtr.Zero);
            mciSendString("close " + track, sb, 0, IntPtr.Zero);
        }
    }
}
