using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.MixedReality.OpenXR.Remoting;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class GpuKiller : MonoBehaviour
{
    private readonly List<Texture2D> _aliveTextures = new();
    private readonly object HolographicRemoting;
    private int resWidth = 3840;
    private int resHeight = 2160;
    [SerializeField]
    private RemotingConnectConfiguration _config;
    [SerializeField]
    private Camera cam;

    private string _ipAddr = "127.0.0.1";
    [SerializeField]
    private GameObject imagePrefab;
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(50, 50, 50 + Screen.width/2f, 50 + Screen.height /2f));
        GUILayout.BeginVertical();

        var t = GUILayout.Button("Leak a small texture");
        if (t)
        {
            LeakTexture();
        }

        t = GUILayout.Button("Leak a large texture");
        if (t)
        {
            LeakTexture(16);
        }

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        _ipAddr = GUILayout.TextField(_ipAddr, 20);
        var connect = GUILayout.Button("Connect");
        if (connect)
        {
            HolographicRemotingConnect(_ipAddr);
        }

        connect = GUILayout.Button("Disconnect");
        if (connect)
        {
            HolographicRemoteDisconnect();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void OnDestroy()
    {
        foreach (var t in _aliveTextures)
        {
            Destroy(t);
        }
    }

    private void LeakTexture(int mult = 4)
    {
        resWidth = 1024 * mult;
        resHeight = 1024 * mult;
        TakeCameraRender(destroyCancellationToken).Forget();
    }

    private void TakeScreenshot()
    {
        TakeCameraRender(destroyCancellationToken).Forget();
    }

    private async UniTaskVoid TakeCameraRender(CancellationToken cancelToken)
    {
        await UniTask.WaitForEndOfFrame(this, cancelToken);
        var oldTex = cam.targetTexture;

        // resize the original image:
        var resizeRT = cam.targetTexture = RenderTexture.GetTemporary(resWidth, resHeight, 32);
        cam.Render();
        cam.targetTexture = oldTex;

        // create a native array to receive data from the GPU:
        var nArray = new NativeArray<byte>(resWidth * resHeight * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        // request the texture data back from the GPU:
        var request = AsyncGPUReadback.RequestIntoNativeArray(ref nArray, resizeRT, 0, async (AsyncGPUReadbackRequest request) =>
        {
            WriteToFile(nArray, resizeRT, request).Forget();
        });

        var vis = Instantiate(imagePrefab, transform);
        var pos = Camera.main.transform.position;
        vis.transform.position = pos + Camera.main.transform.forward * 3f;
        vis.transform.LookAt(pos, Vector3.up);

        var rend = vis.GetComponentInChildren<Renderer>();
        rend.material.mainTexture = resizeRT;
    }

    private async UniTaskVoid WriteToFile(NativeArray<byte> narray, RenderTexture resizeRT, AsyncGPUReadbackRequest request)
    {
        // if the readback was successful, encode and write the results to disk
        if (!request.hasError)
        {
            var colorFormat = resizeRT.graphicsFormat;
            var loc = Application.persistentDataPath;
            await UniTask.SwitchToThreadPool();
            var encoded = ImageConversion.EncodeNativeArrayToPNG(narray, colorFormat, (uint)resWidth, (uint)resHeight);

            var folder = loc;
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            var fileName = Path.Combine(folder, $"Snapshot_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.png");

            //await File.WriteAllBytesAsync(fileName, encoded.ToArray());
            await UniTask.SwitchToMainThread();
            encoded.Dispose();
        }

        narray.Dispose();
    }

    private void HolographicRemotingConnect(string ip)
    {
        _config.RemoteHostName = ip;
        AppRemoting.StartConnectingToPlayer(_config);
    }

    private void HolographicRemoteDisconnect()
    {
        AppRemoting.Disconnect();
    }
}
