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
using System.Linq.Expressions;
using FrooxEngine.Store;

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
    public override string Version => "1.2.0";
    public override string Link => "https://github.com/LeCloutPanda/VideoProxy";

    public static ModConfiguration config;
    [AutoRegisterConfigKey] private static ModConfigurationKey<bool> ENABLED = new ModConfigurationKey<bool>("enabledToggle", "Generate option to use VideoProxy when importing a video", () => true);
    [AutoRegisterConfigKey] private static ModConfigurationKey<ProxyLocation> PROXY_LOCATION = new ModConfigurationKey<ProxyLocation>("serverRegion", "Proxy Server Region", () => ProxyLocation.NorthAmerica);
    [AutoRegisterConfigKey] private static ModConfigurationKey<string> PROXY_URI = new ModConfigurationKey<string>("serverAddress", "Proxy Server address(Used when ServerRegion is set to CUSTOM)", () => "http://127.0.0.1:8080/");
    [AutoRegisterConfigKey] private static ModConfigurationKey<Resolution> RESOLUTION = new ModConfigurationKey<Resolution>("resolutionPreset", "Quality Preset: <color=yellow>⚠</color> QBest will load the best quality it can so be careful when using this setting <color=yellow>⚠</color>", () => Resolution.Q720P);
    [AutoRegisterConfigKey] private static ModConfigurationKey<bool> FORCED = new ModConfigurationKey<bool>("forceCodecToggle", "Force h264 Codec: <color=yellow>⚠</color> Force h264 Codec only works for quality levels Q480P, Q720P, Q1080P <color=yellow>⚠</color>", () => false);
    [AutoRegisterConfigKey] private static ModConfigurationKey<string> DOWNLOAD_FOLDER = new ModConfigurationKey<string>("downloadFolder", "Folder which is used to download and import videos", () => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Resonite"));

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
                LocaleString text = "YouTube Proxy";
                Button proxyButton1 = uIBuilder.Button(in text);
                proxyButton1.LocalPressed += async (IButton button, ButtonEventData eventData) =>
                {
                    // Disable all other buttons so you can't break the import process
                    proxyButton1.Slot.Parent.GetComponentsInChildren<Button>().ForEach(button => button.Enabled = false);
                    proxyButton1.LabelText = "Importing...";
                    
                    try {
                        TryImport(__instance, proxyButton1);
                    } catch (Exception ex) {
                        proxyButton1.LabelText = $"<alpha=red>Failed to import video. Check logs.";
                        Error(ex);
                    }
                };

                text = "YouTube Proxy(Download & Import)";
                Button proxyButton2 = uIBuilder.Button(in text);
                proxyButton2.LocalPressed += async (IButton button, ButtonEventData eventData) =>
                {
                    // Disable all other buttons so you can't break the import process
                    proxyButton2.Slot.Parent.GetComponentsInChildren<Button>().ForEach(button => button.Enabled = false);
                    proxyButton2.LabelText = "Importing...";
                    
                    try {
                        TryLocalImport(__instance, proxyButton2);
                    } catch (Exception ex) {
                        proxyButton2.LabelText = $"<alpha=red>Failed to import video. Check logs.";
                        Error(ex);
                    }
                };
            }
        }

        private static void TryLocalImport(VideoImportDialog __instance, Button proxyButton) {
            float3 b = float3.Zero;
            float num = __instance.LocalUserRoot?.GlobalScale ?? 1f;

            __instance.Paths.ForEach(async (item) => {
                Slot slot = __instance.LocalUserSpace.AddSlot("Video Proxy(Local Import)");
                float3 a = __instance.Slot.GlobalPosition;
                slot.GlobalPosition = a + b;
                slot.GlobalRotation = __instance.Slot.GlobalRotation;
                a = float3.One;
                slot.GlobalScale = num * a;
                a = __instance.Slot.Right;
                b += a;
                
                UniversalImporter.UndoableImport(slot, async () => await DownloadFileAndImport(__instance.Slot, slot, item, config.GetValue(DOWNLOAD_FOLDER)));
            });
        }

        public static async Task<Result> DownloadFileAndImport(Slot __instance, Slot slot, ImportItem item, string downloadPath)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    //HttpResponseMessage response = await client.GetAsync(item.filePath);
                    //response.EnsureSuccessStatusCode();
                    //byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
                    //File.WriteAllBytes(downloadPath, fileBytes);
                    //downloadPath = Path.Combine(downloadPath, Path.GetFileName(item.filePath));
                    //Msg(downloadPath);

                    //await default(ToBackground);
                    //string text = await __instance.Engine.AssetManager.GatherAssetFile(new Uri(item.filePath), 100f);
                    //await default(ToWorld);

                    if (true) { // text != null
                        await default(ToBackground);
                        Uri uri = await slot.World.Engine.LocalDB.ImportLocalAssetAsync("C:/Users/lucas/Desktop/image.png", LocalDB.ImportLocation.Original).ConfigureAwait(continueOnCapturedContext: false);
                        await default(ToWorld);
                        VideoPlayerInterface videoInterface = await slot.SpawnEntity<VideoPlayerInterface, LegacyVideoPlayer>(FavoriteEntity.VideoPlayer);
                        videoInterface.InitializeEntity(item.itemName);
                        slot = videoInterface.Slot.GetObjectRoot();
                        videoInterface.SetSource(uri, true);
                        slot.Name = $"Video Proxy(Local Import): {uri}";
                        __instance?.Destroy();
                        return Result.Success();
                    } else return Result.Failure("Failed to gather video from url.");          
                }
                catch (Exception ex)
                {
                    Error($"An error occurred: {ex.Message}");
                    return Result.Failure(ex.Message);
                }
            }
        }

/*
		StartTask(async delegate
		{
			Uri url = base.Slot.GetComponent<StaticBinary>()?.URL.Value;
			if (!(url == null))
			{
				IsProcessing.Value = true;
				await default(ToBackground);
				string text = await base.Engine.AssetManager.GatherAssetFile(url, 100f);
				if (text != null)
				{
					base.Engine.PlatformInterface.NotifyOfFile(text, Filename);
					await default(ToWorld);
					IsProcessing.Value = false;
				}
				else
				{
					_exported = false;
				}
			}
		})
*/
        private static void TryImport(VideoImportDialog __instance, Button proxyButton)
        {
            float3 b = float3.Zero;
            float num = __instance.LocalUserRoot?.GlobalScale ?? 1f;

            __instance.Paths.ForEach(async (item) => {
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
                    string youtubeIdRegex = "(?:youtube\\.com\\/(?:[^\\/]+\\/.+\\/|(?:v|e(?:mbed)?|shorts)\\/|.*[?&]v=)|youtu\\.be\\/)([^\"&?\\/\\s]{11})";
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
