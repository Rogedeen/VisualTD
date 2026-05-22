using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

public class FrameReceiver : MonoBehaviour
{
    public static FrameReceiver Instance;

    [SerializeField] private int port = 5053;
    
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning;

    private Texture2D remoteTexture;
    private byte[] lastFrameBytes;
    private bool hasNewFrame;
    private object lockObj = new object();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 640x360 (480p dengeli) olarak ayarlandı, yüksek detay sağlar
        remoteTexture = new Texture2D(640, 360, TextureFormat.RGB24, false);
    }

    public void StartReceiving()
    {
        if (isRunning) return;
        isRunning = true;
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveData()
    {
        udpClient = new UdpClient();
        udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));

        while (isRunning)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref anyIP);

                lock (lockObj)
                {
                    lastFrameBytes = data;
                    hasNewFrame = true;
                }
            }
            catch (Exception ex)
            {
                if (isRunning) Debug.LogWarning("[FrameReceiver] Receive error: " + ex.Message);
            }
        }
    }

    public void UpdateTexture(Image targetImage)
    {
        if (targetImage == null) return;

        lock (lockObj)
        {
            if (hasNewFrame && lastFrameBytes != null)
            {
                remoteTexture.LoadImage(lastFrameBytes);
                targetImage.image = remoteTexture;
                hasNewFrame = false;
            }
        }
    }

    private void OnDisable()
    {
        isRunning = false;
        if (udpClient != null) udpClient.Close();
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Join(500);
    }

    private void OnApplicationQuit() => OnDisable();
}
