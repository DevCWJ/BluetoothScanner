using CWJ;
using CWJ.Serializable;

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

    public DeviceCache(string id, string name, bool isConnectable)
    {
        this.id = id;
        this.name = name;
        this.isConnectable = isConnectable;
    }

    public bool CheckIsVerifiedAndConnectable()
    {
        return name.Length > 0 && isConnectable;
    }
}

public class BleScanner : DisposableMonoBehaviour
{
    public DictionaryVisualized<string, DeviceCache> deviceCacheById = new DictionaryVisualized<string, DeviceCache>();

    public UnityEvent<DeviceCache> deviceUpdateEvent = new UnityEvent<DeviceCache>();


    HashSet<string> targetNames;
    bool hasTarget;
    bool isTargetUntilConnectable = false;
    bool isLoopScan = false;
    [SerializeField, Readonly] UnityEvent<string> targetDeviceEvent = new UnityEvent<string>();


    //public void SetTargetBleName(string[] names, bool isTargetUntilConnectable, UnityAction<string> targetDeviceAction = null)
    //{
    //    if (names.LengthSafe() == 0)
    //    {
    //        return;
    //    }
    //    hasTarget = true;
    //    this.isTargetUntilConnectable = isTargetUntilConnectable;

    //    targetNames = new HashSet<string>(names);
    //    targetDeviceEvent = new UnityEvent<string>();
    //    if (targetDeviceAction != null)
    //        targetDeviceEvent.AddListener(targetDeviceAction);

    //    StartScan();
    //}

    public void StartScan(bool isLoopScan = false)
    {
        this.isLoopScan = isLoopScan;

        Debug.Log($"[Start Scan]");

        CO_ScanBle = StartCoroutine(DO_ScanBle());
    }

    public void StopScan()
    {
        isLoopScan = false;

        if (CO_ScanBle != null)
        {
            StopCoroutine(CO_ScanBle);
            BleApi.StopDeviceScan();
            Debug.Log($"[Stop Scan]");
            Debug.Log("----------------------------------");
            CO_ScanBle = null;
        }

        deviceCacheById.Clear();
    }
    string lastError;

    Coroutine CO_ScanBle = null;
    IEnumerator DO_ScanBle()
    {
        BleApi.StartDeviceScan();

        bool isScanFinished = false;
        do
        {
            yield return null;
            yield return null;

            if (isScanFinished)
            {
                continue;
            }

            BleApi.ScanStatus status;
            var device = new BleApi.DeviceUpdate();
            do
            {
                status = BleApi.PollDevice(ref device, false);

                if (status == BleApi.ScanStatus.FINISHED)
                {
                    isScanFinished = true;
                    break;
                }

                if (device.id.Length == 0)
                {
                    continue;
                }

                bool isUpdated = false;

                if (!deviceCacheById.TryGetValue(device.id, out var deviceCache))
                {
                    deviceCacheById.Add(device.id, deviceCache = new DeviceCache(device.id, device.name ?? string.Empty, device.isConnectable));
                    string newDeviceName = deviceCache.name;
                    if (newDeviceName.Length > 0)
                    {
                        isUpdated = true;
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
                        isUpdated = true;
                        Debug.LogWarning($"[ Updated ] name: '{deviceCache.name}'->'{device.name}'\nid: '{device.id}'");
                        deviceCache.name = device.name;
                    }

                    if (device.isConnectableUpdated && deviceCache.isConnectable != device.isConnectable)
                    {
                        isUpdated = true;
                        if (deviceCache.isConnectable)
                            Debug.LogWarning($"[ Updated ] name: '{deviceCache.name}' {deviceCache.isConnectable}->{device.isConnectable}'\nid: '{device.id}'");

                        deviceCache.isConnectable = device.isConnectable;
                        //Debug.LogWarning($"[ Updated ] name: '{deviceCache.name}'\nisConnectable : {deviceCache.isConnectable}->{device.isConnectable}\nid: '{device.id}'");
                    }

                    if (isUpdated)
                    {
                        deviceCacheById[deviceCache.id] = deviceCache;
                    }
                }

                if (isUpdated)
                {
                    deviceUpdateEvent.Invoke(deviceCache);
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

        } while (isLoopScan || (hasTarget && targetNames.Count > 0));

        StopScan();
    }

    protected override void OnDispose()
    {
        StopScan();
        BleApi.Quit();
    }
}
