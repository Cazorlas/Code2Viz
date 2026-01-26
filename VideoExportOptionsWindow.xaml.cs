using System.Windows;
using System.Windows.Media;

namespace Code2Viz;

public partial class VideoExportOptionsWindow : Window
{
    public Brush? SelectedBackground { get; private set; }
    public bool IncludeGrid { get; private set; }
    public double Duration { get; private set; } = 5.0;
    public int Fps { get; private set; } = 30;
    public uint Bitrate { get; private set; } = 5;

    public VideoExportOptionsWindow()
    {
        InitializeComponent();
        SliderDuration.ValueChanged += (s, e) => UpdateDisplay();
        SliderFps.ValueChanged += (s, e) => UpdateDisplay();
        SliderBitrate.ValueChanged += (s, e) => UpdateDisplay();
        UpdateDisplay();
    }

    public void SetDuration(double duration)
    {
        Duration = duration;
        if (duration >= 1 && duration <= 60)
        {
            SliderDuration.Value = duration;
        }
        else
        {
            SliderDuration.Value = Math.Min(60, Math.Max(1, duration));
        }
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        Duration = SliderDuration.Value;
        Fps = (int)SliderFps.Value;
        Bitrate = (uint)SliderBitrate.Value;

        TextDuration.Text = Duration.ToString("0");
        TextFps.Text = Fps.ToString();
        TextBitrate.Text = Bitrate.ToString();

        int totalFrames = (int)(Duration * Fps);
        // Rough estimate: bitrate * duration / 8 (bits to bytes)
        double estimatedSizeMB = (Bitrate * Duration) / 8.0;
        TextFrameInfo.Text = $"Total frames: {totalFrames} | Estimated size: ~{estimatedSizeMB:F1} MB";
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (RadioCanvas.IsChecked == true)
            SelectedBackground = null;
        else if (RadioWhite.IsChecked == true)
            SelectedBackground = Brushes.White;
        else if (RadioBlack.IsChecked == true)
            SelectedBackground = Brushes.Black;

        IncludeGrid = CheckShowGrid.IsChecked == true;
        Duration = SliderDuration.Value;
        Fps = (int)SliderFps.Value;
        Bitrate = (uint)SliderBitrate.Value;

        DialogResult = true;
        Close();
    }
}
