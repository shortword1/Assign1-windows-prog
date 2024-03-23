using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using System.IO;
using TagLib;
using System.Collections.ObjectModel;
using System.Windows.Controls;

// The main namespace for our application
namespace Assign1
{
    // The main window class for our application
    public partial class MainWindow : Window
    {
        // The path of the currently playing song
        private string currentFilePath;
        // A timer to update the progress bar
        private DispatcherTimer progressTimer;
        // A collection to store the recently played songs
        private ObservableCollection<string> recentSongs = new ObservableCollection<string>();

        // The constructor for the MainWindow class
        public MainWindow()
        {
            // Initialize the UI components
            InitializeComponent();
            // Initialize the timer with an interval of 200 milliseconds
            progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            // Attach an event handler to the Tick event of the timer
            progressTimer.Tick += ProgressTimer_Tick;
            // Bind the recentSongs collection to the ItemsSource property of the recentSongsList
            recentSongsList.ItemsSource = recentSongs;
            // Attach event handlers to the MediaOpened and MediaEnded events of the mediaPlayer
            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        }

        // Event handler for the OpenFile button click event
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            // Create an OpenFileDialog to select the MP3 file
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "MP3 files (*.mp3)|*.mp3" };
            // If a file is selected
            if (openFileDialog.ShowDialog() == true)
            {
                // Store the file path
                currentFilePath = openFileDialog.FileName;
                // Set the source of the mediaPlayer to the selected file
                mediaPlayer.Source = new Uri(currentFilePath);
                // Load the MP3 file
                LoadMP3File(currentFilePath);
                // Add the file to the recent songs list
                AddToRecentSongs(currentFilePath);
            }
        }

        // Event handler for the Play button click event
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            // Start playing the media
            mediaPlayer.Play();
            // Start the progress timer
            progressTimer.Start();
        }

        // Event handler for the Pause button click event
        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            // Pause the media
            mediaPlayer.Pause();
            // Stop the progress timer
            progressTimer.Stop();
        }

        // Event handler for the Stop button click event
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            // Stop the media
            mediaPlayer.Stop();
            // Stop the progress timer
            progressTimer.Stop();
            // Reset the progress bar value
            progressBar.Value = 0;
            // Update the time display
            UpdateTimeDisplay();
        }

        // Event handler for the SaveTags button click event
        private void SaveTags_Click(object sender, RoutedEventArgs e)
        {
            // If no file is selected, return
            if (string.IsNullOrEmpty(currentFilePath)) return;

            try
            {
                // Create a temporary file to work with
                string tempFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".mp3");
                // Copy the current file to the temporary file
                System.IO.File.Copy(currentFilePath, tempFilePath, true);

                // Use TagLib to edit the tags of the MP3 file
                using (var mp3File = TagLib.File.Create(tempFilePath))
                {
                    // Set the title, performers, album, and year of the song
                    mp3File.Tag.Title = TitleTextBox.Text;
                    mp3File.Tag.Performers = new[] { ArtistTextBox.Text };
                    mp3File.Tag.Album = AlbumTextBox.Text;
                    if (uint.TryParse(YearTextBox.Text, out var year)) mp3File.Tag.Year = year;
                    // Save the changes
                    mp3File.Save();
                }

                // Delete the original file
                System.IO.File.Delete(currentFilePath);
                // Move the temporary file to the original file's location
                System.IO.File.Move(tempFilePath, currentFilePath);
                // Reload the MP3 file
                LoadMP3File(currentFilePath);
            }
            catch (Exception ex)
            {
                // Show an error message if something goes wrong
                MessageBox.Show($"Error saving tags: {ex.Message}");
            }
        }

        // Event handler for the recentSongsList SelectionChanged event
        private void recentSongsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If a song is selected from the list
            if (recentSongsList.SelectedItem is string selectedFilePath)
            {
                // Store the file path
                currentFilePath = selectedFilePath;
                // Set the source of the mediaPlayer to the selected file
                mediaPlayer.Source = new Uri(currentFilePath);
                // Start playing the media
                mediaPlayer.Play();
                // Load the MP3 file
                LoadMP3File(currentFilePath);
            }
        }

        // Method to load an MP3 file
        private void LoadMP3File(string filePath)
        {
            try
            {
                // Use TagLib to read the tags of the MP3 file
                using (var file = TagLib.File.Create(filePath))
                {
                    // Set the text boxes to the song's metadata
                    TitleTextBox.Text = file.Tag.Title;
                    ArtistTextBox.Text = string.Join(", ", file.Tag.Performers);
                    AlbumTextBox.Text = file.Tag.Album;
                    YearTextBox.Text = file.Tag.Year.ToString();
                    // Load the album art
                    LoadAlbumArt(file);
                }
            }
            catch (Exception ex)
            {
                // Show an error message if something goes wrong
                MessageBox.Show($"Error loading file: {ex.Message}");
            }
        }

        // Method to add a song to the recent songs list
        private void AddToRecentSongs(string filePath)
        {
            // If the song is not already in the list
            if (!recentSongs.Contains(filePath))
            {
                // Add the song to the list
                recentSongs.Add(filePath);
            }
        }

        // Method to load the album art of a song
        private void LoadAlbumArt(TagLib.File file)
        {
            // If the song has album art
            if (file.Tag.Pictures.Length > 0)
            {
                // Load the album art
                var bin = (byte[])(file.Tag.Pictures[0].Data.Data);
                using (MemoryStream ms = new MemoryStream(bin))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    // Set the source of the albumArtImage to the loaded image
                    albumArtImage.Source = image;
                }
            }
            else
            {
                // If the song does not have album art, set the source of the albumArtImage to null
                albumArtImage.Source = null;
            }
        }

        // Event handler for the mediaPlayer MediaOpened event
        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            // Set the maximum value of the progress bar to the duration of the song
            progressBar.Maximum = mediaPlayer.NaturalDuration.HasTimeSpan ? mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds : 0;
            // Update the time display
            UpdateTimeDisplay();
        }

        // Event handler for the progressTimer Tick event
        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            // Update the value of the progress bar to the current position of the song
            progressBar.Value = mediaPlayer.Position.TotalSeconds;
            // Update the time display
            UpdateTimeDisplay();
        }

        // Event handler for the mediaPlayer MediaEnded event
        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Stop the media and the progress timer
            mediaPlayer.Stop();
            progressTimer.Stop();
            // Reset the progress bar value
            progressBar.Value = 0;
            // Update the time display
            UpdateTimeDisplay();
        }

        // Event handler for the Exit button click event
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // Shut down the application
            Application.Current.Shutdown();
        }

        // Method to update the time display
        private void UpdateTimeDisplay()
        {
            // Calculate the total and current time of the song
            var totalSeconds = mediaPlayer.NaturalDuration.HasTimeSpan ? mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds : 0;
            var currentSeconds = mediaPlayer.Position.TotalSeconds;
            // Update the songTimeDisplay text
            songTimeDisplay.Text = $"{TimeSpan.FromSeconds(currentSeconds):m\\:ss}/{TimeSpan.FromSeconds(totalSeconds):m\\:ss}";
        }
    }
}
