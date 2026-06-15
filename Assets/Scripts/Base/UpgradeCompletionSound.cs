using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace LaneSurvivor.Base
{
    public enum UpgradeCompletionSoundProfile
    {
        Hq,
        BioLab,
        Hangar,
        TrainingFacility,
        LivingQuarters
    }

    public static class UpgradeCompletionSound
    {
        private static readonly Dictionary<UpgradeCompletionSoundProfile, object> FinishSoundClips = new();

        public static bool HasGeneratedCompletionSoundClip => FinishSoundClips.Count > 0;

        public static bool HasGeneratedCompletionSoundClipForProfile(UpgradeCompletionSoundProfile profile)
        {
            // Each building family owns a separate generated clip, so tests can verify profile-specific playback.
            return FinishSoundClips.ContainsKey(profile);
        }

        public static string GetSoundSignature(UpgradeCompletionSoundProfile profile)
        {
            // Expose a stable non-audio signature so tests can prove the synthesized profiles are not aliases.
            SoundDefinition definition = GetSoundDefinition(profile);
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1:0.00}:{2:0.0}:{3:0.0}:{4:0.0}:{5:0.00}",
                definition.ClipName,
                definition.ClipSeconds,
                definition.FirstToneHz,
                definition.SecondToneHz,
                definition.ThirdToneHz,
                definition.Volume);
        }

        public static Component CreateOptionalAudioSource(GameObject ownerObject)
        {
            // Resolve AudioSource lazily so the prototype can compile even when AudioModule is not referenced.
            Type audioSourceType = FindOptionalUnityAudioType("UnityEngine.AudioSource");
            if (ownerObject == null || audioSourceType == null)
            {
                return null;
            }

            // Add the reflected component to the building root so completion sound follows that building.
            Component audioSource = ownerObject.AddComponent(audioSourceType);
            SetReflectedProperty(audioSource, "playOnAwake", false);
            SetReflectedProperty(audioSource, "spatialBlend", 0.15f);
            SetReflectedProperty(audioSource, "volume", 0.55f);
            return audioSource;
        }

        public static void Play(Component audioSource, UpgradeCompletionSoundProfile profile)
        {
            // Audio can be absent in some test runners, so missing components simply skip playback.
            if (audioSource == null)
            {
                return;
            }

            // Lazily generate the chime only when the completion path is actually used.
            object clip = GetFinishSoundClip(profile);
            if (clip == null)
            {
                return;
            }

            // Invoke PlayOneShot through reflection so the runtime assembly does not depend on AudioModule.
            MethodInfo playOneShotMethod = audioSource.GetType().GetMethod("PlayOneShot", new[] { clip.GetType() });
            playOneShotMethod?.Invoke(audioSource, new[] { clip });
        }

        public static Type FindOptionalUnityAudioType(string fullTypeName)
        {
            // First try the direct assembly-qualified lookup, which works when Unity has loaded AudioModule normally.
            Type directType = Type.GetType($"{fullTypeName}, UnityEngine.AudioModule");
            if (directType != null)
            {
                return directType;
            }

            // Some test runners load engine modules without making Type.GetType resolve them by name.
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type existingType = assembly.GetType(fullTypeName);
                if (existingType != null)
                {
                    return existingType;
                }
            }

            try
            {
                // Loading by assembly name gives the optional audio path one last chance without compile-time references.
                Assembly audioAssembly = Assembly.Load("UnityEngine.AudioModule");
                return audioAssembly.GetType(fullTypeName);
            }
            catch
            {
                // Missing AudioModule is allowed in stripped test environments; callers will skip playback.
                return null;
            }
        }

        private static object GetFinishSoundClip(UpgradeCompletionSoundProfile profile)
        {
            // Reuse each building profile's generated clip so repeated upgrades do not allocate new audio buffers.
            if (FinishSoundClips.TryGetValue(profile, out object existingClip))
            {
                return existingClip;
            }

            // Resolve AudioClip only when Unity has loaded the optional audio module.
            Type audioClipType = FindOptionalUnityAudioType("UnityEngine.AudioClip");
            if (audioClipType == null)
            {
                return null;
            }

            // A short profile-specific synthesized chime satisfies the finish-sound requirement without imported assets.
            SoundDefinition definition = GetSoundDefinition(profile);
            const int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(sampleRate * definition.ClipSeconds);
            float[] samples = new float[sampleCount];

            for (int index = 0; index < sampleCount; index += 1)
            {
                // Time in seconds drives the generated sine waves.
                float time = index / (float)sampleRate;

                // The envelope fades out quickly so repeated local upgrades do not become harsh.
                float progress01 = Mathf.Clamp01(time / definition.ClipSeconds);
                float envelope = Mathf.Sin(progress01 * Mathf.PI) * (1f - progress01 * 0.18f);

                // Three profile-tuned tones make each building's completion sound audibly distinct.
                float firstTone = Mathf.Sin(Mathf.PI * 2f * definition.FirstToneHz * time);
                float secondTone = Mathf.Sin(Mathf.PI * 2f * definition.SecondToneHz * time) * 0.55f;
                float thirdTone = Mathf.Sin(Mathf.PI * 2f * definition.ThirdToneHz * time) * 0.30f;
                samples[index] = (firstTone + secondTone + thirdTone) * envelope * definition.Volume;
            }

            // Create the clip through reflection so builds without AudioModule can still compile the prototype.
            MethodInfo createMethod = audioClipType.GetMethod("Create", new[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(bool) });
            object finishSoundClip = createMethod?.Invoke(null, new object[] { definition.ClipName, sampleCount, 1, sampleRate, false });
            if (finishSoundClip == null)
            {
                return null;
            }

            // Push the generated samples into the reflected AudioClip instance when the method is present.
            MethodInfo setDataMethod = audioClipType.GetMethod("SetData", new[] { typeof(float[]), typeof(int) });
            setDataMethod?.Invoke(finishSoundClip, new object[] { samples, 0 });
            FinishSoundClips[profile] = finishSoundClip;
            return finishSoundClip;
        }

        private static SoundDefinition GetSoundDefinition(UpgradeCompletionSoundProfile profile)
        {
            // The frequencies are intentionally separated by building role so completions do not blur together.
            return profile switch
            {
                UpgradeCompletionSoundProfile.Hq => new SoundDefinition("HQ Command Upgrade Chime", 0.50f, 392f, 587.33f, 783.99f, 0.20f),
                UpgradeCompletionSoundProfile.BioLab => new SoundDefinition("Bio Lab Upgrade Chime", 0.42f, 659.25f, 987.77f, 1318.51f, 0.18f),
                UpgradeCompletionSoundProfile.Hangar => new SoundDefinition("Hangar Upgrade Chime", 0.48f, 220f, 329.63f, 493.88f, 0.24f),
                UpgradeCompletionSoundProfile.TrainingFacility => new SoundDefinition("Training Facility Upgrade Chime", 0.36f, 523.25f, 698.46f, 1046.50f, 0.19f),
                UpgradeCompletionSoundProfile.LivingQuarters => new SoundDefinition("Living Quarters Upgrade Chime", 0.46f, 440f, 660f, 880f, 0.17f),
                _ => new SoundDefinition("Generic Building Upgrade Chime", 0.44f, 493.88f, 739.99f, 987.77f, 0.18f)
            };
        }

        private static void SetReflectedProperty(object target, string propertyName, object value)
        {
            // Optional audio properties differ by Unity module version, so silently skip missing setters.
            PropertyInfo propertyInfo = target?.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (propertyInfo == null || !propertyInfo.CanWrite)
            {
                return;
            }

            // Apply simple bool/float values to the reflected AudioSource component.
            propertyInfo.SetValue(target, value);
        }

        private readonly struct SoundDefinition
        {
            public SoundDefinition(string clipName, float clipSeconds, float firstToneHz, float secondToneHz, float thirdToneHz, float volume)
            {
                // The clip name is visible in Unity audio tooling and doubles as a readable test signature.
                ClipName = clipName;

                // The clip duration controls how long the generated local flourish lasts.
                ClipSeconds = clipSeconds;

                // Tone frequencies define the audible identity for one building family.
                FirstToneHz = firstToneHz;
                SecondToneHz = secondToneHz;
                ThirdToneHz = thirdToneHz;

                // Profile volume keeps low-pitched and high-pitched sounds similarly gentle.
                Volume = volume;
            }

            public string ClipName { get; }

            public float ClipSeconds { get; }

            public float FirstToneHz { get; }

            public float SecondToneHz { get; }

            public float ThirdToneHz { get; }

            public float Volume { get; }
        }
    }
}
