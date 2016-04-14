using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using SpeedwayClientWpf.ViewModels;

namespace SpeedwayClientWpf
{
    /// <summary>
    ///     Operations with application settings
    /// </summary>
    public class Settings : ViewModelBase
    {
        private bool _addDateToOutput;
        private bool _addReaderInfoToOutput;
        private string _folderPath;
        private bool _playSoundForFilteredRead;
        private bool _playSoundForRead;
        private int _rereadTime;
        private string _tagFilter;

        public Settings()
        {
            SelectFolderPathCommand = new DelegateCommand(SelectFolderPath);
        }

        /// <summary>
        ///     Folder to save the output files
        /// </summary>
        public string FolderPath
        {
            get { return _folderPath; }
            set
            {
                if (Directory.Exists(value))
                {
                    _folderPath = value;
                    OnPropertyChanged("FolderPath");
                }
            }
        }

        /// <summary>
        ///     Some string to filter tags
        /// </summary>
        public string TagFilter
        {
            get { return _tagFilter; }
            set
            {
                _tagFilter = value;
                OnPropertyChanged("TagFilter");
            }
        }

        /// <summary>
        ///     Time to check if tag was seen before, seconds
        /// </summary>
        public int RereadTime
        {
            get { return _rereadTime; }
            set
            {
                _rereadTime = value;
                OnPropertyChanged("RereadTime");
            }
        }

        /// <summary>
        ///     Add date to the file output
        /// </summary>
        public bool AddDateToOutput
        {
            get { return _addDateToOutput; }
            set
            {
                _addDateToOutput = value;
                OnPropertyChanged("AddDateToOutput");
            }
        }

        /// <summary>
        ///     Add reader id & antenna id to the file output
        /// </summary>
        public bool AddReaderInfoToOutput
        {
            get { return _addReaderInfoToOutput; }
            set
            {
                _addReaderInfoToOutput = value;
                OnPropertyChanged("AddReaderInfoToOutput");
            }
        }

        /// <summary>
        ///     Play sound for each read
        /// </summary>
        public bool PlaySoundForRead
        {
            get { return _playSoundForRead; }
            set
            {
                _playSoundForRead = value;
                OnPropertyChanged("PlaySoundForRead");
            }
        }

        /// <summary>
        ///     Play sound just for filtered read
        /// </summary>
        public bool PlaySoundForFilteredRead
        {
            get { return _playSoundForFilteredRead; }
            set
            {
                _playSoundForFilteredRead = value;
                OnPropertyChanged("PlaySoundForFilteredRead");
            }
        }

        public ICommand SelectFolderPathCommand { get; set; }

        /// <summary>
        ///     Shows a dialog to choose a folder to save the files
        /// </summary>
        private void SelectFolderPath()
        {
            var dialog = new FolderBrowserDialog
            {
                Description = "Please select directory to save the files.",
                SelectedPath = FolderPath,
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == DialogResult.OK)
                FolderPath = dialog.SelectedPath;
        }

        /// <summary>
        ///     Saves settings to the app.config file
        /// </summary>
        public void SaveSettings()
        {
            ConfigHelper.Set("TagFilter", TagFilter);
            ConfigHelper.Set("RereadTime", RereadTime.ToString());
            ConfigHelper.Set("FolderPath", FolderPath);
            ConfigHelper.Set("AddDateToOutput", AddDateToOutput.ToString());
            ConfigHelper.Set("AddReaderInfoToOutput", AddReaderInfoToOutput.ToString());
            ConfigHelper.Set("PlaySoundForRead", PlaySoundForRead.ToString());
            ConfigHelper.Set("PlaySoundForFilteredRead", PlaySoundForFilteredRead.ToString());
        }

        /// <summary>
        ///     Loads settings from the app.config file
        /// </summary>
        public void LoadSettings()
        {
            TagFilter = ConfigHelper.Get("TagFilter");
            FolderPath = ConfigHelper.Get("FolderPath");
            AddDateToOutput = bool.Parse(ConfigHelper.Get("AddDateToOutput"));
            AddReaderInfoToOutput = bool.Parse(ConfigHelper.Get("AddReaderInfoToOutput"));
            PlaySoundForRead = bool.Parse(ConfigHelper.Get("PlaySoundForRead"));
            PlaySoundForFilteredRead = bool.Parse(ConfigHelper.Get("PlaySoundForFilteredRead"));

            int rereadtime;
            if (int.TryParse(ConfigHelper.Get("RereadTime"), out rereadtime))
                RereadTime = rereadtime;
        }
    }
}