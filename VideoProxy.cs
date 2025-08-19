using FrooxEngine.UIX;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Threading.Tasks;
using Elements.Core;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using SkyFrost.Base;

public class VideoProxy : ResoniteMod
{

    public enum Resolution
    {
        Q480P,
        Q720P,
        Q1080P,
        Q1440P,
        Q2160P,
        QBest
    }

    public enum ProxyLocation
    {
        Australia,
        NorthAmerica,
        CUSTOM
    }

    public override string Author => "LeCloutPanda & Sveken";
    public override string Name => "Video Proxy";
    public override string Version => "1.3.0";
    public override string Link => "https://github.com/LeCloutPanda/VideoProxy";
    private static string youtubeIdRegex = "(?:youtube\\.com\\/(?:[^\\/]+\\/.+\\/|(?:v|e(?:mbed)?|shorts)\\/|.*[?&]v=)|youtu\\.be\\/)([^\"&?\\/\\s]{11})";

    public static ModConfiguration config;
    [AutoRegisterConfigKey] private static ModConfigurationKey<bool> ENABLED = new ModConfigurationKey<bool>("enabledToggle", "Generate option to use VideoProxy when importing a video", () => true);
    [AutoRegisterConfigKey] private static ModConfigurationKey<ProxyLocation> PROXY_LOCATION = new ModConfigurationKey<ProxyLocation>("serverRegion", "Proxy Server Region", () => ProxyLocation.NorthAmerica);
    [AutoRegisterConfigKey] private static ModConfigurationKey<string> PROXY_URI = new ModConfigurationKey<string>("serverAddress", "Proxy Server address(Used when ServerRegion is set to CUSTOM)", () => "http://127.0.0.1:8080/");
    [AutoRegisterConfigKey] private static ModConfigurationKey<Resolution> RESOLUTION = new ModConfigurationKey<Resolution>("resolutionPreset", "Quality Preset: <color=yellow>⚠</color> QBest will load the best quality it can so be careful when using this setting <color=yellow>⚠</color>", () => Resolution.Q720P);
    [AutoRegisterConfigKey] private static ModConfigurationKey<bool> FORCED = new ModConfigurationKey<bool>("forceCodecToggle", "Force h264 Codec: <color=yellow>⚠</color> Force h264 Codec only works for quality levels Q480P, Q720P, Q1080P <color=yellow>⚠</color>", () => false);

    public override void OnEngineInit()
    {
        config = GetConfiguration();
        config.Save(true);

        Harmony harmony = new Harmony("dev.lecloutpanda.videoproxy");
        harmony.PatchAll();
    }

    [HarmonyPatch(typeof(VideoImportDialog), "OpenRoot")]
    class YouTubeProxy
    {
        [HarmonyPostfix]
        private static void OpenRoot(VideoImportDialog __instance, UIBuilder ui)
        {
            if (config.GetValue(ENABLED))
            {

                UIBuilder uIBuilder = ui;
                LocaleString text = "Video Proxy: Nicetube proxy";
                Button proxyButton1 = uIBuilder.Button(in text);
                proxyButton1.LocalPressed += (IButton button, ButtonEventData eventData) =>
                {
                    proxyButton1.Slot.Parent.GetComponentsInChildren<Button>().ForEach(button => button.Enabled = false);
                    proxyButton1.LabelText = "Importing...";

                    try
                    {
                        TryImport(__instance, proxyButton1);
                    }
                    catch (Exception ex)
                    {
                        proxyButton1.LabelText = $"<alpha=red>Failed to import video. Check logs.";
                        Error(ex);
                    }
                };

                text = "Video Proxy: Import as Audio Clip\n<size=50%>(Not localized, convert to wav/orbis/etc to localize)";
                Button proxyButton2 = uIBuilder.Button(in text);
                proxyButton2.LocalPressed += (IButton button, ButtonEventData eventData) =>
                {
                    proxyButton2.Slot.Parent.GetComponentsInChildren<Button>().ForEach(button => button.Enabled = false);
                    proxyButton2.LabelText = "Importing...";

                    try
                    {
                        ImportAsAudioClip(__instance, proxyButton2);
                    }
                    catch (Exception ex)
                    {
                        proxyButton2.LabelText = $"<alpha=red>Failed to import video. Check logs.";
                        Error(ex);
                    }
                };
            }
        }

        private static void ImportAsAudioClip(VideoImportDialog __instance, Button proxyButton) {
            float3 b = float3.Zero;
            float num = __instance.LocalUserRoot?.GlobalScale ?? 1f;

            __instance.Paths.ForEach((item) =>
            {
                Slot slot = __instance.LocalUserSpace.AddSlot("Video Proxy(Local Import)");
                float3 a = __instance.Slot.GlobalPosition;
                slot.GlobalPosition = a + b;
                slot.GlobalRotation = __instance.Slot.GlobalRotation;
                a = float3.One;
                slot.GlobalScale = num * a;
                a = __instance.Slot.Right;
                b += a;

                UniversalImporter.UndoableImport(slot, async () =>
                {
                    string id = null;
                    try
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            Msg($"Attempting to process video URL: {item.filePath.ToString()}");
                            Match match = Regex.Match(item.filePath.ToString(), youtubeIdRegex);

                            if (match.Success)
                            {
                                id = match.Groups[1].Value;
                            }
                            else return Result.Failure("Failed to get video ID from URL");

                            string baseUri = null;
                            List<string> path = new List<string>();
                            switch (config.GetValue(PROXY_LOCATION))
                            {
                                default:
                                case ProxyLocation.CUSTOM:
                                    baseUri = config.GetValue(PROXY_URI).Trim();
                                    break;

                                case ProxyLocation.Australia:
                                    baseUri = "https://ntau1.sveken.com";
                                    break;

                                case ProxyLocation.NorthAmerica:
                                    baseUri = "https://ntna1.sveken.com";
                                    break;
                            }

                            UriBuilder builder = new UriBuilder(baseUri);

                            path.Add("reso");
                            path.Add("oggvorbis");
                            path.Add(id);
                            builder.Path = string.Join("/", path);

                            Msg($"Attempting to fetch URL: {builder.Uri.ToString()}");

                            HttpResponseMessage response = await client.GetAsync(builder.Uri);
                            string content = await response.Content.ReadAsStringAsync();

                            if (!content.ToLower().StartsWith("error:") && response.IsSuccessStatusCode)
                            {
                                Msg($"Importing as Audio Clip: {builder.Uri}");
                                float3 b = float3.Zero;
                                float num = proxyButton.LocalUserRoot?.GlobalScale ?? 1f;
                                await default(ToWorld);
                                Slot slot = proxyButton.LocalUserSpace.AddSlot("Video Proxy");
                                float3 a = proxyButton.Slot.GlobalPosition;
                                slot.GlobalPosition = a + b;
                                slot.GlobalRotation = proxyButton.Slot.GlobalRotation;
                                a = float3.One;
                                slot.GlobalScale = num * a;
                                a = proxyButton.Slot.Right;
                                b += a;

                                AudioPlayerInterface audioPlayer = await slot.SpawnEntity<AudioPlayerInterface, LegacyAudioPlayer>(FavoriteEntity.AudioPlayer);
                                audioPlayer.InitializeEntity(System.IO.Path.GetFileName(content));
                                audioPlayer.SetSource(new Uri(content));
                                audioPlayer.SetType(AudioTypeGroup.Multimedia, false, 0f, 0f);

                                __instance.Slot.Destroy();
                                return Result.Success();
                            }
                            else
                            {
                                string error = $"Failed fetching video URL: ({response.StatusCode}) {content}";
                                proxyButton.LabelText = $"<alpha=red>{error}";
                                return Result.Failure(error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return Result.Failure(ex.Message);
                    }
                });
            });
        }

        private static void TryImport(VideoImportDialog __instance, Button proxyButton)
        {
            float3 b = float3.Zero;
            float num = __instance.LocalUserRoot?.GlobalScale ?? 1f;

            __instance.Paths.ForEach((item) =>
            {
                Slot slot = __instance.LocalUserSpace.AddSlot("Video Proxy");
                float3 a = __instance.Slot.GlobalPosition;
                slot.GlobalPosition = a + b;
                slot.GlobalRotation = __instance.Slot.GlobalRotation;
                a = float3.One;
                slot.GlobalScale = num * a;
                a = __instance.Slot.Right;
                b += a;
                UniversalImporter.UndoableImport(slot, async () => await CustomImportAsync(__instance.Slot, slot, item, proxyButton));
            });
        }

        private static async Task<Result> CustomImportAsync(Slot __instance, Slot slot, ImportItem item, Button proxyButton)
        {
            Uri uri = null;
            string id = null;
            try {
                using (HttpClient client = new HttpClient())
                {
                    Msg($"Attempting to process video URL: {item.filePath.ToString()}");
                    Match match = Regex.Match(item.filePath.ToString(), youtubeIdRegex);

                    if (match.Success)
                    {
                        id = match.Groups[1].Value;
                    } 
                    else 
                    {
                        return Result.Failure("Failed to get video ID from URL");
                    }

                    string baseUri = null;
                    List<string> path = new List<string>();
                    switch (config.GetValue(PROXY_LOCATION))
                    {
                        default:
                        case ProxyLocation.CUSTOM:
                            baseUri = config.GetValue(PROXY_URI).Trim();
                            break;

                        case ProxyLocation.Australia:
                            baseUri = "https://ntau1.sveken.com";
                            break;

                        case ProxyLocation.NorthAmerica:
                            baseUri = "https://ntna1.sveken.com";
                            break;
                    }

                    UriBuilder builder = new UriBuilder(baseUri);

                    path.Add("reso");

                    switch (config.GetValue(RESOLUTION))
                    {
                        case Resolution.Q480P:
                            if (config.GetValue(FORCED)) path.Add("Q480Ph264Forced");
                            else path.Add("Q480P");
                            break;

                        case Resolution.Q720P:
                            if (config.GetValue(FORCED)) path.Add("Q720Ph264Forced");
                            else path.Add("Q720P");
                            break;

                        case Resolution.Q1080P:
                            if (config.GetValue(FORCED)) path.Add("Q1080Ph264Forced");
                            else path.Add("Q1080P");
                            break;

                        case Resolution.Q1440P:
                            path.Add("Q1440P");
                            break;


                        case Resolution.Q2160P:
                            path.Add("Q2160P");
                            break;

                        case Resolution.QBest:
                            path.Add("QPoggers");
                            break;

                        default:
                            path.Add("Q720P");
                            break;
                    }

                    path.Add(id);

                    builder.Path = string.Join("/", path);

                    Msg($"Attempting to fetch URL: {builder.Uri.ToString()}");

                    HttpResponseMessage response = await client.GetAsync(builder.Uri);
                    string content = await response.Content.ReadAsStringAsync();

                    if (!content.ToLower().StartsWith("error:") && response.IsSuccessStatusCode)
                    {
                        uri = new Uri(await response.Content.ReadAsStringAsync());
                        Msg($"Successfully fetched video URL: {uri.ToString()}");
                        VideoPlayerInterface videoInterface = await slot.SpawnEntity<VideoPlayerInterface, LegacyVideoPlayer>(FavoriteEntity.VideoPlayer);
                        videoInterface.InitializeEntity(item.itemName);
                        slot = videoInterface.Slot.GetObjectRoot();
                        videoInterface.SetSource(uri, true);
                        slot.Name = $"Video Proxy: {uri}";

                        __instance?.Destroy();
                        return Result.Success();
                    }
                    else
                    {
                        string error = $"Failed fetching video URL: ({response.StatusCode}) {content}";
                        proxyButton.LabelText = $"<alpha=red>{error}";
                        return Result.Failure(error);
                    }
                }
            }                
            catch (Exception ex) 
            {
                return Result.Failure(ex.Message);
            }
        }
    }
}
