using CWJ.IoT;

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurtainAnimCtrlr : MonoBehaviour
{
    public Animator animator;
    public Curtain curtain;

    private void Start()
    {
        curtain.curtainStateEvent.AddListener(OnChangeCurtainState);
    }

    private void OnChangeCurtainState(Curtain_State.ActType oldAct, Curtain_State.ActType newAct, float dimmer)
    {
        if (dimmer >= 0)
            animator.SetFloat("value", Mathf.Clamp01(dimmer * 0.01f));
    }


}
