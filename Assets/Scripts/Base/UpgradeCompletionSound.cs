using System;
using System.Reflection;
using UnityEngine;

namespace LaneSurvivor.Base
{
    public static class UpgradeCompletionSound
    {
        private static object finishSoundClip;

        public static bool HasGeneratedCompletionSoundClip => finishSoundClip != null;

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

        public static void Play(Component audioSource)
        {
            // Audio can be absent in some test runners, so missing components simply skip playback.
            if (audioSource == null)
            {
                return;
            }

            // Lazily generate the chime only when the completion path is actually used.
            object clip = GetFinishSoundClip();
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

        private static object GetFinishSoundClip()
        {
            // Reuse one generated clip so repeated upgrades do not allocate new audio buffers.
            if (finishSoundClip != null)
            {
                return finishSoundClip;
            }

            // Resolve AudioClip only when Unity has loaded the optional audio module.
            Type audioClipType = FindOptionalUnityAudioType("UnityEngine.AudioClip");
            if (audioClipType == null)
            {
                return null;
            }

            // A short two-tone synthesized chime satisfies the finish-sound requirement without imported assets.
            const int sampleRate = 22050;
            const float clipSeconds = 0.42f;
            int sampleCount = Mathf.CeilToInt(sampleRate * clipSeconds);
            float[] samples = new float[sampleCount];

            for (int index = 0; index < sampleCount; index += 1)
            {
                // Time in seconds drives the generated sine waves.
                float time = index / (float)sampleRate;

                // The envelope fades out quickly so repeated local upgrades do not become harsh.
                float envelope = 1f - Mathf.Clamp01(time / clipSeconds);

                // Two simple tones create a tiny "upgrade complete" flourish.
                float firstTone = Mathf.Sin(Mathf.PI * 2f * 660f * time);
                float secondTone = Mathf.Sin(Mathf.PI * 2f * 990f * time) * 0.55f;
                samples[index] = (firstTone + secondTone) * envelope * 0.22f;
            }

            // Create the clip through reflection so builds without AudioModule can still compile the prototype.
            MethodInfo createMethod = audioClipType.GetMethod("Create", new[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(bool) });
            finishSoundClip = createMethod?.Invoke(null, new object[] { "Building Upgrade Complete Chime", sampleCount, 1, sampleRate, false });
            if (finishSoundClip == null)
            {
                return null;
            }

            // Push the generated samples into the reflected AudioClip instance when the method is present.
            MethodInfo setDataMethod = audioClipType.GetMethod("SetData", new[] { typeof(float[]), typeof(int) });
            setDataMethod?.Invoke(finishSoundClip, new object[] { samples, 0 });
            return finishSoundClip;
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
    }
}
