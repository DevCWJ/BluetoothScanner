using System.Collections;

using UnityEngine;

namespace CWJ
{
    public enum MousePhase
    {
        Began = 0,

        Moved = 1,

        Stationary = 2,

        Ended = 3,

        None = 4
    }

    public class TouchManager_PC : TouchManager
    {
        public override sealed bool AddTouchListener(TouchListener listener)
        {
            if (!base.AddTouchListener(listener))
            {
                return false;
            }

            for (int i = 0; i < 5; i++) //5 = EnumUtil.GetLength<TouchPhase>()
            {
                touchEvents[i].AddListener_New(listener.touchEvents[i].Invoke, false);
            }

            onUpdateEnded.AddListener_New(listener.onUpdateEnded.Invoke, false);

            ArrayUtil.Add(ref touchListeners, listener);
            return true;
        }

        public override sealed bool RemoveTouchListener(TouchListener listener)
        {
            if (!base.RemoveTouchListener(listener))
            {
                return false;
            }

            ArrayUtil.Remove(ref touchListeners, listener);

            for (int i = 0; i < 5; i++) //5 = EnumUtil.GetLength<TouchPhase>()
            {
                touchEvents[i].RemoveListener_New(listener.touchEvents[i].Invoke);
            }

            onUpdateEnded.RemoveListener_New(listener.onUpdateEnded.Invoke);
            return true;
        }

        public override sealed void UpdateInputSystem()
        {
            MousePhase mousePhase = MousePhase.None;

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                mousePhase = MousePhase.Began;
            }
            else if (Input.GetKey(KeyCode.Mouse0))
            {
                mousePhase = GetPhaseWhenMousePressed();
            }
            else if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                mousePhase = MousePhase.Ended;
                if (CO_StationaryCheck != null)
                {
                    StopCoroutine(CO_StationaryCheck);
                    CO_StationaryCheck = null;
                }
            }

            isHoldDown = mousePhase < MousePhase.Ended;
            isMoving = mousePhase == MousePhase.Moved;

            if (mousePhase != MousePhase.None)
            {
                touchEvents[mousePhase.ToInt()]?.Invoke();
            }

            onUpdateEnded?.Invoke();
        }

        private Vector3 prevMousePos;

        //(Input.GetAxisRaw("Mouse X") == .0f && Input.GetAxisRaw("Mouse Y") == .0f); touch되는 windows에서 touch 커서드래그를 인식못함
        private bool GetMouseStationary()
        {
            Vector3 prevPos = prevMousePos;
            prevMousePos = Input.mousePosition;
            return prevPos == Input.mousePosition;
        }

        //GetAxisRaw 가 버그가있어서 코루틴필요
        private MousePhase GetPhaseWhenMousePressed()
        {
            if (GetMouseStationary())
            {
                if (isMoving)
                {
                    if (CO_StationaryCheck == null)
                    {
                        CO_StationaryCheck = DO_StationaryCheck();
                        StartCoroutine(CO_StationaryCheck);
                    }
                    return MousePhase.Moved;
                }
                else
                {
                    return MousePhase.Stationary;
                }
            }
            else
            {
                if (CO_StationaryCheck != null)
                {
                    StopCoroutine(CO_StationaryCheck);
                    CO_StationaryCheck = null;
                }

                return MousePhase.Moved;
            }
        }

        private IEnumerator CO_StationaryCheck = null;

        private IEnumerator DO_StationaryCheck()
        {
            yield return null; //이상하면 WaitForEndOfFrame() 로 바꾸기

            if (GetMouseStationary())
            {
                isMoving = false;
            }

            CO_StationaryCheck = null;
        }
    }
}