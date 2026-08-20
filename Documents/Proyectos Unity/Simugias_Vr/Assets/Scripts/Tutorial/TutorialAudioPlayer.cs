using UnityEngine;

/// <summary>
/// Audio del tutorial. Si no hay clips asignados, genera tonos suaves.
/// Cada paso puede sobrescribir el clip de narración o de evento.
/// </summary>
public class TutorialAudioPlayer : MonoBehaviour
{
    [SerializeField] AudioClip attentionClip;
    [SerializeField] AudioClip selectClip;
    [SerializeField] AudioClip confirmClip;
    [SerializeField] AudioClip errorClip;
    [SerializeField] AudioClip transitionClip;

    AudioSource _source;
    AudioClip _genAttention;
    AudioClip _genSelect;
    AudioClip _genConfirm;
    AudioClip _genError;
    AudioClip _genTransition;

    public static TutorialAudioPlayer Create(Transform parent)
    {
        var go = new GameObject("TutorialAudio");
        if (parent != null)
            go.transform.SetParent(parent, false);
        var player = go.AddComponent<TutorialAudioPlayer>();
        player.Build();
        return player;
    }

    public void BindClips(AudioClip attention, AudioClip select, AudioClip confirm, AudioClip error, AudioClip transition)
    {
        attentionClip = attention;
        selectClip = select;
        confirmClip = confirm;
        errorClip = error;
        transitionClip = transition;
    }

    void Build()
    {
        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;
        _source.loop = false;
        _source.volume = 0.55f;

        _genAttention = MakeTone("tut_attention", 784f, 0.14f, 0.22f);
        _genSelect = MakeTone("tut_select", 1174f, 0.07f, 0.16f);
        _genConfirm = MakeChord("tut_confirm", 523f, 784f, 0.18f, 0.22f);
        _genError = MakeTone("tut_error", 196f, 0.16f, 0.14f);
        _genTransition = MakeTone("tut_transition", 440f, 0.09f, 0.12f);
    }

    public void PlayAttention(AudioClip overrideClip = null) => Play(overrideClip != null ? overrideClip : attentionClip, _genAttention, 0.7f);
    public void PlaySelect() => Play(selectClip, _genSelect, 0.45f);
    public void PlayConfirm(AudioClip overrideClip = null) => Play(overrideClip != null ? overrideClip : confirmClip, _genConfirm, 0.65f);
    public void PlayError(AudioClip overrideClip = null) => Play(overrideClip != null ? overrideClip : errorClip, _genError, 0.4f);
    public void PlayTransition() => Play(transitionClip, _genTransition, 0.4f);

    public void PlayNarration(AudioClip clip)
    {
        if (clip == null || _source == null) return;
        _source.PlayOneShot(clip, 1f);
    }

    void Play(AudioClip preferred, AudioClip generated, float volume)
    {
        if (_source == null) return;
        AudioClip clip = preferred != null ? preferred : generated;
        if (clip == null) return;
        _source.PlayOneShot(clip, volume);
    }

    static AudioClip MakeTone(string name, float freq, float duration, float amplitude)
    {
        int rate = 22050;
        int samples = Mathf.CeilToInt(rate * duration);
        var clip = AudioClip.Create(name, samples, 1, rate, false);
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)samples;
            float env = t < 0.12f ? t / 0.12f : (t > 0.7f ? (1f - t) / 0.3f : 1f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * i / rate) * amplitude * env;
        }
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip MakeChord(string name, float f1, float f2, float duration, float amplitude)
    {
        int rate = 22050;
        int samples = Mathf.CeilToInt(rate * duration);
        var clip = AudioClip.Create(name, samples, 1, rate, false);
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)samples;
            float env = t < 0.1f ? t / 0.1f : (t > 0.65f ? (1f - t) / 0.35f : 1f);
            float s = Mathf.Sin(2f * Mathf.PI * f1 * i / rate) + Mathf.Sin(2f * Mathf.PI * f2 * i / rate);
            data[i] = s * 0.5f * amplitude * env;
        }
        clip.SetData(data, 0);
        return clip;
    }
}
