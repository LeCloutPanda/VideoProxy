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
    public override string Version => "1.1.2";

    public static ModConfiguration config;
    [AutoRegisterConfigKey] private static ModConfigurationKey<bool> ENABLED = new ModConfigurationKey<bool>("enabledToggle", "Whether or not to generate custom import button", () => true);
    [AutoRegisterConfigKey] private static ModConfigurationKey<ProxyLocation> PROXY_LOCATION = new ModConfigurationKey<ProxyLocation>("serverRegion", "Proxy Server Region", () => ProxyLocation.NorthAmerica);
    [AutoRegisterConfigKey] private static ModConfigurationKey<string> PROXY_URI = new ModConfigurationKey<string>("serverAddress", "Proxy Server address", () => "http://127.0.0.1:8080/");
    [AutoRegisterConfigKey] private static ModConfigurationKey<Resolution> RESOLUTION = new ModConfigurationKey<Resolution>("resolutionPreset", "Quality Preset", () => Resolution.Q720P);
    [AutoRegisterConfigKey] private static ModConfigurationKey<dummy> RESOLUTION_WARNING = new ModConfigurationKey<dummy>("resolutionWarning", "<color=yellow>⚠</color> QBest will load the best quality it can so be careful when using this setting. <color=yellow>⚠</color>");
    [AutoRegisterConfigKey] private static ModConfigurationKey<bool> FORCED = new ModConfigurationKey<bool>("forceCodecToggle", "Force h264 Codec", () => false);
    [AutoRegisterConfigKey] private static ModConfigurationKey<dummy> CODEC_WARNING = new ModConfigurationKey<dummy>("codecWarning", "<color=yellow>⚠</color> Force h264 Codec only works for quality levels Q480P, Q720P, Q1080P <color=yellow>⚠</color>");

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
                if(__instance.Paths.Select(item => item.assetUri.Query.Contains("dummyParam=1")).Any()) return;

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

                    bool fail = false;

                    __instance.Paths = (await Task.WhenAll(__instance.Paths.Zip(videoIds, async (item, videoId) =>
                    {
                        if (string.IsNullOrEmpty(videoId)) return item;

                        var newUri = await GetProxyUri(videoId);
                        if (newUri != null)
                        {
                            return new ImportItem(newUri, item.itemName);
                        }

                        proxyButton.LabelText = "<alpha=red>Error! Check log for details.";
                        fail = true;
                        return item;
                    }))).ToList();

                    // normally it swaps to the next import step (which is the same stuff but without the "Import Proxy" button)
                    // but if it fails we let the user see the error message and pick something else.
                    if (fail) return;

                    // Idk why we need a delay specifically because MonkeyLoader but we need one so /shrug
                    __instance.RunInUpdates(3, () =>
                    {
                        // hmm yes reflection is fun
                        AccessTools.Method(typeof(ImportDialog), "Open")
                            .Invoke(__instance, new object[] {
                                (Action<UIBuilder>)AccessTools.Method(typeof(VideoImportDialog),
                                    "OpenRoot",
                                    new Type[] { typeof(UIBuilder) }).CreateDelegate(typeof(Action<UIBuilder>),
                                    __instance
                                )
                                }
                            );
                    });
                };
            }
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
                            if (config.GetValue(FORCED)) path.Add("Q480Ph264Forced");
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

                    if (!content.StartsWith("error:") && response.IsSuccessStatusCode)
                    {
                        uri = new Uri(await response.Content.ReadAsStringAsync() + "?dummyParam=1");
                        Msg($"Successfully found and loaded: {uri.ToString()}");
                        return uri;
                    }
                    else
                    {
                        string error = $"Failed to load video: ({response.StatusCode}) {content}";
                        Error(error);
                        return null;
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
