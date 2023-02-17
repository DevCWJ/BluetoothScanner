using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.UI;


public class Demo : MonoBehaviour
{
    [SerializeField] bool isScanningDevices = false;
    [SerializeField] bool isScanningServices = false;
    [SerializeField] bool isScanningCharacteristics = false;
    [SerializeField] bool isSubscribed = false;
    public Text deviceScanButtonText;
    public Text deviceScanStatusText;
    public GameObject deviceScanResultProto;
    public Button serviceScanButton;
    public Text serviceScanStatusText;
    public Dropdown serviceDropdown;
    public Button characteristicScanButton;
    public Text characteristicScanStatusText;
    public Dropdown characteristicDropdown;
    public Button subscribeButton;
    public Text subcribeText;
    public Button writeButton;
    public InputField writeInput;
    public Text errorText;

    Transform scanResultRoot;
    public string selectedDeviceId;
    public string selectedServiceId;
    Dictionary<string, string> characteristicNames = new Dictionary<string, string>();
    public string selectedCharacteristicId;
    string lastError;
    Dictionary<string, DeviceCache> deviceCacheById = new Dictionary<string, DeviceCache>();



    void Start()
    {
        scanResultRoot = deviceScanResultProto.transform.parent;
        deviceScanResultProto.transform.SetParent(null);
        isScanningDevices = true;
    }

    // Update is called once per frame
    void Update()
    {
        BleApi.ScanStatus status;
        if (isScanningDevices)
        {
            var device = new BleApi.DeviceUpdate();
            do
            {
                status = BleApi.PollDevice(ref device, false);
                if (status == BleApi.ScanStatus.AVAILABLE)
                {
                    bool isUpdated = false;
                    if(!deviceCacheById.TryGetValue(device.id, out var deviceCache))
                    {
                        deviceCacheById.Add(device.id, deviceCache = new DeviceCache(device.id, device.name, device.isConnectable));
                        if (!string.IsNullOrEmpty(device.name))
                            Debug.Log($"[   New   ] name: '{device.name}'\nid: '{device.id}'");
                    }

                    if (device.nameUpdated)
                    {
                        if (!string.IsNullOrEmpty(device.name))
                            Debug.Log($"[ Updated ] name: '{deviceCache.name}'->'{device.name}'\nid: '{device.id}'");
                        deviceCache.name = device.name;
                        isUpdated = true;
                    }
                    if (device.isConnectableUpdated)
                    {
                        if (!string.IsNullOrEmpty(device.name))
                            Debug.Log($"[ Updated ] name: '{deviceCache.name}' - isConnectable : {deviceCache.isConnectable}->{device.isConnectable}\nid: '{device.id}'");
                        deviceCache.isConnectable = device.isConnectable;
                        isUpdated = true;
                    }
                    
                    if (isUpdated)
                    {
                        deviceCache.SetDirty();
                    }

                    if (deviceCache.isDirty && deviceCache.CheckIsVerifiedAndConnectable())
                    {
                        Debug.Log($"[ 연결가능] name: '{deviceCache.name}'\nid: '{deviceCache.id}'");
                        //GameObject g = Instantiate(deviceScanResultProto, scanResultRoot);
                        //g.name = device.id;
                        //g.transform.GetChild(0).GetComponent<Text>().text = deviceCache.name;
                        //g.transform.GetChild(1).GetComponent<Text>().text = deviceCache.id;
                    }
                }
                else if (status == BleApi.ScanStatus.FINISHED)
                {
                        Debug.Log($"[ 스캔완료]");
                    isScanningDevices = false;
                    //deviceScanButtonText.text = "Scan devices";
                    //deviceScanStatusText.text = "finished";
                }
            } while (status == BleApi.ScanStatus.AVAILABLE);
        }
        if (isScanningServices)
        {
            do
            {
                status = BleApi.PollService(out var service, false);
                if (status == BleApi.ScanStatus.AVAILABLE)
                {
                    serviceDropdown.AddOptions(new List<string> { service.uuid });

                    // first option gets selected
                    if (serviceDropdown.options.Count == 1)
                        SelectService(serviceDropdown.gameObject);
                }
                else if (status == BleApi.ScanStatus.FINISHED)
                {
                    isScanningServices = false;
                    serviceScanButton.interactable = true;
                    serviceScanStatusText.text = "finished";
                }
            } while (status == BleApi.ScanStatus.AVAILABLE);
        }
        if (isScanningCharacteristics)
        {
            do
            {
                status = BleApi.PollCharacteristic(out var characteristic, false);
                if (status == BleApi.ScanStatus.AVAILABLE)
                {
                    string name = characteristic.userDescription != "no description available" ? characteristic.userDescription : characteristic.uuid;
                    characteristicNames[name] = characteristic.uuid;
                    characteristicDropdown.AddOptions(new List<string> { name });
                    // first option gets selected
                    if (characteristicDropdown.options.Count == 1)
                        SelectCharacteristic(characteristicDropdown.gameObject);
                }
                else if (status == BleApi.ScanStatus.FINISHED)
                {
                    isScanningCharacteristics = false;
                    characteristicScanButton.interactable = true;
                    characteristicScanStatusText.text = "finished";
                }
            } while (status == BleApi.ScanStatus.AVAILABLE);
        }
        if (isSubscribed)
        {
            while (BleApi.PollData(out var res, false))
            {
                subcribeText.text = BitConverter.ToString(res.buf, 0, res.size);
                // subcribeText.text = Encoding.ASCII.GetString(res.buf, 0, res.size);
            }
        }
        // log potential errors
        BleApi.GetError(out var errMsg);
        if (!string.IsNullOrEmpty(errMsg.msg))
        {
            if (lastError != errMsg.msg)
            {
                Debug.LogError(errMsg.msg);
                errorText.text = errMsg.msg;
                lastError = errMsg.msg;
            }
        }
    }

    private void OnApplicationQuit()
    {
        BleApi.Quit();
    }

    public void StartStopDeviceScan()
    {
        if (!isScanningDevices)
        {
            // start new scan
            for (int i = scanResultRoot.childCount - 1; i >= 0; i--)
                Destroy(scanResultRoot.GetChild(i).gameObject);
            BleApi.StartDeviceScan();
            isScanningDevices = true;
            deviceScanButtonText.text = "Stop scan";
            deviceScanStatusText.text = "scanning";
        }
        else
        {
            // stop scan
            isScanningDevices = false;
            BleApi.StopDeviceScan();
            deviceScanButtonText.text = "Start scan";
            deviceScanStatusText.text = "stopped";
        }
    }

    public void SelectDevice(GameObject data)
    {
        for (int i = 0; i < scanResultRoot.transform.childCount; i++)
        {
            var child = scanResultRoot.transform.GetChild(i).gameObject;
            child.transform.GetChild(0).GetComponent<Text>().color = child == data ? Color.red :
                deviceScanResultProto.transform.GetChild(0).GetComponent<Text>().color;
        }
        selectedDeviceId = data.name;
        serviceScanButton.interactable = true;
    }

    public void StartServiceScan()
    {
        if (!isScanningServices)
        {
            // start new scan
            serviceDropdown.ClearOptions();
            BleApi.ScanServices(selectedDeviceId);
            isScanningServices = true;
            serviceScanStatusText.text = "scanning";
            serviceScanButton.interactable = false;
        }
    }

    public void SelectService(GameObject data)
    {
        selectedServiceId = serviceDropdown.options[serviceDropdown.value].text;
        characteristicScanButton.interactable = true;
    }
    public void StartCharacteristicScan()
    {
        if (!isScanningCharacteristics)
        {
            // start new scan
            characteristicDropdown.ClearOptions();
            BleApi.ScanCharacteristics(selectedDeviceId, selectedServiceId);
            isScanningCharacteristics = true;
            characteristicScanStatusText.text = "scanning";
            characteristicScanButton.interactable = false;
        }
    }

    public void SelectCharacteristic(GameObject data)
    {
        string name = characteristicDropdown.options[characteristicDropdown.value].text;
        selectedCharacteristicId = characteristicNames[name];
        subscribeButton.interactable = true;
        writeButton.interactable = true;
    }

    public void Subscribe()
    {
        // no error code available in non-blocking mode
        BleApi.SubscribeCharacteristic(selectedDeviceId, selectedServiceId, selectedCharacteristicId, false);
        isSubscribed = true;
    }

    public void Write()
    {
        byte[] payload = Encoding.ASCII.GetBytes(writeInput.text);
        BleApi.BLEData data = new BleApi.BLEData();
        data.buf = new byte[512];
        data.size = (short)payload.Length;
        data.deviceId = selectedDeviceId;
        data.serviceUuid = selectedServiceId;
        data.characteristicUuid = selectedCharacteristicId;
        for (int i = 0; i < payload.Length; i++)
            data.buf[i] = payload[i];
        // no error code available in non-blocking mode
        BleApi.SendData(in data, false);
    }
}
