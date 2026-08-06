using DevWinUI;
using JustBedwars.Models;
using JustBedwars.Services;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace JustBedwars.Views
{
    public sealed partial class StatsPage : Page
    {
        private readonly HypixelApi _hypixelApi;
        private readonly SettingsService _settingsService;
        private readonly AptabaseClient _aptabaseClient;
        private readonly HttpClient _httpClient;
        private const string ApiKeySettingName = "HypixelApiKey";

        public StatsPage()
        {
            this.InitializeComponent();
            _hypixelApi = new HypixelApi();
            _settingsService = new SettingsService();
            _httpClient = new HttpClient();
            LoadApiKey();

            var instance = (App)Application.Current;
            _aptabaseClient = instance.AptabaseClient;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string username)
            {
                LoadPlayerStats(username);
            }
        }

        private void LoadApiKey()
        {
            var apiKey = _settingsService.GetValue(ApiKeySettingName);
            if (apiKey != null)
            {
                _hypixelApi.SetApiKey((string)apiKey);
            }
        }

        public async void LoadPlayerStats(string username)
        {
            ScreenshotButtonBorder.Visibility = Visibility.Collapsed;
            AccentBgImage.Opacity = 0;
            var props = new
            {
                username = username,
                source = "SearchPage"
            };
            _ = _aptabaseClient.TrackEvent("PlayerSearched", props);

            if (string.IsNullOrWhiteSpace(username))
            {
                return;
            }
            UsernameAutoSuggestBox.Text = username;

            ProgressBars.Visibility = Visibility.Collapsed;
            SetLoaderVisibility(Visibility.Visible);
            ImageLoader.Visibility = Visibility.Visible;
            ImageError.Visibility = Visibility.Collapsed;
            SetStatsVisibility(Visibility.Collapsed);
            ContentBorder.Visibility = Visibility.Visible;
            PlayerImage.Source = null;

            var player = await _hypixelApi.GetPlayerStats(username);

            if (player != null)
            {
                if (player.PlayerTag != "NICK" && player.PlayerTag != "ERROR")
                {
                    HypixelLevelText.Text = $"Hypixel Level: {player.HypixelLevel}";
                    HypixelLevelProgress.Value = player.HypixelLevelProgress;
                    BedwarsLevelText.Text = $"Bedwars Level: {player.Star}";
                    BedwarsLevelProgress.Value = player.BedwarsLevelProgress;
                    ProgressBars.Visibility = Visibility.Visible;
                    ScreenshotButton.IsEnabled = false;
                    ScreenshotButtonBorder.Visibility = Visibility.Visible;

                    _ = GetAccentImage(player.Username??"");
                }
                UsernameTextBlock.Text = player.Username;
                StarTextBlock.Text = player.Star.ToString();
                FkdrTextBlock.Text = player.FKDR.ToString();
                WlrTextBlock.Text = player.WLR.ToString();
                BblrTextBlock.Text = player.BBLR.ToString();
                FinalsTextBlock.Text = player.Finals.ToString();
                WinsTextBlock.Text = player.Wins.ToString();
                KdrTextBlock.Text = player.KDR.ToString();
                FinalDeathsTextBlock.Text = player.FinalDeaths.ToString();
                LossesTextBlock.Text = player.Losses.ToString();
                KillsTextBlock.Text = player.Kills.ToString();
                DeathsTextBlock.Text = player.Deaths.ToString();
                BedsLostTextBlock.Text = player.BedsLost.ToString();
                BedsTextBlock.Text = player.Beds.ToString();
                TagTextBlock.Text = player.PlayerTag;
                FirstLoginTextBlock.Text = player.FirstLoginDate;
                SetStatsVisibility(Visibility.Visible);
                SetLoaderVisibility(Visibility.Collapsed);

                if (!string.IsNullOrEmpty(player.PlayerUUID))
                {
                    PlayerImage.Source = new BitmapImage(new Uri($"https://skins.jbw.fexei.at/fullbody/{player.PlayerUUID}"));
                    var response = await _httpClient
                        .GetAsync($"https://starlightskins.lunareclipse.studio/render/default/{player.PlayerUUID}/full");
                    if (response.IsSuccessStatusCode)
                    {
                        PlayerImage.Source = new BitmapImage(new Uri(
                            $"https://starlightskins.lunareclipse.studio/render/default/{player.PlayerUUID}/full"));
                    }
                }
                else
                {
                    ImageError.Visibility = Visibility.Visible;
                    ImageLoader.Visibility = Visibility.Collapsed;
                    PlayerImage.Source = null;
                    StarTextBlock.Text = "Player not found!";
                    FkdrTextBlock.Text = "Error";
                    WlrTextBlock.Text = "Error";
                    BblrTextBlock.Text = "Error";
                    FinalsTextBlock.Text = "Error";
                    WinsTextBlock.Text = "Error";
                    KdrTextBlock.Text = "Error";
                    FirstLoginTextBlock.Text = "Error";
                    FinalDeathsTextBlock.Text = "Error";
                    LossesTextBlock.Text = "Error";
                    KillsTextBlock.Text = "Error";
                    DeathsTextBlock.Text = "Error";
                    TagTextBlock.Text = "Error";
                    BedsTextBlock.Text = "Error";
                    BedsLostTextBlock.Text = "Error";
                }
            }
            else
            {
                PlayerImage.Source = null;
                UsernameTextBlock.Text = "Player not found";
                StarTextBlock.Text = string.Empty;
                FkdrTextBlock.Text = string.Empty;
                WlrTextBlock.Text = string.Empty;
                BblrTextBlock.Text = string.Empty;
                FinalsTextBlock.Text = string.Empty;
                WinsTextBlock.Text = string.Empty;
                KdrTextBlock.Text = string.Empty;
                FirstLoginTextBlock.Text = string.Empty;
                FinalDeathsTextBlock.Text = string.Empty;
                LossesTextBlock.Text = string.Empty;
                KillsTextBlock.Text = string.Empty;
                DeathsTextBlock.Text = string.Empty;
                TagTextBlock.Text = string.Empty;
                BedsTextBlock.Text = string.Empty;
                BedsLostTextBlock.Text = string.Empty;
                SetLoaderVisibility(Visibility.Visible);
            }
        }

        private async Task GetAccentImage(string playername)
        {
            AccentBgImage.Source =
                new BitmapImage(new Uri(
                    await _httpClient.GetStringAsync(
                        $"https://jbw.fexei.at/api/justbedwars/v2/bgImage?username={playername}")));
        }

        private async void UsernameAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var query = sender.Text;
                if (string.IsNullOrWhiteSpace(query))
                {
                    return;
                }

                try
                {
                    var response = await _httpClient.GetStringAsync($"http://jbw.fexei.at/api/justbedwars/v2/autocomplete?query={query}&limit=10&mode=name");
                    var suggestions = JsonConvert.DeserializeObject<List<string>>(response);
                    sender.ItemsSource = suggestions;
                }
                catch (HttpRequestException)
                {
                    // Handle API errors gracefully
                }
            }
        }

        private void UsernameAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = args.SelectedItem.ToString();
            // LoadPlayerStats(sender.Text);
        }

        private void PlayerImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            ImageLoader.Visibility = Visibility.Collapsed;
            ScreenshotButton.IsEnabled = true;
        }

        private void PlayerImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ImageLoader.Visibility = Visibility.Collapsed;
            ImageError.Visibility = Visibility.Visible;
        }

        private void SetStatsVisibility(Visibility visibility)
        {
            UsernameLabel.Visibility = visibility;
            UsernameTextBlock.Visibility = visibility;
            StarLabel.Visibility = visibility;
            StarTextBlock.Visibility = visibility;
            Stars.Visibility = visibility;
            FkdrLabel.Visibility = visibility;
            FkdrTextBlock.Visibility = visibility;
            FKDR.Visibility = visibility;
            WlrLabel.Visibility = visibility;
            WlrTextBlock.Visibility = visibility;
            WLR.Visibility = visibility;
            FinalsLabel.Visibility = visibility;
            FinalsTextBlock.Visibility = visibility;
            Finals.Visibility = visibility;
            WinsLabel.Visibility = visibility;
            WinsTextBlock.Visibility = visibility;
            Wins.Visibility = visibility;
            KdrLabel.Visibility = visibility;
            KdrTextBlock.Visibility = visibility;
            KDR.Visibility = visibility;
            FirstLoginLabel.Visibility = visibility;
            FirstLoginTextBlock.Visibility = visibility;
            FinalDeathsLabel.Visibility = visibility;
            FinalDeathsTextBlock.Visibility = visibility;
            FinalDeaths.Visibility = visibility;
            LossesLabel.Visibility = visibility;
            LossesTextBlock.Visibility = visibility;
            Losses.Visibility = visibility;
            KillsLabel.Visibility = visibility;
            KillsTextBlock.Visibility = visibility;
            Kills.Visibility = visibility;
            DeathsLabel.Visibility = visibility;
            DeathsTextBlock.Visibility = visibility;
            Deaths.Visibility = visibility;
            TagLabel.Visibility = visibility;
            TagTextBlock.Visibility = visibility;
            BblrLabel.Visibility = visibility;
            BblrTextBlock.Visibility = visibility;
            BBLR.Visibility = visibility;
            BedsLabel.Visibility = visibility;
            BedsTextBlock.Visibility = visibility;
            Beds.Visibility = visibility;
            BedsLostLabel.Visibility = visibility;
            BedsLostTextBlock.Visibility = visibility;
            BedsLost.Visibility = visibility;
        }

        private void SetLoaderVisibility(Visibility visibility)
        {
            StarsLoader.Visibility = visibility;
            FkdrLoader.Visibility = visibility;
            WlrLoader.Visibility = visibility;
            FinalsLoader.Visibility = visibility;
            FinalDeathsLoader.Visibility = visibility;
            WinsLoader.Visibility = visibility;
            LossesLoader.Visibility = visibility;
            KdrLoader.Visibility = visibility;
            KillsLoader.Visibility = visibility;
            DeathsLoader.Visibility = visibility;
            BblrLoader.Visibility = visibility;
            BedsLoader.Visibility = visibility;
            BedsLostLoader.Visibility = visibility;
            HypixelLevelProgressLoad.Visibility = visibility;
            HypixelLevelTextLoad.Visibility = visibility;
            BedwarsLevelProgressLoad.Visibility = visibility;
            BedwarsLevelTextLoad.Visibility = visibility;
            ProgressBarsLoader.Visibility = visibility;
        }

        private void UsernameAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            LoadPlayerStats(sender.Text);
        }

        private void AccentBgImage_OnImageOpened(object sender, RoutedEventArgs e)
        {
            ScalarTransition transition = new()
            {
                Duration = new TimeSpan(0, 0, 0, 0, 800)
            };
            AccentBgImage.OpacityTransition = transition;
            AccentBgImage.Opacity = App.Current.RequestedTheme == ApplicationTheme.Dark? 0.3 : 0.1;
        }

        private async void ScreenshotButton_OnClick(object sender, RoutedEventArgs e)
        {
            ScreenshotButton.IsEnabled = false;
            UsernameAutoSuggestBox.IsEnabled = false;
            RenderTargetBitmap rtb = new RenderTargetBitmap();
            ScreenshotLoadingIndicator.Visibility = Visibility.Visible;
            await AnimateScreenshotCard(false);

            if (AccentBgImage.Source is BitmapImage image)
            {
                AccentBgRenderImage.Source = await CreateBlurredImageSourceAsync(image, 30);
                AccentBgRenderImage.Visibility = Visibility.Visible;
            }

            ScreenshotAd.Visibility = Visibility.Visible;

            DateUpdatedTextBlock.Text = $"Captured on\n{DateTime.Now.ToString(CultureInfo.InvariantCulture)}";
            ScreenshotGrid.RequestedTheme = ElementTheme.Dark;
            ScreenshotGrid.UpdateLayout();

            await Task.Delay(100);

            await rtb.RenderAsync(ScreenshotGrid);
            double dpi = WindowHelper.GetDpiForWindow(App.Window);

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Window);

            var savePicker = new FileSavePicker();
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
            savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            savePicker.FileTypeChoices.Add("PNG", new List<string>() { ".png" });
            savePicker.FileTypeChoices.Add("JPEG", new List<string>() { ".jpg" });
            savePicker.SuggestedFileName = $"{UsernameTextBlock.Text}_JBW";

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file == null)
            {
                ScreenshotLoadingIndicator.Visibility = Visibility.Collapsed;
                AccentBgRenderImage.Visibility = Visibility.Collapsed;
                ScreenshotAd.Visibility = Visibility.Collapsed;
                ScreenshotGrid.RequestedTheme = ElementTheme.Default;
                await AnimateScreenshotCard(true);
                ScreenshotButton.IsEnabled = true;
                UsernameAutoSuggestBox.IsEnabled = true;
                return;
            }

            var pixelBuffer = await rtb.GetPixelsAsync();
            byte[] pixels = pixelBuffer.ToArray();

            using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                Guid encoderId = file.FileType == ".png" ? BitmapEncoder.PngEncoderId : BitmapEncoder.JpegEncoderId;
                var encoder = await BitmapEncoder.CreateAsync(encoderId, stream);

                BitmapAlphaMode alphaMode = file.FileType == ".png" ? BitmapAlphaMode.Premultiplied : BitmapAlphaMode.Ignore;

                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8,
                    alphaMode,
                    (uint)rtb.PixelWidth,
                    (uint)rtb.PixelHeight,
                    dpi,
                    dpi,
                    pixels);

                await encoder.FlushAsync();
            }

            ScreenshotLoadingIndicator.Visibility = Visibility.Collapsed;
            AccentBgRenderImage.Visibility = Visibility.Collapsed;
            ScreenshotAd.Visibility = Visibility.Collapsed;
            ScreenshotGrid.RequestedTheme = ElementTheme.Default;

            await AnimateScreenshotCard(true);

            ScreenshotButton.IsEnabled = true;
            UsernameAutoSuggestBox.IsEnabled = true;

            TeachingTipImage.Source = rtb;
            TeachingTip.IsOpen = true;
        }

        private async Task AnimateScreenshotCard(bool back)
        {
            var visual = ElementCompositionPreview.GetElementVisual(ContentBorder);
            var compositor = visual.Compositor;

            var centerPointAnimation = compositor.CreateExpressionAnimation("Vector3(this.Target.Size.X / 2, this.Target.Size.Y / 2, 0)");

            if (!back)
            {
                var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
                opacityAnimation.InsertKeyFrame(0, 1);
                opacityAnimation.InsertKeyFrame(1, 0);
                opacityAnimation.DelayTime = new TimeSpan(0, 0, 0, 0, 700);
                opacityAnimation.Duration = new TimeSpan(0, 0, 0, 0, 300);

                var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
                scaleAnimation.InsertKeyFrame(0, new Vector3(1, 1, 1));
                scaleAnimation.InsertKeyFrame(0.3f, new Vector3(0.9f, 0.9f, 1), CompositionEasingFunction.CreateCircleEasingFunction(compositor, CompositionEasingFunctionMode.InOut));
                scaleAnimation.InsertKeyFrame(0.35f, new Vector3(0.9f, 0.9f, 1));
                scaleAnimation.InsertKeyFrame(1, new Vector3(0.4f, 0.4f, 1), CompositionEasingFunction.CreateCircleEasingFunction(compositor, CompositionEasingFunctionMode.In));
                scaleAnimation.Duration = new TimeSpan(0, 0, 0, 1);

                var translationAnimation = compositor.CreateVector3KeyFrameAnimation();
                translationAnimation.InsertKeyFrame(0, new Vector3(0, 0, 0));
                translationAnimation.InsertKeyFrame(0.3f, new Vector3(0, -5, 0), CompositionEasingFunction.CreateCircleEasingFunction(compositor, CompositionEasingFunctionMode.InOut));
                translationAnimation.InsertKeyFrame(0.35f, new Vector3(0, -5, 0));
                translationAnimation.InsertKeyFrame(1, new Vector3(0, 500, 0), CompositionEasingFunction.CreateCircleEasingFunction(compositor, CompositionEasingFunctionMode.In));
                translationAnimation.Duration = new TimeSpan(0, 0, 0, 1);

                visual.StartAnimation("CenterPoint", centerPointAnimation);
                visual.StartAnimation("Opacity", opacityAnimation);
                visual.StartAnimation("Scale", scaleAnimation);
                visual.StartAnimation("Translation", translationAnimation);

                await Task.Delay(1000);
            }
            else
            {
                var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
                opacityAnimation.InsertKeyFrame(0, 0);
                opacityAnimation.InsertKeyFrame(1, 1);
                opacityAnimation.Duration = new TimeSpan(0, 0, 0, 0, 300);

                var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
                scaleAnimation.InsertKeyFrame(0, new Vector3(0.9f, 0.9f, 1));
                scaleAnimation.InsertKeyFrame(1, new Vector3(1, 1, 1));
                scaleAnimation.Duration = new TimeSpan(0, 0, 0, 0, 300);

                var translationAnimation = compositor.CreateVector3KeyFrameAnimation();
                translationAnimation.InsertKeyFrame(0, new Vector3(0, 0, 0));
                translationAnimation.InsertKeyFrame(1, new Vector3(0, 0, 0));
                translationAnimation.Duration = new TimeSpan(0, 0, 0, 0, 300);

                visual.StartAnimation("CenterPoint", centerPointAnimation);
                visual.StartAnimation("Opacity", opacityAnimation);
                visual.StartAnimation("Scale", scaleAnimation);
                visual.StartAnimation("Translation", translationAnimation);

                await Task.Delay(300);
            }
        }

        public async Task<SoftwareBitmapSource> CreateBlurredImageSourceAsync(BitmapImage bitmapImage, float blurAmount = 10.0f, float darkenAmount = 0.4f)
        {
            try
            {
                if (bitmapImage == null || bitmapImage.UriSource == null)
                {
                    return await CreateTransparentImageSourceAsync();
                }

                if (bitmapImage.PixelWidth == 0)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    RoutedEventHandler? onOpened = null;
                    ExceptionRoutedEventHandler? onFailed = null;

                    onOpened = (s, e) => {
                        bitmapImage.ImageOpened -= onOpened;
                        bitmapImage.ImageFailed -= onFailed;
                        tcs.TrySetResult(true);
                    };

                    onFailed = (s, e) => {
                        bitmapImage.ImageOpened -= onOpened;
                        bitmapImage.ImageFailed -= onFailed;
                        tcs.TrySetResult(false);
                    };

                    bitmapImage.ImageOpened += onOpened;
                    bitmapImage.ImageFailed += onFailed;

                    bool loadSuccess = await tcs.Task;
                    if (!loadSuccess) return await CreateTransparentImageSourceAsync();
                }

                var device = CanvasDevice.GetSharedDevice();
                using var canvasBitmap = await CanvasBitmap.LoadAsync(device, bitmapImage.UriSource);

                using var blurEffect = new GaussianBlurEffect
                {
                    Source = canvasBitmap,
                    BlurAmount = blurAmount,
                    BorderMode = EffectBorderMode.Hard
                };

                using var renderTarget = new CanvasRenderTarget(
                    device,
                    (float)canvasBitmap.Bounds.Width,
                    (float)canvasBitmap.Bounds.Height,
                    canvasBitmap.Dpi);

                using (var ds = renderTarget.CreateDrawingSession())
                {
                    ds.Clear(Microsoft.UI.Colors.Transparent);

                    ds.DrawImage(blurEffect);

                    if (darkenAmount > 0.0f)
                    {
                        darkenAmount = Math.Clamp(darkenAmount, 0.0f, 1.0f);

                        byte alpha = (byte)(255 * darkenAmount);
                        var overlayColor = Windows.UI.Color.FromArgb(alpha, 0, 0, 0);

                        ds.FillRectangle(
                            0, 0,
                            (float)canvasBitmap.Bounds.Width,
                            (float)canvasBitmap.Bounds.Height,
                            overlayColor);
                    }
                }

                byte[] pixelBytes = renderTarget.GetPixelBytes();

                var softwareBitmap = new SoftwareBitmap(
                    BitmapPixelFormat.Bgra8,
                    (int)renderTarget.SizeInPixels.Width,
                    (int)renderTarget.SizeInPixels.Height,
                    BitmapAlphaMode.Premultiplied);

                softwareBitmap.CopyFromBuffer(pixelBytes.AsBuffer());

                var source = new SoftwareBitmapSource();
                await source.SetBitmapAsync(softwareBitmap);

                return source;
            }
            catch (Exception)
            {
                return await CreateTransparentImageSourceAsync();
            }
        }

        private async Task<SoftwareBitmapSource> CreateTransparentImageSourceAsync()
        {
            var softwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, 1, 1, BitmapAlphaMode.Premultiplied);
            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(softwareBitmap);
            return source;
        }
    }
}