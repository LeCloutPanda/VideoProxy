using FrooxEngine.UIX;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Threading.Tasks;
using SkyFrost.Base;
using Elements.Core;
using System.Net.Http;
using System.Text;

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
        AU,
        NA,
        CUSTOM
    }

    public override string Author => "LeCloutPanda & Sveken";
    public override string Name => "Video Proxy";
    public override string Version => "0.0.1";

    public static ModConfiguration config;
    [AutoRegisterConfigKey] private static ModConfigurationKey<bool> ENABLED = new ModConfigurationKey<bool>("enabledToggle", "Whether or not to generate custom import button", () => true);
    [AutoRegisterConfigKey] private static ModConfigurationKey<ProxyLocation> PROXY_LOCATION = new ModConfigurationKey<ProxyLocation>("serverRegion", "Proxy Server Region", () => ProxyLocation.AU);
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
                UIBuilder uIBuilder9 = ui;
                LocaleString text = "YouTube Proxy";
                Button proxyButton = uIBuilder9.Button(in text);
                proxyButton.LocalPressed += (IButton button, ButtonEventData eventData) =>
                {
                    TryImport(__instance);
                };
            }
        }

        private static void TryImport(VideoImportDialog __instance)
        {
            float3 b = float3.Zero;
            float num = __instance.LocalUserRoot?.GlobalScale ?? 1f;
            foreach (ImportItem item in __instance.Paths)
            {
                Slot s = __instance.LocalUserSpace.AddSlot("Video Proxy");
                Slot slot = s;
                float3 a = __instance.Slot.GlobalPosition;
                slot.GlobalPosition = a + b;
                s.GlobalRotation = __instance.Slot.GlobalRotation;
                Slot slot2 = s;
                a = float3.One;
                slot2.GlobalScale = num * a;
                a = __instance.Slot.Right;
                b += a;
                UniversalImporter.UndoableImport(s, async () => await CustomImportAsync(s, item));
            }
            __instance.Slot.Destroy();

        }
        private static async Task<Result> CustomImportAsync(Slot slot, ImportItem item)
        {
            Uri uri = null;

            TextRenderer textRenderer = slot.AttachComponent<TextRenderer>();
            textRenderer.Text.Value = $"Loading video through proxy...";
            textRenderer.Bounded.Value = true;
            textRenderer.Size.Value = 0.5f;

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    switch (config.GetValue(PROXY_LOCATION))
                    {
                        default:
                        case ProxyLocation.CUSTOM:
                            stringBuilder.Append(config.GetValue(PROXY_URI));
                            break;

                        case ProxyLocation.AU:
                            stringBuilder.Append("https://ntau1.sveken.com");
                            break;

                        case ProxyLocation.NA:
                            stringBuilder.Append(config.GetValue(PROXY_URI));
                            break;
                    }

                    if (config.GetValue(PROXY_URI).ToString().EndsWith("/")) stringBuilder.Append("reso/");
                    else stringBuilder.Append("/reso/");

                    switch (config.GetValue(RESOLUTION))
                    {
                        case Resolution.Q480P:
                            stringBuilder.Append("Q480P");
                            if (config.GetValue(FORCED)) stringBuilder.Append("h264Forced");
                            break;

                        case Resolution.Q720P:
                            stringBuilder.Append("Q720P");
                            if (config.GetValue(FORCED)) stringBuilder.Append("h264Forced");
                            break;

                        case Resolution.Q1080P:
                            stringBuilder.Append("Q1080P");
                            if (config.GetValue(FORCED)) stringBuilder.Append("h264Forced");
                            break;

                        case Resolution.Q1440P:
                            stringBuilder.Append("Q1440P");
                            break;


                        case Resolution.Q2160P:
                            stringBuilder.Append("Q2160P");
                            break;

                        case Resolution.QBest:
                            stringBuilder.Append("QPoggers");
                            break;

                        default:
                            stringBuilder.Append("Q720P");
                            break;
                    }

                    string videoID = item.filePath.Replace("https://www.youtube.com/watch?v=", "").Replace("https://www.youtube.com/shorts/", "");
                    stringBuilder.Append($"/{videoID}");

                    Msg($"Attempting to load url: {stringBuilder.ToString()}");

                    HttpResponseMessage response = await client.GetAsync(new Uri(stringBuilder.ToString()));
                    string content = await response.Content.ReadAsStringAsync();

                    if (!content.StartsWith("error:") && response.IsSuccessStatusCode)
                    {
                        uri = new Uri(await response.Content.ReadAsStringAsync());
                        _ = slot.Engine;
                        VideoPlayerInterface videoInterface = await slot.SpawnEntity<VideoPlayerInterface, LegacyVideoPlayer>(FavoriteEntity.VideoPlayer);
                        videoInterface.InitializeEntity(item.itemName);
                        slot = videoInterface.Slot.GetObjectRoot();
                        videoInterface.SetSource(uri, true);
                        slot.Name = $"Video Proxy: {uri}";
                        Msg($"Successfully found and loaded: {stringBuilder.ToString()}");
                        return Result.Success();
                    }
                    else
                    {
                        string error = $"Failed to load video: ({response.StatusCode}) {content}";

                        if (textRenderer != null)
                        {
                            textRenderer.Text.Value = "<color=red>" + error;
                            textRenderer.SetupBoxCollider();
                            slot.AttachComponent<Grabbable>();
                        }
                        Error(error);
                        return Result.Failure(error);
                    }
                }
                catch (Exception ex)
                {
                    string error = $"Failed to load video: {ex.Message}";

                    if (textRenderer != null)
                    {
                        textRenderer.Text.Value = "<color=red>" + error;
                        //textRenderer.Size.Value = 0.25f;
                        //textRenderer.HorizontalAlign.Value = Elements.Assets.TextHorizontalAlignment.Left;
                        //textRenderer.BoundsSize.Value = new float2(4, 2);

                        textRenderer.SetupBoxCollider();
                        slot.AttachComponent<Grabbable>();
                    }
                    Error(error);
                    return Result.Failure(error);
                }
            }
        }
    }
}