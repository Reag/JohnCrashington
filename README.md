# JohnCrashington
A repo to show a particular crash in OpenXR with holographic remoting

# Steps to Reproduce Crash

1) Enter into holographic remoting
2) Leak textures into the scene until remoting drops in the device

# Key Observations
It appears the crash/stall is caused by leaking Unity RenderTextures of large size. 
This was tested with normal textures (Texture2D), and the crash did not occur.


Note that this crash does NOT occur on OpenXR's Holographic Remoting dll v2.7.5, but DOES on v2.8.0. The working version of this binary can be found in OpenXR 1.4.0.