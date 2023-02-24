using CWJ;
using CWJ.IoT;

using System;
using System.Linq;

using UnityEngine;

[RequireComponent(typeof(Light_Switch))]
public class LightSwitchCtrlr : MonoBehaviour
{
    [SerializeField, GetComponent] Light_Switch lightSwitch;

    [SerializeField] Renderer lightRenderer;
    [SerializeField] int lightMatIndex = 0;
    [SerializeField] Light[] lights;

    [NonSerialized] MaterialPropertyBlock lightOnBlock, lightOffBlock;

    [SerializeField, ColorUsage(false, true)]
    Color emissionColor = Color.white;

    int emissionColorId;

    private void Start()
    {
        emissionColorId = Shader.PropertyToID("_EmissionColor");
        lightOnBlock = new MaterialPropertyBlock();
        lightOffBlock = new MaterialPropertyBlock();

        lightRenderer.GetPropertyBlock(lightOnBlock);
        lightOnBlock.SetColor(emissionColorId, emissionColor);
        lightRenderer.GetPropertyBlock(lightOffBlock);
        lightOffBlock.SetColor(emissionColorId, Color.black);

        lightSwitch.onChangeLightState.AddListener_New(SwitchLightOnOff);
    }

    void SwitchLightOnOff(bool isOnOff)
    {
        lightRenderer.SetPropertyBlock(isOnOff ? lightOnBlock : lightOffBlock, lightMatIndex);
        lights.ForEach(l => l.enabled = isOnOff); 
    }
}
