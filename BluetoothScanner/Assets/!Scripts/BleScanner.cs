using CWJ;

using System;
using System.Collections;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public class DeviceCache
{
    [Readonly] public string id;
    [Readonly] public string name;
    [Readonly] public bool isConnectable;
    public bool isDirty => _isDirty;
    [SerializeField, Readonly] bool _isDirty;

    public DeviceCache(string id, string name, bool isConnectable)
    {
        this.id = id;
        this.name = name;
        this.isConnectable = isConnectable;
        _isDirty = false;
    }


    public void SetDirty()
    {
        if (!_isDirty)
            _isDirty = true;
    }

    public void SolveDirty()
    {
        if (_isDirty)
            _isDirty = false;
    }

    public bool CheckIsVerifiedAndConnectable()
    {
        return name.Length > 0 && isConnectable;
    }
}

public class BleScanner : DisposableMonoBehaviour
{
    Dictionary<string, DeviceCache> deviceCacheById = new Dictionary<string, DeviceCache>();

    public UnityEvent<string, bool> deviceUpdateEvent = new UnityEvent<string, bool>();

    HashSet<string> targetNames;
    bool hasTarget;
    bool isTargetUntilConnectable = false;
    bool isLoopScan = false;
    [SerializeField, Readonly] UnityEvent<string> targetDeviceEvent = new UnityEvent<string>();


    public void SetTargetBleName(string[] names, bool isTargetUntilConnectable, UnityAction<string> targetDeviceAction = null)
    {
        if (names.LengthSafe() == 0)
        {
            return;
        }
        hasTarget = true;
        this.isTargetUntilConnectable = isTargetUntilConnectable;

        targetNames = new HashSet<string>(names);
        targetDeviceEvent = new UnityEvent<string>();
        if (targetDeviceAction != null)
            targetDeviceEvent.AddListener(targetDeviceAction);

        StartScan();
    }

    public void StartScan(bool isLoopScan = false)
    {
        StopScan();

        this.isLoopScan = isLoopScan;

        BleApi.StartDeviceScan();
        Debug.Log($"[Start Scan]");

        CO_ScanBle = StartCoroutine(DO_ScanBle());
    }

    public void StopScan()
    {
        isLoopScan = false;

        if (CO_ScanBle != null)
        {
            StopCoroutine(CO_ScanBle);
            Debug.Log($"[Stop Scan]");
            Debug.Log("----------------------------------");
            CO_ScanBle = null;
        }
        BleApi.StopDeviceScan();
        deviceCacheById.Clear();
    }
    string lastError;

    Coroutine CO_ScanBle = null;
    IEnumerator DO_ScanBle()
    {
        do
        {
            BleApi.ScanStatus status;

            do
            {
                var device = new BleApi.DeviceUpdate();
                status = BleApi.PollDevice(ref device, false);

                if (device.id.Length == 0)
                {
                    continue;
                }

                if (!deviceCacheById.TryGetValue(device.id, out var deviceCache))
                {
                    deviceCacheById.Add(device.id, deviceCache = new DeviceCache(device.id, device.name ?? string.Empty, device.isConnectable));
                    string newDeviceName = deviceCache.name;
                    if (newDeviceName.Length > 0)
                    {
                        deviceCache.SetDirty();
                        Debug.LogWarning($"[   New   ] name: '{device.name}'\nid: '{device.id}'\nconnectable: '{device.isConnectable}'");
                        if (hasTarget && !isTargetUntilConnectable && targetNames.Contains(newDeviceName))
                        {
                            targetNames.Remove(newDeviceName);
                            targetDeviceEvent?.Invoke(newDeviceName);
                        }
                    }
                }
                else
                {
                    if (device.nameUpdated && device.name.Length > 0 && !deviceCache.name.Equals(device.name))
                    {
                        deviceCache.SetDirty();
                        Debug.LogWarning($"[ Updated ] name: '{deviceCache.name}'->'{device.name}'\nid: '{device.id}'");
                        deviceCache.name = device.name;
                    }
                    // 현재 isConnectable가 이상하게 작동함. 꺼졌다 켜졌다 반복함.
                    //if (device.isConnectableUpdated && deviceCache.name.Length > 0 && !deviceCache.isConnectable.Equals(device.isConnectable))
                    //{
                    //    deviceCache.SetDirty();
                    //    Debug.LogWarning($"[ Updated ] name: '{deviceCache.name}'\nisConnectable : {deviceCache.isConnectable}->{device.isConnectable}\nid: '{device.id}'");
                    //    deviceCache.isConnectable = device.isConnectable;
                    //}
                }

                if (deviceCache.isDirty)
                {
                    deviceCache.SolveDirty();
                    deviceUpdateEvent.Invoke(deviceCache.name, deviceCache.CheckIsVerifiedAndConnectable());
                    if (hasTarget && isTargetUntilConnectable && targetNames.Contains(deviceCache.name))
                    {
                        targetNames.Remove(deviceCache.name);
                        targetDeviceEvent?.Invoke(deviceCache.name);
                    }
                }
                yield return null;
                yield return null;

            } while (status == BleApi.ScanStatus.AVAILABLE);

            // log potential errors
            BleApi.GetError(out var errMsg);
            if (!string.IsNullOrEmpty(errMsg.msg))
            {
                if (lastError != errMsg.msg)
                {
                    Debug.LogError(errMsg.msg);
                    lastError = errMsg.msg;
                }
            }
            yield return null;
            yield return null;
        } while (isLoopScan || (hasTarget && targetNames.Count > 0));

        StopScan();
    }

    protected override void OnDispose()
    {
        StopScan();
        BleApi.Quit();
    }
}
