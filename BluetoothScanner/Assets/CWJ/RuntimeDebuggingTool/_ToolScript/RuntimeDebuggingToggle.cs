using UnityEngine;
using CWJ;

public class RuntimeDebuggingToggle : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button button;

    [SerializeField] int maxClick = 5;
    private float clickCnt = 0;

    private void Start()
    {
#if CWJ_RUNTIMEDEBUGGING_DISABLED
        return;
#endif
        button.onClick.AddListener(OnClickBtn);
        RuntimeDebuggingTool.Instance.allVisibleMultipleEvent += OnAllVisibleKeyEvent;
    }

    private void OnClickBtn()
    {
        ++clickCnt;
    }

    private bool OnAllVisibleKeyEvent()
    {
        float check = clickCnt;
        clickCnt = Mathf.Repeat(clickCnt, maxClick); //0~(maxClick-1)  
        return check == maxClick;
    }
}
