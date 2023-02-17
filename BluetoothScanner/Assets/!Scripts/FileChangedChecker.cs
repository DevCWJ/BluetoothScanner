using CWJ;

using System.Collections.Generic;
using System.IO;

using UnityEngine;

[UnityEngine.DisallowMultipleComponent]
public class FileChangedChecker : DisposableMonoBehaviour
{
    [SerializeField, Readonly] private string folderPath;
    [SerializeField, Readonly] private string fileName;

    private FileSystemWatcher watcher;
    [EnumFlag] public NotifyFilters notifyFilters = NotifyFilters.LastWrite;
    
    public UnityEngine.Events.UnityEvent<string> fileChangedEvent = new UnityEngine.Events.UnityEvent<string>();
    public UnityEngine.Events.UnityEvent<string> fileIsNullEvent = new UnityEngine.Events.UnityEvent<string>();

    private Queue<string> changedPathQueue;

    bool isInit = false;

    public void InitSystemWatcher(string folderPath , string filterOrFileName)
    {
        isInit = true;

        if (string.IsNullOrEmpty(folderPath))
        {
            this.enabled = false;
            return;
        }
        this.folderPath = folderPath;
        this.fileName = filterOrFileName;

        changedPathQueue = new Queue<string>();

        watcher = new FileSystemWatcher();
        watcher.Path = folderPath;
        
        if (!string.IsNullOrEmpty(fileName))
        {
            watcher.Filter = fileName;

            var fullPath = Path.Combine(folderPath, fileName);
            if (!File.Exists(fullPath))
            {
                fileIsNullEvent?.Invoke(fullPath);
            }
        }

        watcher.NotifyFilter = notifyFilters;

        watcher.Changed += OnChanged;

        this.enabled = true;
    }

    private void Awake()
    {
        if (!isInit)
            this.enabled = false;
    }

    private void OnEnable()
    {
        if (watcher != null)
            watcher.EnableRaisingEvents = true;
    }
    private void OnDisable()
    {
        if (watcher != null)
            watcher.EnableRaisingEvents = false;
    }

    protected override void OnDispose()
    {
        if (watcher != null)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Changed -= OnChanged;
            watcher.Dispose();
            watcher = null;
        }
        if (changedPathQueue != null)
        {
            changedPathQueue.Clear();
            changedPathQueue = null;
        }
    }

    private void Update()
    {
        if (changedPathQueue.Count > 0)
        {
            fileChangedEvent?.Invoke(changedPathQueue.Dequeue());
        }
    }

    private void OnChanged(object source, FileSystemEventArgs e)
    {
        changedPathQueue.Enqueue(e.FullPath);
    }


}
