using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using System.IO;
using TagLib;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace Assign1
{
    public partial class MainWindow : Window
    {
        private string currentFilePath;
        private DispatcherTimer progressTimer;
        private ObservableCollection<string> recentSongs = new ObservableCollection<string>();

        public MainWindow()
        {
            InitializeComponent();
            progressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            progressTimer.Tick += ProgressTimer_Tick;
            recentSongsList.ItemsSource = recentSongs;
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "MP3 files (*.mp3)|*.mp3"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                currentFilePath = openFileDialog.FileName;
                mediaPlayer.Source = new Uri(currentFilePath);
                mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
                mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
                LoadMP3File(currentFilePath);
                AddToRecentSongs(currentFilePath);
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Play();
            progressTimer.Start();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Pause();
            progressTimer.Stop();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();
            progressTimer.Stop();
            progressBar.Value = 0;
            UpdateTimeDisplay();
        }

        private void SaveTags_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();
            progressTimer.Stop();

            try
            {
                if (string.IsNullOrEmpty(currentFilePath))
                {
                    MessageBox.Show("No file is loaded.");
                    return;
                }

                string tempFilePath = System.IO.Path.ChangeExtension(System.IO.Path.GetTempFileName(), ".mp3");
                System.IO.File.Copy(currentFilePath, tempFilePath, true);

                using (var mp3File = TagLib.File.Create(tempFilePath))
                {
                    mp3File.Tag.Title = TitleTextBox.Text;
                    mp3File.Tag.Performers = new[] { ArtistTextBox.Text };
                    mp3File.Tag.Album = AlbumTextBox.Text;
                    if (uint.TryParse(YearTextBox.Text, out var year))
                    {
                        mp3File.Tag.Year = year;
                    }
                    mp3File.Save();
                }

                System.IO.File.Delete(currentFilePath);
                System.IO.File.Move(tempFilePath, currentFilePath);

                MessageBox.Show("Tags saved successfully.");
                LoadMP3File(currentFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving tags: {ex.Message}");
            }
        }

        private void recentSongsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (recentSongsList.SelectedItem is string selectedFilePath)
            {
                // Assuming `selectedFilePath` is the full path to the MP3 file
                currentFilePath = selectedFilePath;
                mediaPlayer.Source = new Uri(currentFilePath);
                mediaPlayer.Play(); // Play the selected file
                LoadMP3File(currentFilePath); // Update UI based on selected file
            }
        }


        private void LoadMP3File(string filePath)
        {
            try
            {
                using (var file = TagLib.File.Create(filePath))
                {
                    TitleTextBox.Text = file.Tag.Title;
                    ArtistTextBox.Text = string.Join(", ", file.Tag.Performers);
                    AlbumTextBox.Text = file.Tag.Album;
                    YearTextBox.Text = file.Tag.Year.ToString();

                    if (file.Tag.Pictures.Length > 0)
                    {
                        LoadAlbumArt(filePath);
                    }
                    else
                    {
                        albumArtImage.Source = null; // Clear album art if no picture is available
                    }

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

                    albumArtImage.Source = image;
                }
            }
        }

        private void AddToRecentSongs(string filePath)
        {
            if (!recentSongs.Contains(filePath))
            {
                recentSongs.Add(filePath);
            }
        }

        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                progressBar.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                UpdateTimeDisplay();
            }
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                progressBar.Value = mediaPlayer.Position.TotalSeconds;
                UpdateTimeDisplay();
            }
        }

        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();
            progressTimer.Stop();
            progressBar.Value = 0;
            UpdateTimeDisplay();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void UpdateTimeDisplay()
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                var totalSeconds = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                var currentSeconds = mediaPlayer.Position.TotalSeconds;
                var timeLeft = totalSeconds - currentSeconds;
                songTimeDisplay.Text = $"{TimeSpan.FromSeconds(currentSeconds):m\\:ss}/{TimeSpan.FromSeconds(totalSeconds):m\\:ss}";
            }
            else
            {
                songTimeDisplay.Text = "0:00/0:00";
            }
        }
    }
}