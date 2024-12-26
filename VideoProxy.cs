using FrooxEngine.UIX;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Threading.Tasks;
using Elements.Core;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using SkyFrost.Base;
using System.Reflection;

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
    public override string Version => "1.2.0-b";
    public override string Link => "https://github.com/LeCloutPanda/VideoProxy";

    public static ModConfiguration config;
    [AutoRegisterConfigKey] private static ModConfigurationKey<bool> ENABLED = new ModConfigurationKey<bool>("enabledToggle", "Generate option to use VideoProxy when importing a video", () => true);
    [AutoRegisterConfigKey] private static ModConfigurationKey<ProxyLocation> PROXY_LOCATION = new ModConfigurationKey<ProxyLocation>("serverRegion", "Proxy Server Region", () => ProxyLocation.NorthAmerica);
    [AutoRegisterConfigKey] private static ModConfigurationKey<string> PROXY_URI = new ModConfigurationKey<string>("serverAddress", "Proxy Server address(Used when ServerRegion is set to CUSTOM)", () => "http://127.0.0.1:8080/");
    [AutoRegisterConfigKey] private static ModConfigurationKey<Resolution> RESOLUTION = new ModConfigurationKey<Resolution>("resolutionPreset", "Quality Preset: <color=yellow>⚠</color> QBest will load the best quality it can so be careful when using this setting <color=yellow>⚠</color>", () => Resolution.Q720P);
    [AutoRegisterConfigKey] private static ModConfigurationKey<bool> FORCED = new ModConfigurationKey<bool>("forceCodecToggle", "Force h264 Codec: <color=yellow>⚠</color> Force h264 Codec only works for quality levels Q480P, Q720P, Q1080P <color=yellow>⚠</color>", () => false);
    //[AutoRegisterConfigKey] private static ModConfigurationKey<bool> SHOW_ERROR_ON_IMPORT_DIALOG = new ModConfigurationKey<bool>("showErrorOnImportDialog", "Show Error on import dialog", () => true);

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
                UIBuilder uIBuilder9 = ui;
                LocaleString text = "YouTube Proxy";
                Button proxyButton = uIBuilder9.Button(in text);
                proxyButton.LocalPressed += async (IButton button, ButtonEventData eventData) =>
                {
                    string youtubeIdRegex = "(?:youtube\\.com\\/(?:[^\\/]+\\/.+\\/|(?:v|e(?:mbed)?|shorts)\\/|.*[?&]v=)|youtu\\.be\\/)([^\"&?\\/\\s]{11})";
                    IEnumerable<string> videoIds = __instance.Paths
                        .Select(item => (!(item.assetUri != null)) ? new Uri(item.filePath) : item.assetUri)
                        .Select(uri => Regex.Match(uri.ToString(), youtubeIdRegex).Groups[1].Value);

                    proxyButton.Enabled = false;
                    proxyButton.LabelText = "Importing...";
                    
                    try {
                        foreach(string videoId in videoIds) {
                            var newUri = await GetProxyUri(videoId);
                            if (newUri != null && !newUri.ToString().ToLower().StartsWith("error:"))
                            {
                                ImportBasicVideo(__instance, newUri);
                            } 
                            else 
                            {
                                proxyButton.LabelText = $"<alpha=red>{newUri.ToString().Replace("error:", "Error!")}";
                                Error(newUri);
                            }
                        }
                    } catch (Exception ex) {
                        proxyButton.LabelText = $"Failed to import video. Check logs.";
                        Error(ex);
                    }
                };
            }
        }

        public static void ImportBasicVideo(VideoImportDialog __instance, Uri uri)
        {
            float3 b = float3.Zero;
            float num = __instance.LocalUserRoot?.GlobalScale ?? 1f;

            Slot s = __instance.LocalUserSpace.AddSlot("Test");
            Slot slot = s;
            float3 a = __instance.Slot.GlobalPosition;
            slot.GlobalPosition = a + b;
            s.GlobalRotation = __instance.Slot.GlobalRotation;
            Slot slot2 = s;
            a = float3.One;
            slot2.GlobalScale = num * a;
            a = __instance.Slot.Right;
            b += a;
            UniversalImporter.UndoableImport(s, async () => await CustomImportAsync(s, uri));
            __instance.Slot.Destroy();
        }

        private static async Task<Result> CustomImportAsync(Slot slot, Uri uri)
        {
            _ = slot.Engine;
            VideoPlayerInterface videoInterface = await slot.SpawnEntity<VideoPlayerInterface, LegacyVideoPlayer>(FavoriteEntity.VideoPlayer);
            videoInterface.InitializeEntity("Test Entity");
            slot = videoInterface.Slot.GetObjectRoot();
            videoInterface.SetSource(uri, true);
            slot.Name = $"Video Proxy: {uri}";
            return Result.Success();
        }

        private static async Task<Uri> GetProxyUri(string videoId)
        {
            Uri uri = null;

            using (HttpClient client = new HttpClient())
            {
                try
                {
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

                    path.Add(videoId);

                    builder.Path = string.Join("/", path);

                    Msg($"Attempting to load url: {builder.Uri.ToString()}");

                    HttpResponseMessage response = await client.GetAsync(builder.Uri);
                    string content = await response.Content.ReadAsStringAsync();

                    if (!content.ToLower().StartsWith("error:") && response.IsSuccessStatusCode)
                    {
                        uri = new Uri(await response.Content.ReadAsStringAsync() + "?dummyParam=1");
                        Msg($"Successfully found and loaded: {uri.ToString()}");
                        return uri;
                    }
                    else
                    {
                        string error = $"Failed to load video: ({response.StatusCode}) {content}";
                        Error(error);
                        return new Uri(content);
                    }
                }
                catch (Exception ex)
                {
                    string error = $"Failed to load video: {ex.Message}";

                    Error(error);
                    return null;
                }
            }
        }
    }
}
