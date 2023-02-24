using CWJ;
using CWJ.Serializable;

using System;
using System.Collections;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BleScanner : DisposableMonoBehaviour
{
    [Serializable]
    public class DeviceCache
    {
        public DeviceCache() { }

        public string id;
        public string name;
        public bool isConnectable;

        public void SetConnectable(bool _isConnectable)
        {
            if (_isConnectable && !isConnectabledOnce)
            {
                isConnectabledOnce = true;
            }
            this.isConnectable = _isConnectable;
        }

        [SerializeField] private bool isConnectabledOnce;

        public DeviceCache(string id, string name, bool isConnectable)
        {
            this.id = id;
            this.name = name;
            SetConnectable(isConnectable);
        }

        public bool CheckIsVerifiedName()
        {
            return name.Length > 0;
        }

        public bool CheckIsConnectabledOnce()
        {
            return isConnectabledOnce;
        }
    }
    [Readonly] public DictionaryVisualized<string, DeviceCache> deviceCacheById = new DictionaryVisualized<string, DeviceCache>();

    public UnityEvent<DeviceCache, bool> deviceUpdatedEvent = new UnityEvent<DeviceCache, bool>();


    HashSet<string> targetNames;
    bool hasTarget;
    bool isTargetUntilConnectable = false;
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
    bool isScanEnabled = false;

    private void Awake()
    {
        isScanEnabled = false;
        this.enabled = false;
    }
    private void OnEnable()
    {
        StartScan();
    }

    public void StartScan()
    {
        if (isScanEnabled)
        {
            return;
        }
        isScanEnabled = true;
        deviceCacheById.Clear();
        BleApi.StartDeviceScan();
        Debug.Log($"[Start Scan]");
        if (!this.enabled)
            this.enabled = true;
    }

    private void OnDisable()
    {
        StopScan();
    }
    public void StopScan()
    {
        if (!isScanEnabled)
        {
            return;
        }

        isScanEnabled = false;
        BleApi.StopDeviceScan();
        Debug.Log($"[Stop Scan]");
        Debug.Log("----------------------------------");
        if (this.enabled)
            this.enabled = false;
    }
    string lastError;

    private void Update()
    {
        BleApi.ScanStatus status;

        var device = new BleApi.DeviceUpdate();
        do
        {
            status = BleApi.PollDevice(ref device, false);

            if (status == BleApi.ScanStatus.FINISHED)
            {
                Debug.Log("ScanStatus : Finished");
                StopScan();
                break;
            }

            if (device.id.Length == 0)
            {
                continue;
            }

            bool isNew = false;
            if (!deviceCacheById.TryGetValue(device.id, out var deviceCache))
            {
                string newDeviceName = (device.name ?? string.Empty).Trim();
                deviceCacheById.Add(device.id, deviceCache = new DeviceCache(device.id, newDeviceName, device.isConnectable));
                if (newDeviceName.Length > 0)
                {
                    //Debug.LogWarning($"[   New   ] name: '{device.name}'\nid: '{device.id}'\nconnectable: '{device.isConnectable}'");
                    if (hasTarget && !isTargetUntilConnectable && targetNames.Equals(newDeviceName))
                    {
                        targetNames.Remove(newDeviceName);
                        targetDeviceEvent?.Invoke(newDeviceName);
                    }
                }
                isNew = true;
            }
            else
            {
                if (device.nameUpdated = (device.nameUpdated && device.name.Length > 0 && !deviceCache.name.Equals(device.name)))
                {
                    //Debug.LogWarning($"[ Updated ] name: '{deviceCache.name}'->'{device.name}'\nid: '{device.id}'");
                    deviceCache.name = device.name;
                }

                if (device.isConnectableUpdated = (device.isConnectableUpdated && deviceCache.isConnectable != device.isConnectable))
                {
                    deviceCache.SetConnectable(device.isConnectable);
                    //Debug.LogWarning($"[ Updated ] name: '{deviceCache.name}'\nisConnectable : {deviceCache.isConnectable}->{device.isConnectable}\nid: '{device.id}'");
                }
            }

            if (isNew || device.nameUpdated || device.isConnectableUpdated)
            {
                deviceUpdatedEvent.Invoke(deviceCache, isNew);
                if (hasTarget && isTargetUntilConnectable && targetNames.Equals(deviceCache.name))
                {
                    targetNames.Remove(deviceCache.name);
                    targetDeviceEvent?.Invoke(deviceCache.name);
                }
            }

        } while (status == BleApi.ScanStatus.AVAILABLE);

        // log potential errors
        BleApi.GetError(out var bleResult);
        if (!string.IsNullOrEmpty(bleResult.msg))
        {
            if (lastError != bleResult.msg)
            {
                if (!bleResult.msg.Equals("Ok"))
                    Debug.LogError("ble Error : " + bleResult.msg);
                lastError = bleResult.msg;
            }
        }
    }

    protected override void OnDispose()
    {
        StopScan();
        BleApi.Quit();
    }


}
