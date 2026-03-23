using UnityEngine;

namespace Project.Core
{
        /// <summary>
    /// Spectator 本地音频播放组件。
    /// </summary>
    public sealed class SpectatorAudioPlayer
    {
        private readonly AudioSource _audioSource;

        public SpectatorAudioPlayer(Transform parent)
        {
            var root = new GameObject("SpectatorAudioPlayer");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            _audioSource = root.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.spatialBlend = 0f;
            _audioSource.volume = 0.9f;
        }

        public void Play(AudioClip clip, float volume)
        {
            if (_audioSource == null || clip == null)
            {
                return;
            }

            _audioSource.volume = Mathf.Clamp01(volume);
            _audioSource.PlayOneShot(clip);
        }
    }
}

