
//using UnityEngine;
//using System.Runtime.InteropServices;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//public class Test_BleScanner : MonoBehaviour
//{
//    const string User32Dll = "user32.dll";
//    [DllImport(User32Dll)] private static extern IntPtr GetActiveWindow();

//    /// <summary>
//    /// //m
//    /// </summary>
//    /// <param name="hWnd"></param>
//    /// <param name="nCmdShow"></param>
//    /// <returns></returns>
//    [DllImport(User32Dll)] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

//    const int SW_HIDE = 0;

//    public static void HideWindow()
//    {
//        var hwnd = GetActiveWindow();
//        ShowWindow(hwnd, SW_HIDE);
//    }

//    Dictionary<string, DeviceCache> deviceCacheById = new Dictionary<string, DeviceCache>();

//    private void Start()
//    {
//        if (!Application.isEditor)
//        {
//            HideWindow();
//        }
//    }
//    private void OnEnable()
//    {
//        BleApi.StartDeviceScan();
//        t = waitSecond;
//    }

//    private void OnDisable()
//    {
//        BleApi.StopDeviceScan();
//    }

//    private void OnApplicationQuit()
//    {
//        BleApi.Quit();
//    }

//    [SerializeField] string[] targetNames;
//    public void ScanBluetoothName(string nameLine)
//    {

//    }

//    [SerializeField] int waitSecond = 5;
//    [SerializeField] int maxDeviceLength = 10;
//    float t = 0;
//    private void Update()
//    {
//        if (isLoading)
//        {
//            return;
//        }

//        if ((t += Time.deltaTime) < waitSecond)
//        {
//            return;
//        }

//        isLoading = true;
//        t = 0;
//        Test();
//        Debug.Log($" Waiting for {waitSecond} second.");
//    }

//    string lastError;
//    bool isLoading = false;
//    private void Test()
//    {
//        BleApi.ScanStatus status;

//        var device = new BleApi.DeviceUpdate();
//        int checkDevice = 0;
//        Debug.Log($"[ 스캔시작 ]" + $"(max device length:{maxDeviceLength}");

//        do
//        {
//            status = BleApi.PollDevice(ref device, false);

//            if (!deviceCacheById.TryGetValue(device.id, out var deviceCache))
//            {
//                deviceCacheById.Add(device.id, deviceCache = new DeviceCache(device.id, device.name ?? string.Empty, device.isConnectable));
//                if (deviceCache.name.Length > 0)
//                {
//                    Debug.LogWarning($"[   New   ] name: '{device.name}'\nid: '{device.id}'");
//                    deviceCache.SetDirty();
//                }

//            }
//            else
//            {
//                if (device.nameUpdated && device.name.Length > 0 && deviceCache.name != device.name)
//                {
//                    deviceCache.SetDirty();
//                    Debug.LogWarning($"[ Updated ] name: '{deviceCache.name}'->'{device.name}'\nid: '{device.id}'");
//                    deviceCache.name = device.name;
//                }
//                if (device.isConnectableUpdated && deviceCache.name.Length > 0 && deviceCache.isConnectable != device.isConnectable)
//                {
//                    deviceCache.SetDirty();
//                    Debug.LogWarning($"[ Updated ] name: '{deviceCache.name}'\nisConnectable : {deviceCache.isConnectable}->{device.isConnectable}\nid: '{device.id}'");
//                    deviceCache.isConnectable = device.isConnectable;
//                }
//            }

//            if (deviceCache.isDirty)
//            {
//                deviceCache.SolveDirty();
//                //이벤트 실행
//            }
//        } while (status == BleApi.ScanStatus.AVAILABLE && ++checkDevice <= maxDeviceLength);

//        Debug.Log($"[ 스캔종료 ]");
//        Debug.Log("----------------------------------");

//        // log potential errors
//        BleApi.GetError(out var errMsg);
//        if (!string.IsNullOrEmpty(errMsg.msg))
//        {
//            if (lastError != errMsg.msg)
//            {
//                Debug.LogError(errMsg.msg);
//                lastError = errMsg.msg;
//            }
//        }

//        isLoading = false;
//    }

//}
