
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.Events;

using CWJ;
using CWJ.AccessibleEditor;
using CWJ.Serializable;

[DisallowMultipleComponent]
public class BleScannerProcessMngr : DisposableMonoBehaviour
{
    public enum CommandType
    {
        NULL = 0,
        //start,
        //stop,
        //show,
        //hide,
        quit,
        exit
    }

    [SerializeField, Readonly] string editorBuildFilePath;


#if UNITY_EDITOR    
    [InvokeButton(isNeedSave:true)]
    void Editor_SetEditorBuildFile()
    {
        PathUtil.OpenFileDialog("Select BuildFile",
        (path) =>
        {
            if (path.Length == 0)
                Debug.LogError("선택안함");
            else
                editorBuildFilePath = path;
        }, UnityEngine.Application.dataPath, "실행파일", "exe");
    }

    [UnityEditor.InitializeOnLoadMethod]
    static void Editor_BuildVersionDate()
    {
        BuildEventSystem.IsAutoDateVersion = true;
    }
#endif

    [SerializeField, ShowConditional(EPlayMode.PlayMode), Readonly] string commandTxtPath;
    [SerializeField, ShowConditional(EPlayMode.PlayMode), Readonly] string detectedTxtPath;

    [MustRequiredComp, SerializeField, Readonly] FileChangedChecker fileChangedChecker;

    [MustRequiredComp, SerializeField, Readonly] BleScanner bleScanner;

    [SerializeField, Readonly] CommandType commandType = CommandType.NULL;

    bool isDetectOnlyConnectabled = false;
    string separatorStr;
    const string Tag_System = "SYSTEM PATH AND CONFIGURATION";
    const string Tag_CommandTextFile = "Command File Name";
    const string Tag_DetectedTextFile = "Detected File Name";
    const string Tag_SeparatorChar = "Separator Char";
    
    [SerializeField]
    DictionaryVisualized<CommandType, UnityEvent> callbackByCmdType = new DictionaryVisualized<CommandType, UnityEvent>();


    private void Start()
    {
        if (!MonoBehaviourEventHelper.IS_EDITOR && !WinSysHelper.isVerifiedProcess)
        {
            return;
        }
        callbackByCmdType.AddCallbackInDictionaryByEnum(this, nameof(OnCommand_quit), enumStartValue: CommandType.quit, separatorChr: '_');

        string buildFolderPath = null;
        if (!MonoBehaviourEventHelper.IS_EDITOR)
        {
            buildFolderPath = WinSysHelper.MyExeFolderPath;
        }

#if UNITY_EDITOR
        buildFolderPath = Path.GetDirectoryName(editorBuildFilePath);
#endif

        var exeFolder = buildFolderPath;
        string iniPath = Path.Combine(exeFolder, "BleConfig.ini");
        IniFile ini = new IniFile();
        Debug.Log("ini path : " + iniPath);
        if (!File.Exists(iniPath))
        {
            ini[Tag_System][Tag_CommandTextFile] = commandTxtPath = "Command.txt";
            ini[Tag_System][Tag_DetectedTextFile] = detectedTxtPath = "DetectedNames.txt";
            ini[Tag_System][Tag_SeparatorChar] = separatorStr = ",";
            ini[Tag_System][nameof(isDetectOnlyConnectabled)] = isDetectOnlyConnectabled = true;
            ini.Save(iniPath, FileMode.Create);
        }
        else
        {
            ini.Load(iniPath);
            isDetectOnlyConnectabled = ini[Tag_System][nameof(isDetectOnlyConnectabled)].ToBool();
            separatorStr = ini[Tag_System][Tag_SeparatorChar].ToString();
            commandTxtPath = ini[Tag_System][Tag_CommandTextFile].ToString();
            detectedTxtPath = ini[Tag_System][Tag_DetectedTextFile].ToString();
        }

        commandTxtPath = Path.Combine(exeFolder, commandTxtPath);
        detectedTxtPath = Path.Combine(exeFolder, detectedTxtPath);

        if (!File.Exists(commandTxtPath))
            TextReadOrWriter.CreateText(commandTxtPath, FileShare.Write);
        else
            UpdateCommandByTxt(commandTxtPath);

        TextReadOrWriter.WriteText(commandTxtPath, string.Empty, FileShare.Write);
        lastCommandStr = string.Empty;
        commandType = CommandType.NULL;

        if (!File.Exists(detectedTxtPath))
            TextReadOrWriter.CreateText(detectedTxtPath);

        fileChangedChecker.fileChangedEvent.AddListener_New(UpdateCommandByTxt);
        fileChangedChecker.InitSystemWatcher(Path.GetDirectoryName(commandTxtPath), Path.GetFileName(commandTxtPath));

        verifiedId = new HashSet<string>();
        bleScanner.deviceUpdatedEvent.AddListener((device, isNew) =>
        {
            if (device.CheckIsVerifiedName() 
                    && (!isDetectOnlyConnectabled || device.CheckIsConnectabledOnce()))
            {
                if (verifiedId.Add(device.id))
                {
                    Debug.Log($"[ New ] Name: '{device.name}'\n ID: '{device.id}'");
                }
            }
        });

        if (CO_LoopStart == null)
            CO_LoopStart = StartCoroutine(IE_LoopStartAndStop());
    }

    HashSet<string> verifiedId;

    static Coroutine CO_LoopStart = null;
    IEnumerator IE_LoopStartAndStop()
    {
        var waitForSec = new WaitForSeconds(3.7f);
        yield return null;

        do
        {
            OnCommand_start();
            yield return waitForSec;
            OnCommand_stop();
            verifiedId.Clear();

        } while (CO_LoopStart != null);
    }

    static Coroutine CO_WriteBleDevice = null;
    const string timeFormat = "HH:mm:ss";

    IEnumerator DO_WriteBleDevice()
    {
        TextReadOrWriter.WriteText(detectedTxtPath, string.Empty);
        var waitForTime = new WaitForSeconds(0.7f);
        int i = 0;
        do
        {
            yield return waitForTime;

            if (CO_WriteBleDevice == null)
            {
                yield break;
            }
            string combinedNames = string.Empty;
            string[] deviceNames = new string[0];
            if (bleScanner.deviceCacheById.Count > 0)
            {
                deviceNames = bleScanner.deviceCacheById.Values
                                .Where(device => device.CheckIsVerifiedName() && (!isDetectOnlyConnectabled || device.CheckIsConnectabledOnce()))
                                .Select(device => device.name).ToArray();

                combinedNames = string.Join(separatorStr, deviceNames);
            }

            TextReadOrWriter.WriteText(detectedTxtPath, $"[{DateTime.Now.ToString(timeFormat)}]" + combinedNames);

            Debug.Log($"Try {(++i).ConvertToOrdinal()} : " + deviceNames.Length);


        } while (CO_WriteBleDevice != null);

        CO_WriteBleDevice = null;
    }

    string lastCommandStr = string.Empty;
    void UpdateCommandByTxt(string path)
    {
        string curCommandStr = TextReadOrWriter.ReadText(path).Trim();
        if (lastCommandStr != curCommandStr)
        {
            lastCommandStr = curCommandStr;
            {
                if (commandType != CommandType.NULL)
                    commandType = CommandType.NULL;
                return;
            }
        }
        else
        {
            return;
        }

        if (!EnumUtil.TryToEnum<CommandType>(curCommandStr.ToLower(), out var newCommandType)
            || newCommandType == commandType)
        {
            return;
        }

        if (CO_LoopStart == null && (newCommandType == CommandType.quit || newCommandType == CommandType.exit))
        { //실행전, quit이나 exit을 받은경우
            return;
        }

        commandType = newCommandType;

        Debug.Log("[Command] Received : " + commandType.ToString());

        callbackByCmdType[commandType].Invoke();

        TextReadOrWriter.WriteText(commandTxtPath, string.Empty, FileShare.Write);
    }

    void OnCommand_start()
    {
        bleScanner.StartScan();

        if (CO_WriteBleDevice == null)
            CO_WriteBleDevice = StartCoroutine(DO_WriteBleDevice());
    }

    void OnCommand_stop()
    {
        if (CO_WriteBleDevice != null)
        {
            StopCoroutine(CO_WriteBleDevice);
            CO_WriteBleDevice = null;
        }

        bleScanner.StopScan();
    }

    void OnCommand_show()
    {
        WinSysHelper.ShowMyWindow();
    }
    void OnCommand_hide()
    {
        WinSysHelper.HideMyWindow();
    }
    void OnCommand_exit()
    {
        AppQuit();
    }
    void OnCommand_quit()
    {
        AppQuit();
    }

    void AppQuit()
    {
        Dispose();

        if (!Application.isEditor)
        {
            UnityEngine.Application.Quit();
        }
        else
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
                UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }

    protected override void OnDispose()
    {
        if (!WinSysHelper.isVerifiedProcess)
        {
            return;
        }
        if (CO_LoopStart != null)
        {
            StopCoroutine(CO_LoopStart);
            CO_LoopStart = null;
        }
        OnCommand_stop();
        fileChangedChecker.Dispose();
        bleScanner.Dispose();
        if (File.Exists(commandTxtPath))
            TextReadOrWriter.WriteText(commandTxtPath, string.Empty, FileShare.ReadWrite);
    }
}
