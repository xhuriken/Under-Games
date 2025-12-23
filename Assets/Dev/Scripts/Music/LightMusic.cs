using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using DG.Tweening;

public class LightMusic : MonoBehaviour
{

    [SerializeField] private Light light;
    [SerializeField] private float baseIntensity = 0.5f;
    [Tooltip("This value is copied from the intensity value at the begening of the game")]
    [SerializeField] private float scopeIntensity;
    [SerializeField] private float blinkDuration = 0.15f;
    [Tooltip("Renderer of the Light object.")]
    [SerializeField] private Renderer lightRenderer;

    [ColorUsage(showAlpha: true, hdr: true)]
    [SerializeField] private Color baseEmissionColor;

    [ColorUsage(showAlpha: true, hdr: true)]
    [SerializeField] private Color scopeEmissionColor;

    private MaterialPropertyBlock _mpb;

    private Tween tweenL;
    private Tween ColorTweenL;
    void Start()
    {
        if (_mpb == null)
            _mpb = new MaterialPropertyBlock();
        if(light == null) light = transform.GetChild(1).GetComponent<Light>();
        scopeIntensity = light.intensity; // LIGHT: get the current intensity
        lightRenderer.GetPropertyBlock(_mpb);
        if (!_mpb.isEmpty)
            scopeEmissionColor = _mpb.GetColor("_EmissionColor");
        else
            scopeEmissionColor = lightRenderer.sharedMaterial.GetColor("_EmissionColor");

        light.intensity = baseIntensity; // LIGHT : Set it to black
        SetEmissionInstant(baseEmissionColor); // MAT : set it to black

        MusicManager.beatUpdated += Blink; // Subscribe to the beatUpdated event (every beat yk)
    }

    void Blink()
    {
        // kill the previous tween if still running
        tweenL?.Kill();
        light.intensity = scopeIntensity; // Set to the maximum now
        tweenL = light.DOIntensity(baseIntensity, blinkDuration).SetEase(Ease.OutQuad);

        ColorTweenL?.Kill();
        ColorTweenL = DOVirtual.Color(scopeEmissionColor, baseEmissionColor, blinkDuration, c =>
        {
            SetEmissionInstant(c);
        });

    }

    /// <summary>
    /// Apply the emission color directly to the MaterialPropertyBlock and cache it.
    /// </summary>
    private void SetEmissionInstant(Color emissionColor)
    {
        // Get the current MaterialPropertyBlock from the renderer
        lightRenderer.GetPropertyBlock(_mpb);

        // Set the emission color in the MaterialPropertyBlock
        _mpb.SetColor("_EmissionColor", emissionColor);
        // Apply the updated MaterialPropertyBlock to the LED renderer !
        lightRenderer.SetPropertyBlock(_mpb);
    }
}
