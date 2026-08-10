using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.PerformanceData;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using imageViewer;

using Microsoft.WindowsAPICodePack.Dialogs;

namespace ImageViewer
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private List<string> _images = new List<string>();
        private int _currentIndex = 0;
        private FileSystemWatcher _fileWatcher;
        private string _currentDirectory;
        private string _metadataString;
        private string _comfyString;
        private string _workflowString;
        private string _fileCounter;
        private string _titleText;
        private bool _isDragging = false;
        private Point _startPoint;
        private double _scrollStartH;
        private double _scrollStartV;
        private bool isDark=true;

        public string MetadataString { get => _metadataString; set { _metadataString = value; OnPropertyChanged(); } }
        public string ComfyString { get => _comfyString; set { _comfyString = value; OnPropertyChanged(); } }
        public string WorkflowString { get => _workflowString; set { _workflowString = value; OnPropertyChanged(); } }
        public string FileCounter { get => _fileCounter; set { _fileCounter = value; OnPropertyChanged(); } }

        public string TitleText { get => _titleText; set { _titleText = value; OnPropertyChanged(); } }


        // Update constructor to accept startup path
        public MainWindow(string startupFilePath = null)
        {
            InitializeComponent();

            DataContext = this;

            TitleText = "AI Image Viewer";

            var dict = new ResourceDictionary();
            dict.Source = isDark
                ? new Uri("DarkTheme.xaml", UriKind.Relative)
                : new Uri("LightTheme.xaml", UriKind.Relative);
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);

            if (!string.IsNullOrEmpty(startupFilePath) && File.Exists(startupFilePath))
            {
                LoadFolderFromFilePath(startupFilePath);
            }
            else
            {
                // Optional: Do whatever your default app startup did before (e.g., empty state)
                //CounterText.Text = "0 / 0";
            }
        }

        private void UpdateCounter()
        {
            FileCounter = $"{_currentIndex + 1} / {_images.Count}";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 5. Update your LoadImage method
        private void SetupFileWatcher(string directoryPath)
        {
            // Dispose old watcher if switching folders
            if (_fileWatcher != null)
            {
                _fileWatcher.Dispose();
                _fileWatcher = null;
            }
            Debug.WriteLine($"WATCHING FOLDER: {directoryPath}");
            _currentDirectory = directoryPath;
            _fileWatcher = new FileSystemWatcher(_currentDirectory)
            {
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
            };

            _fileWatcher.Created += FileWatcher_Created;
            _fileWatcher.Renamed += FileWatcher_Created;
            _fileWatcher.EnableRaisingEvents = true;
        }

        // Helper to load the folder and set the correct index based on Windows Explorer launch
        private void LoadFolderFromFilePath(string filePath)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (string.IsNullOrEmpty(directory)) return;

                // Get all supported images in that folder (just like your "Open Folder" button does)
                string[] extensions = { ".png", ".jpg", ".jpeg", ".webp" };
                _images = Directory.GetFiles(directory)
                                   .Where(file => extensions.Contains(Path.GetExtension(file).ToLower()))
                                   .ToList();

                // Find the index of the file that was double-clicked
                _currentIndex = _images.IndexOf(filePath);
                if (_currentIndex < 0) _currentIndex = 0;

                SetupFileWatcher(directory);

                // Load the image and update UI/metadata
                LoadImage();
            }
            catch (Exception ex)
            {
                MetadataString = $"Error loading folder context: {ex.Message}";
            }
        }

        private void FileWatcher_Created(object sender, FileSystemEventArgs e)
        {
            Debug.WriteLine($"WATCHER FIRED FOR: {e.FullPath}");

            string ext = Path.GetExtension(e.FullPath).ToLower();
            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".webp")
            {
                // Hop back to the UI thread
                Dispatcher.Invoke(() =>
                {
                    // Remember currently viewed file path
                    string currentPath = (_images.Count > 0 && _currentIndex >= 0 && _currentIndex < _images.Count)
                        ? _images[_currentIndex]
                        : null;

                    // Refresh the file list from disk
                    string[] extensions = { ".png", ".jpg", ".jpeg", ".webp" };
                    _images = Directory.GetFiles(_currentDirectory)
                                       .Where(file => extensions.Contains(Path.GetExtension(file).ToLower()))
                                       .ToList();

                    // Try to keep the user looking at the exact same image they were on
                    if (!string.IsNullOrEmpty(currentPath))
                    {
                        int foundIndex = _images.IndexOf(currentPath);
                        if (foundIndex >= 0)
                        {
                            _currentIndex = foundIndex;
                        }
                    }

                    // Optional: update a counter/UI indicator if you have one showing total files
                    UpdateCounter();
                });
            }
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Left) Prev_Click(null, null);
            if (e.Key == System.Windows.Input.Key.Right) Next_Click(null, null);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _fileWatcher?.Dispose();
        }

        // Replace your OpenFolder_Click method with this:
        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            Debug.WriteLine("Open button clicked!");
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                _images = Directory.GetFiles(dialog.FileName, "*.*")
                    .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (_images.Any())
                {
                    _currentIndex = 0;
                    SetupFileWatcher(dialog.FileName);
                    LoadImage();
                }
                else
                {
                    MessageBox.Show("No PNG or JPG images found in this folder.");
                }
            }
        }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {
            if (_images.Count == 0) return;

            _currentIndex--;
            if (_currentIndex < 0)
            {
                _currentIndex = _images.Count - 1; // Wrap around to the last image
            }
            LoadImage();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_images.Count == 0) return;

            _currentIndex++;
            if (_currentIndex >= _images.Count)
            {
                _currentIndex = 0; // Wrap around to the first image
            }
            LoadImage();
        }

        private void FitToArea_Click(object sender, RoutedEventArgs e)
        {
            ApplyFitToArea();
        }

        private void ApplyFitToArea()
        {
            if (MainImage.Source is BitmapSource bitmapSource)
            {
                MainImage.Width = double.NaN;
                MainImage.Height = double.NaN;
                MainImage.Stretch = Stretch.None;

                // Ensure layout has updated so ViewportWidth/Height are available
                ImageScrollViewer.UpdateLayout();

                double availableWidth = ImageScrollViewer.ViewportWidth;
                double availableHeight = ImageScrollViewer.ViewportHeight;

                if (availableWidth <= 0 || availableHeight <= 0) return;

                double scaleX = availableWidth / bitmapSource.PixelWidth;
                double scaleY = availableHeight / bitmapSource.PixelHeight;
                double fitScale = Math.Min(scaleX, scaleY);

                ImageScale.ScaleX = fitScale;
                ImageScale.ScaleY = fitScale;

                ImageScrollViewer.ScrollToHorizontalOffset(0);
                ImageScrollViewer.ScrollToVerticalOffset(0);
            }
        }
        private void ActualSize_Click(object sender, RoutedEventArgs e)
        {
            if (MainImage.Source is BitmapSource bitmapSource)
            {
                MainImage.Stretch = Stretch.None;
                ImageScale.ScaleX = 1;
                ImageScale.ScaleY = 1;
                MainImage.Width = bitmapSource.PixelWidth;
                MainImage.Height = bitmapSource.PixelHeight;
            }
        }

        private void ImageScrollViewer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MainImage.Source == null) return;

            _isDragging = true;
            _startPoint = e.GetPosition(ImageScrollViewer);
            _scrollStartH = ImageScrollViewer.HorizontalOffset;
            _scrollStartV = ImageScrollViewer.VerticalOffset;

            ImageScrollViewer.CaptureMouse();
        }

        private void ImageScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            Point currentPoint = e.GetPosition(ImageScrollViewer);
            Vector delta = currentPoint - _startPoint;

            ImageScrollViewer.ScrollToHorizontalOffset(_scrollStartH - delta.X);
            ImageScrollViewer.ScrollToVerticalOffset(_scrollStartV - delta.Y);
        }

        private void ImageScrollViewer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ImageScrollViewer.ReleaseMouseCapture();
            }
        }

        private void ImageScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            if(e.Delta > 0)
                ZoomIn();
            else
                ZoomOut();

            MainImage.Stretch = Stretch.None;
        }

        private void ZoomIn()
        {
            // Adjust these values to match your current zoom step
            ImageScale.ScaleX += 0.1;
            ImageScale.ScaleY += 0.1;
        }

        private void ZoomOut()
        {
            ImageScale.ScaleX = Math.Max(0.1, ImageScale.ScaleX - 0.1);
            ImageScale.ScaleY = Math.Max(0.1, ImageScale.ScaleY - 0.1);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // Make sure there is actually an image currently loaded
            if (_images == null || _images.Count == 0 || _currentIndex < 0 || _currentIndex >= _images.Count)
                return;

            string fileToDelete = _images[_currentIndex];

            // 1. Confirm with the user
            MessageBoxResult result = MessageBox.Show(
                $"Are you sure you want to delete this image?\n\n{System.IO.Path.GetFileName(fileToDelete)}",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // 2. Delete the file from the hard drive
                    MoveToRecycleBin(fileToDelete);

                    // 3. Remove from our internal list
                    _images.RemoveAt(_currentIndex);

                    // 4. Adjust the index (if we deleted the very last image, step back by one)
                    if (_currentIndex >= _images.Count)
                    {
                        _currentIndex = _images.Count - 1;
                    }

                    // 5. Update the UI
                    if (_images.Count > 0)
                    {
                        LoadImage(); // This will load the next image and update the counter
                    }
                    else
                    {
                        // Folder is now empty, clear the screen
                        MainImage.Source = null;
                        MetadataString = "";
                        UpdateCounter();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not delete file.\n\nError: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MoveToRecycleBin(string filePath)
        {
            var shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType != null)
            {
                dynamic shellApp = Activator.CreateInstance(shellType);
                // 10 represents the Windows Recycle Bin (ssfBITBUCKET)
                dynamic recycleBin = shellApp.Namespace(10);
                recycleBin.MoveHere(filePath);
            }
            else
            {
                // Fallback if COM is unavailable
                System.IO.File.Delete(filePath);
            }
        }

        private void LoadImage()
        {
            string path = _images[_currentIndex];

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            MainImage.Source = bitmap;

            ExtractMetadata(path);
            ApplyFitToArea();
            UpdateCounter();
            TitleText = $"AI Image Viewer - {System.IO.Path.GetFileName(path)} - {bitmap.PixelWidth}x{bitmap.PixelHeight}";

        }

        // Safely attempts to read a metadata query without crashing if it's missing
        private string TryGetMetadata(BitmapMetadata metadata, string query)
        {
            try
            {
                if (metadata.ContainsQuery(query))
                {
                    var result = metadata.GetQuery(query);

                    // If WPF successfully parses it as a string
                    if (result is string str)
                        return str;

                    // If WPF returns it as raw binary data (very common for A1111 parameters)
                    if (result is BitmapMetadataBlob blob)
                    {
                        byte[] bytes = blob.GetBlobValue();
                        // Depending on the chunk, it might have a null terminator we need to trim
                        return System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0');
                    }
                }
            }
            catch
            {
                // Ignore WPF exceptions for missing/invalid chunks
            }
            return null;
        }

        private void ExtractMetadata(string path)
        {
            try
            {
                string outputText = "";

                // 1. Bypass WPF's broken metadata reader and parse the PNG chunks directly
                var pngChunks = ReadPngTextChunks(path);

                // 2. Build the output string in the exact order requested: Parameters first.
                if (pngChunks.TryGetValue("parameters", out string parameters))
                {
                    outputText += $"--- Parameters ---\n{parameters}\n\n";
                }

                if (pngChunks.TryGetValue("prompt", out string comfyPrompt))
                {
                    ComfyString = $"--- ComfyUI Prompt ---\n{FormatJson(comfyPrompt)}\n\n";
                }

                if (pngChunks.TryGetValue("workflow", out string comfyWorkflow))
                {
                    WorkflowString += $"--- ComfyUI Workflow ---\n{FormatJson(comfyWorkflow)}\n\n";
                }

                // 3. Fallback for Midjourney (Uses WPF only for standard EXIF data)
                if (string.IsNullOrEmpty(outputText))
                {
                    using (Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.None);
                        var metadata = decoder.Frames[0].Metadata as BitmapMetadata;
                        if (metadata != null)
                        {
                            string exifDesc = TryGetMetadata(metadata, "/app1/ifd/{ushort=270}");
                            if (!string.IsNullOrEmpty(exifDesc))
                            {
                                outputText += $"--- Midjourney Metadata ---\n{exifDesc}\n\n";
                            }
                        }
                    }
                }

                MetadataString = !string.IsNullOrWhiteSpace(outputText) ? outputText.TrimEnd() : "No recognized AI metadata found.";
            }
            catch (Exception ex)
            {
                MetadataString = $"Error reading metadata: {ex.Message}";
            }
        }

        // A rock-solid manual PNG chunk parser to bypass WPF limitations
        private Dictionary<string, string> ReadPngTextChunks(string path)
        {
            var chunks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var br = new BinaryReader(fs))
                {
                    // Verify PNG Signature
                    if (fs.Length < 8) return chunks;
                    byte[] magic = br.ReadBytes(8);
                    if (magic[0] != 137 || magic[1] != 80 || magic[2] != 78 || magic[3] != 71)
                        return chunks;

                    while (fs.Position < fs.Length)
                    {
                        // Read Chunk Length (PNG uses Big-Endian, so we reverse it for Windows)
                        byte[] lengthBytes = br.ReadBytes(4);
                        if (lengthBytes.Length < 4) break;
                        Array.Reverse(lengthBytes);
                        int length = BitConverter.ToInt32(lengthBytes, 0);

                        // Read Chunk Type
                        string type = System.Text.Encoding.ASCII.GetString(br.ReadBytes(4));

                        if (type == "IEND") break;

                        // Handle standard text chunks
                        if (type == "tEXt")
                        {
                            byte[] data = br.ReadBytes(length);
                            int nullIndex = Array.IndexOf(data, (byte)0);
                            if (nullIndex > 0)
                            {
                                string key = System.Text.Encoding.ASCII.GetString(data, 0, nullIndex);
                                string text = System.Text.Encoding.UTF8.GetString(data, nullIndex + 1, data.Length - nullIndex - 1);
                                chunks[key] = text;
                            }
                        }
                        // Handle uncompressed international text chunks (sometimes used by newer ComfyUI)
                        else if (type == "iTXt")
                        {
                            byte[] data = br.ReadBytes(length);
                            int nullIndex = Array.IndexOf(data, (byte)0);
                            if (nullIndex > 0)
                            {
                                string key = System.Text.Encoding.ASCII.GetString(data, 0, nullIndex);

                                // Check if uncompressed (Compression flag == 0)
                                if (data[nullIndex + 1] == 0)
                                {
                                    // Skip language and translated keyword (find 2 more null bytes)
                                    int textStart = nullIndex + 3;
                                    int nullCount = 0;
                                    for (int i = textStart; i < data.Length; i++)
                                    {
                                        if (data[i] == 0) nullCount++;
                                        if (nullCount == 2)
                                        {
                                            textStart = i + 1;
                                            break;
                                        }
                                    }
                                    if (textStart < data.Length)
                                    {
                                        chunks[key] = System.Text.Encoding.UTF8.GetString(data, textStart, data.Length - textStart);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Skip unknown/image chunks safely
                            fs.Seek(length, SeekOrigin.Current);
                        }

                        // Skip CRC (4 bytes) at the end of every chunk
                        fs.Seek(4, SeekOrigin.Current);
                    }
                }
            }
            catch
            {
                // Ignore file read locks and return whatever we managed to parse
            }
            return chunks;
        }

        private string FormatJson(string raw)
        {
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(raw);
                return JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return raw;
            }
        }

        private void SysButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Create an instance of your settings window
            SettingsWindow settingsDlg = new SettingsWindow();

            // 2. (Optional) Set its Owner so it stays centered and stacked properly over the main window
            settingsDlg.Owner = this;
            settingsDlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // 3. Open it modally (halts execution here and blocks the main window)
            settingsDlg.ShowDialog();
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ZoomIn();
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ZoomOut();
        }
    }
}