using UnityEngine;

namespace Roach.Assets.Scripts.Core
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundPlayer : MonoBehaviour
    {
        private AudioSource audioSource;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        public void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (clip == null)
                return;
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
