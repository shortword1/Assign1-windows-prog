using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using System.IO;
using TagLib;

namespace Assign1
{
    public partial class MainWindow : Window
    {
        private string currentFilePath;
        private DispatcherTimer progressTimer;

        public MainWindow()
        {
            InitializeComponent();
            progressTimer = new DispatcherTimer();
            progressTimer.Interval = TimeSpan.FromMilliseconds(200);
            progressTimer.Tick += ProgressTimer_Tick;
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
        }

        private void SaveTags_Click(object sender, RoutedEventArgs e)
        {
            // Stop the media player and progress timer.
            mediaPlayer.Stop();
            progressTimer.Stop();

            try
            {
                if (string.IsNullOrEmpty(currentFilePath))
                {
                    MessageBox.Show("No file is loaded.");
                    return;
                }

                // Create a temporary file with a .mp3 extension
                string tempFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".mp3");
                System.IO.File.Copy(currentFilePath, tempFilePath, true);

                // Update the tags on the temporary file
                using (var file = TagLib.File.Create(tempFilePath))
                {
                    file.Tag.Title = TitleTextBox.Text;
                    file.Tag.Performers = new[] { ArtistTextBox.Text };
                    file.Tag.Album = AlbumTextBox.Text;
                    file.Tag.Year = uint.Parse(YearTextBox.Text);
                    file.Save();
                }

                // Replace the original file with the updated temp file
                System.IO.File.Delete(currentFilePath);
                System.IO.File.Move(tempFilePath, currentFilePath);

                MessageBox.Show("Tags saved successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving tags: " + ex.Message);
            }
        }



        private void LoadMP3File(string filePath)
        {
            try
            {
                var file = TagLib.File.Create(filePath);
                TitleTextBox.Text = file.Tag.Title;
                ArtistTextBox.Text = string.Join(", ", file.Tag.Performers);
                AlbumTextBox.Text = file.Tag.Album;
                YearTextBox.Text = file.Tag.Year.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading file: " + ex.Message);
            }
        }

        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                progressBar.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
            }
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                progressBar.Value = mediaPlayer.Position.TotalSeconds;
            }
        }

        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();
            progressTimer.Stop();
            progressBar.Value = 0;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
