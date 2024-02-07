using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using System.IO;
using TagLib;
using System.Collections.ObjectModel;
using System.Windows.Controls;

// Define the namespace for the assignment
namespace Assign1
{
    // Define the main window class which inherits from Window
    public partial class MainWindow : Window
    {
        // Fields to store the current file path, a timer for updating the progress, and a collection of recent songs
        private string currentFilePath;
        private DispatcherTimer progressTimer;
        private ObservableCollection<string> recentSongs = new ObservableCollection<string>();

        // Constructor for the MainWindow class
        public MainWindow()
        {
            // Initialize the component (auto-generated code to setup UI components)
            InitializeComponent();
            // Setup the progress timer with a 200ms interval and attach the Tick event
            progressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            progressTimer.Tick += ProgressTimer_Tick;
            // Bind the recentSongs collection to the recentSongsList ListView for displaying recent songs
            recentSongsList.ItemsSource = recentSongs;
        }

        // Event handler for clicking the Open File button
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            // Create and configure an OpenFileDialog for MP3 files
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "MP3 files (*.mp3)|*.mp3"
            };

            // Show the dialog and proceed if a file is selected
            if (openFileDialog.ShowDialog() == true)
            {
                // Store the selected file path and set it as the media player's source
                currentFilePath = openFileDialog.FileName;
                mediaPlayer.Source = new Uri(currentFilePath);
                // Subscribe to MediaOpened and MediaEnded events of the media player
                // to update the UI and recent songs list when the media is opened and ends
                mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
                mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
                // Load the MP3 file tags to the UI and add the file to the recent songs list
                // if it's not already in the list
                LoadMP3File(currentFilePath);
                AddToRecentSongs(currentFilePath);
            }
        }

        // Event handler for clicking the Play button
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Play(); // Play the media
            progressTimer.Start(); // Start the progress timer to update the UI
        }

        // Event handler for clicking the Pause button
        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Pause(); // Pause the media
            progressTimer.Stop(); // Stop the progress timer
        }

        // Event handler for clicking the Stop button
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop(); // Stop the media
            progressTimer.Stop(); // Stop the progress timer
            progressBar.Value = 0; // Reset the progress bar
            UpdateTimeDisplay(); // Update the time display
        }

        // Event handler for clicking the Save Tags button
        private void SaveTags_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop(); // Stop the media
            progressTimer.Stop(); // Stop the progress timer

            try
            {
                // Check if a file is loaded
                if (string.IsNullOrEmpty(currentFilePath))
                {
                    MessageBox.Show("No file is loaded.");
                    return;
                }

                // Create a temporary copy of the MP3 file to edit tags
                string tempFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".mp3");
                System.IO.File.Copy(currentFilePath, tempFilePath, true);

                // Open the temporary file with TagLib, update its tags, and save
                using (var mp3File = TagLib.File.Create(tempFilePath))
                {
                    mp3File.Tag.Title = TitleTextBox.Text;
                    mp3File.Tag.Performers = new[] { ArtistTextBox.Text };
                    mp3File.Tag.Album = AlbumTextBox.Text;
                    // Try to parse the year and set it if successful
                    if (uint.TryParse(YearTextBox.Text, out var year))
                    {
                        mp3File.Tag.Year = year;
                    }
                    mp3File.Save();
                }

                // Replace the original file with the updated copy
                System.IO.File.Delete(currentFilePath);
                System.IO.File.Move(tempFilePath, currentFilePath);

                MessageBox.Show("Tags saved successfully.");
                // Reload the MP3 file to update the UI with the new tags
                LoadMP3File(currentFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving tags: {ex.Message}");
            }
        }

        // Event handler for changing selection in the recent songs list
        private void recentSongsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (recentSongsList.SelectedItem is string selectedFilePath)
            {
                // Play the selected recent song
                currentFilePath = selectedFilePath;
                mediaPlayer.Source = new Uri(currentFilePath);
                mediaPlayer.Play(); // Immediately play the selected file
                LoadMP3File(currentFilePath); // Update the UI based on the selected file
            }
        }

        // Loads the MP3 file's tags into the UI
        private void LoadMP3File(string filePath)
        {
            try
            {
                using (var file = TagLib.File.Create(filePath))
                {
                    TitleTextBox.Text = file.Tag.Title; // Set the title in the UI
                    ArtistTextBox.Text = string.Join(", ", file.Tag.Performers); // Set the artist(s)
                    AlbumTextBox.Text = file.Tag.Album; // Set the album name
                    YearTextBox.Text = file.Tag.Year.ToString(); // Set the year

                    // Load album art if available
                    if (file.Tag.Pictures.Length > 0)
                    {
                        LoadAlbumArt(filePath);
                    }
                    else
                    {
                        albumArtImage.Source = null; // Clear album art if no picture is available
                    }

                    // Update the NowPlayingControl, if it exists, with the current song's info
                    if (this.FindName("nowPlayingControl") is NowPlayingControl npc)
                    {
                        npc.Title = file.Tag.Title;
                        npc.Artist = string.Join(", ", file.Tag.Performers);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading file: {ex.Message}");
            }
        }

        // Loads the album art into the UI
        private void LoadAlbumArt(string filePath)
        {
            var file = TagLib.File.Create(filePath);
            if (file.Tag.Pictures.Length > 0)
            {
                var bin = (byte[])(file.Tag.Pictures[0].Data.Data);
                using (MemoryStream ms = new MemoryStream(bin))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();

                    albumArtImage.Source = image; // Set the album art in the UI
                }
            }
        }

        // Adds a file path to the recent songs collection, avoiding duplicates
        private void AddToRecentSongs(string filePath)
        {
            if (!recentSongs.Contains(filePath))
            {
                recentSongs.Add(filePath);
            }
        }

        // Updates the progress bar and time display when the media is opened
        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                progressBar.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                UpdateTimeDisplay(); // Initial update for the time display
            }
        }

        // Updates the progress bar and time display periodically
        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                progressBar.Value = mediaPlayer.Position.TotalSeconds;
                UpdateTimeDisplay(); // Update the time display with the current position
            }
        }

        // Resets the progress bar and stops the timer when the media playback ends
        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();
            progressTimer.Stop();
            progressBar.Value = 0;
            UpdateTimeDisplay(); // Update the time display to reflect the stopped state
        }

        // Exits the application
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Updates the time display UI with the current playback position and total duration
        private void UpdateTimeDisplay()
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                var totalSeconds = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                var currentSeconds = mediaPlayer.Position.TotalSeconds;
                songTimeDisplay.Text = $"{TimeSpan.FromSeconds(currentSeconds):m\\:ss}/{TimeSpan.FromSeconds(totalSeconds):m\\:ss}";
            }
            else
            {
                songTimeDisplay.Text = "0:00/0:00"; // Default display if duration is not available
            }
        }
    }
}
