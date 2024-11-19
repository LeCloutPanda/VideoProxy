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
    public enum DebugLevel
    {
        None,
        Min,
        Max
    }

    public enum Resolution
    {
        Q480P,
        Q720P,
        Q1080P,
        Q1440P,
        QBest
    }

    public enum ProxyLocation
    {
        AU,
        US,
        CUSTOM
    }

    public override string Author => "LeCloutPanda & Sveken";
    public override string Name => "Video Proxy";
    public override string Version => "0.0.1";

    public static ModConfiguration config;
    [AutoRegisterConfigKey] private static ModConfigurationKey<bool> ENABLED = new ModConfigurationKey<bool>("Whether or not to generate custom import button/s", "", () => true);
    [AutoRegisterConfigKey] private static ModConfigurationKey<ProxyLocation> PROXY_LOCATION = new ModConfigurationKey<ProxyLocation>("Proxy Server Region", "", () => ProxyLocation.AU);
    [AutoRegisterConfigKey] private static ModConfigurationKey<string> PROXY_URI = new ModConfigurationKey<string>("Proxy Server adress", "", () => "http://127.0.0.1:8080/video");
    [AutoRegisterConfigKey] private static ModConfigurationKey<DebugLevel> DEBUG_LEVEL = new ModConfigurationKey<DebugLevel>("Debug Level", "", () => DebugLevel.Min);
    [AutoRegisterConfigKey] private static ModConfigurationKey<Resolution> RESOLUTION = new ModConfigurationKey<Resolution>("Resolution", "", () => Resolution.Q1080P);
    [AutoRegisterConfigKey] private static ModConfigurationKey<bool> FORCED = new ModConfigurationKey<bool>("Force h264 Codec", "", () => true);

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

            if (config.GetValue(DEBUG_LEVEL) != DebugLevel.None)
            {
                TextRenderer textRenderer = slot.AttachComponent<TextRenderer>();
                switch (config.GetValue(DEBUG_LEVEL))
                {
                    case DebugLevel.Min:
                        textRenderer.Text.Value = $"Loading video through proxy...";
                        break;

                    default:
                        textRenderer.Text.Value = $"Loading video <color=green>{item.filePath}</color> through Video Proxy at <color=green>{config.GetValue(PROXY_URI)}</color>";
                        break;
                }
                textRenderer.Bounded.Value = true;
                textRenderer.Size.Value = 0.5f;
            }

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    switch (config.GetValue(PROXY_LOCATION))
                    {
                        default:
                            stringBuilder.Append(config.GetValue(PROXY_URI));
                            break;
                    }

                    switch (config.GetValue(RESOLUTION))
                    {
                        case Resolution.Q480P:
                            stringBuilder.Append("Q480");
                            break;

                        case Resolution.Q720P:
                            stringBuilder.Append("Q720");
                            break;

                        default:
                        case Resolution.Q1080P:
                            stringBuilder.Append("Q1080");
                            break;

                        case Resolution.Q1440P:
                            stringBuilder.Append("Q1440");
                            break;

                        case Resolution.QBest:
                            stringBuilder.Append("QPoggers");
                            break;
                    }

                    if (config.GetValue(FORCED)) stringBuilder.Append("h264Forced");

                    string videoID = item.filePath.Replace("https://www.youtube.com/watch?v=", "").Replace("https://www.youtube.com/shorts/", "");
                    stringBuilder.Append($"/{videoID}");

                    HttpResponseMessage response = await client.GetAsync(new Uri(stringBuilder.ToString()));

                    if (response.IsSuccessStatusCode)
                    {
                        uri = new Uri(await response.Content.ReadAsStringAsync());
                        _ = slot.Engine;
                        VideoPlayerInterface videoInterface = await slot.SpawnEntity<VideoPlayerInterface, LegacyVideoPlayer>(FavoriteEntity.VideoPlayer);
                        videoInterface.InitializeEntity(item.itemName);
                        slot = videoInterface.Slot.GetObjectRoot();
                        videoInterface.SetSource(uri, !config.GetValue(FORCED));
                        slot.Name = $"Video Proxy: {uri}";
                        return Result.Success();
                    }
                    else
                    {
                        TextRenderer textRenderer = slot.GetComponent<TextRenderer>();
                        if (textRenderer != null)
                        {
                            textRenderer.Text.Value = $"<color=red>Error Code: {response.StatusCode}";
                            textRenderer.SetupBoxCollider();
                            slot.AttachComponent<Grabbable>();
                        }
                        return Result.Failure($"Error using video proxy: Status Code {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    TextRenderer textRenderer = slot.GetComponent<TextRenderer>();
                    if (textRenderer != null)
                    {
                        switch (config.GetValue(DEBUG_LEVEL))
                        {
                            default:
                            case DebugLevel.Min:
                                textRenderer.Text.Value = $"<color=red>Failed to load: {ex.Message}";
                                break;

                            case DebugLevel.Max:
                                textRenderer.Text.Value = $"<color=red>Failed to load: {ex.StackTrace}";
                                break;
                        }
                        textRenderer.SetupBoxCollider();
                        slot.AttachComponent<Grabbable>();
                    }
                    return Result.Failure($"Error using video proxy: {ex}");
                }
            }
        }
    }
}