using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;

[Serializable]
public class GestureData
{
    public string gesture;
}

public class UDPReceiver : MonoBehaviour
{
    [Header("Network Settings")]
    [SerializeField] private int listenPort = 5052;
    
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning;
    
    // ConcurrentQueue is thread-safe and allows passing data to the Main Thread safely.
    public ConcurrentQueue<GestureData> gestureQueue = new ConcurrentQueue<GestureData>();

    void Start()
    {
        StartReceiving();
    }

    private void StartReceiving()
    {
        isRunning = true;
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log($"[UDPReceiver] Started listening on UDP port {listenPort}");
    }

    private void ReceiveData()
    {
        udpClient = new UdpClient(listenPort);
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);

        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);
                
                // Parse JSON Data
                GestureData parsedData = JsonUtility.FromJson<GestureData>(text);
                if (parsedData != null && !string.IsNullOrEmpty(parsedData.gesture))
                {
                    gestureQueue.Enqueue(parsedData);
                }
            }
            catch (Exception err)
            {
                if (isRunning) 
                    Debug.LogError($"[UDPReceiver] Exception: {err.ToString()}");
            }
        }
    }

    void OnDisable()
    {
        isRunning = false;
        
        // Closing the client will force udpClient.Receive to throw an exception
        // which gracefully breaks the background thread loop.
        if (udpClient != null)
        {
            udpClient.Close();
        }
        
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(500);
        }
    }
    
    void OnApplicationQuit()
    {
        OnDisable();
    }
}
