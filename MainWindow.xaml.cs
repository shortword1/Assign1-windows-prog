// Pulling in the essentials: UI, media handling, file dialogs, and not forgetting the mighty TagLib for tag editing.
using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using System.IO;
using TagLib;
using System.Collections.ObjectModel;
using System.Windows.Controls;

// Diving into our little music player world within the Assign1 namespace.
namespace Assign1
{
    public partial class MainWindow : Window
    {
        // Keeping track of the song currently playing because we need to know what's on.
        private string currentFilePath;
        // A nifty timer to keep our progress bar moving. It's all about the visual feedback!
        private DispatcherTimer progressTimer;
        // Remembering what you listened to, just like a good friend who knows your music taste.
        private ObservableCollection<string> recentSongs = new ObservableCollection<string>();

        public MainWindow()
        {
            InitializeComponent(); // Setting up the stage for our UI elements.
            // Our timer's like a metronome, ticking away to update the song's progress. Every 200 milliseconds feels just right.
            progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            progressTimer.Tick += ProgressTimer_Tick; // Each tick is a step forward in our song.

            // Hooking up our list of jams to the UI. Let's see what's been on repeat!
            recentSongsList.ItemsSource = recentSongs;

            // These media player events are the behind-the-scenes crew, making sure everything runs smoothly.
            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        }

        // That moment when you find a new song and just have to play it. This button's for that.
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "MP3 files (*.mp3)|*.mp3" };
            if (openFileDialog.ShowDialog() == true)
            {
                currentFilePath = openFileDialog.FileName;
                mediaPlayer.Source = new Uri(currentFilePath);
                LoadMP3File(currentFilePath); // Let's dig into those MP3 tags, shall we?
                AddToRecentSongs(currentFilePath); // And don't forget, this one's going on the recent list.
            }
        }

        // Hit play and let the music take over. Also, don't let that progress bar stand still!
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Play();
            progressTimer.Start();
        }

        // Sometimes you just need a pause, catch your breath, or sip your coffee. Music waits for you.
        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Pause();
            progressTimer.Stop();
        }

        // All things come to an end, songs included. This stops the music and resets our visual cues.
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();
            progressTimer.Stop();
            progressBar.Value = 0;
            UpdateTimeDisplay(); // Let's also make sure the time display knows we've stopped.
        }

        // Ever wanted to play the role of a music editor? This button lets you save your tag edits back into the song file.
        private void SaveTags_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath)) return; // No file, no work to do.

            try
            {
                // Creating a temporary file because it's safer to work with. Just like having a rehearsal before the live show.
                string tempFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".mp3");
                System.IO.File.Copy(currentFilePath, tempFilePath, true);

                // TagLib makes editing tags feel like a breeze. Changing the song's title, artist, and more right here.
                using (var mp3File = TagLib.File.Create(tempFilePath))
                {
                    mp3File.Tag.Title = TitleTextBox.Text;
                    mp3File.Tag.Performers = new[] { ArtistTextBox.Text };
                    mp3File.Tag.Album = AlbumTextBox.Text;
                    if (uint.TryParse(YearTextBox.Text, out var year)) mp3File.Tag.Year = year;
                    mp3File.Save();
                }

                // Now, let's make the changes real by swapping the old file with our newly edited one.
                System.IO.File.Delete(currentFilePath);
                System.IO.File.Move(tempFilePath, currentFilePath);
                LoadMP3File(currentFilePath); // A quick refresh to show off our editing skills.
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Whoops! Ran into a snag saving tags: {ex.Message}");
            }
        }

        // Picking a song from your recent list? It's like saying, "Hey, long time no see! Let's hang out."
        private void recentSongsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (recentSongsList.SelectedItem is string selectedFilePath)
            {
                currentFilePath = selectedFilePath;
                mediaPlayer.Source = new Uri(currentFilePath);
                mediaPlayer.Play();
                LoadMP3File(currentFilePath); // Freshen up those song details for another round.
            }
        }

        // Loading a song is more than just playing it. It's about getting to know it. Here's where we peek at the tags.
        private void LoadMP3File(string filePath)
        {
            try
            {
                using (var file = TagLib.File.Create(filePath))
                {
                    // Filling in the blanks with the song's metadata. It's like a mini-biography.
                    TitleTextBox.Text = file.Tag.Title;
                    ArtistTextBox.Text = string.Join(", ", file.Tag.Performers);
                    AlbumTextBox.Text = file.Tag.Album;
                    YearTextBox.Text = file.Tag.Year.ToString();
                    LoadAlbumArt(file); // And for the grand finale, the album art makes its appearance.
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hit a bump loading this file: {ex.Message}");
            }
        }

        private void AddToRecentSongs(string filePath)
        {
            // First, let's do a quick check - is this tune already on our recent hits list?
            // We're all for replaying the classics, but let's keep our list unique, shall we?
            if (!recentSongs.Contains(filePath))
            {
                // Looks like it's a fresh hit! Let's add this track to our collection of recent jams.
                // This way, our musical memory keeps growing, always ready with your latest favorites.
                recentSongs.Add(filePath);
            }
        }



        // Album art isn't just about the visuals; it's about setting the mood, telling a story with a glance.
        private void LoadAlbumArt(TagLib.File file)
        {
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
                    albumArtImage.Source = image; // Voilà, our musical story now has a face.
                }
            }
            else
            {
                albumArtImage.Source = null; // No picture? No problem. We'll leave it to the imagination.
            }
        }

        // Keeping an eye on the time isn't just polite; it's essential for tracking our journey through the song.
        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            progressBar.Maximum = mediaPlayer.NaturalDuration.HasTimeSpan ? mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds : 0;
            UpdateTimeDisplay(); // A quick setup to ensure our time display is on point.
        }

        // Like the ticking of a clock, our progress timer keeps us in sync with the song's flow.
        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            progressBar.Value = mediaPlayer.Position.TotalSeconds;
            UpdateTimeDisplay(); // Always keeping you in the loop with how much song you've enjoyed and how much is still to come.
        }

        // When the music's over, we take a moment to reset, readying ourselves for the next adventure.
        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();
            progressTimer.Stop();
            progressBar.Value = 0;
            UpdateTimeDisplay(); // Back to square one, but with new songs to discover.
        }

        // And when it's time to say goodbye, we do so gracefully, making sure everything's wrapped up nicely.
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Our song's timeline isn't just numbers; it's a narrative of our listening experience, beautifully told in minutes and seconds.
        private void UpdateTimeDisplay()
        {
            var totalSeconds = mediaPlayer.NaturalDuration.HasTimeSpan ? mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds : 0;
            var currentSeconds = mediaPlayer.Position.TotalSeconds;
            songTimeDisplay.Text = $"{TimeSpan.FromSeconds(currentSeconds):m\\:ss}/{TimeSpan.FromSeconds(totalSeconds):m\\:ss}";
        }
    }
}


// http://taglib.org/api/ 
// https://chat.openai.com/
// https://www.github.com/
// https://www.microsoft.com/learn
