# Resonite Youtube Video Proxy
This Resonite mod restores higher quality youtube content in game by using an external proxy to present the final video file to the game. This can also be used to get around geo blocking so everyone in world can see the video.

## Install
To install place the mod.dll from the release page inside your rml_mods folder. If you need information on modding resonite please visit the [Resonite Mod Loader page](https://github.com/resonite-modding-group/ResoniteModLoader). 

It is also highly reccomended to also have/install [ResoniteModSettings](https://github.com/badhaloninja/ResoniteModSettings) to configure the quality settings if the default does not work for you.

## How to use
Simply paste the Youtube link into the game like normal, the dialog box will now have an extra option called ```Youtube Proxy```. After proccessing the URL to the video file will be dropped in world as a video player. Depending on the quality set this may take a few moments. 

By default the quality is set to 720P using VP9 codec which is a noticable upgrade already to the default 360p. Higher qualities may take additional time for everyone in the world to load. 

### Quality settings available.

Quality settings are adjustable using [ResoniteModSettings](https://github.com/badhaloninja/ResoniteModSettings)

The Mod supports quality ranges  360, 480, 720, 1080 at 60fps if available otherwise 30fps, using VP9 by default with a failover to AV1. 

There is also a force H264 option available for 360, 480, 720, 1080. This will force the video codec to be in H264 which may help with performance in larger lobbies but may result in a larger download size. The Default 720P VP9 setting in testing has shown not to be cpu heavy and should be fine.

**Note** There is also QBest which will get the absolute best available version of the video. However this means versions such as AV1 4K 60fps may be acquired and this will **not end well** in most cases. 

## Proxy server
By default the mod will use a North American based server, There is also a preset option for an AU based one. 

### Custom Proxy Server
You can also host your own server. Information is available on the [nicetube repo here](https://github.com/sveken/nicetube) **Note:** The server will need to be public facing if you want none local users to be able to load the videos.

## Known issues. 
If a MP4 file is presented it may not load if ```Stream``` is enabled. Disabling this check on the video player will allow it to load. To get around this the default file presented should be webm.