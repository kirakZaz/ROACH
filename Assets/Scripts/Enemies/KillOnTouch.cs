using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Roach.Assets.Scripts.Enemies
{
    public class PlayerDeathOnTrigger : MonoBehaviour
    {
        [SerializeField]
        private string enemyTag = "Enemy";

        [SerializeField]
        private float reloadDelay = 0.1f;
        private bool isReloading;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isReloading)
                return;
            if (!other.CompareTag(enemyTag))
                return;

            isReloading = true;
            Invoke(nameof(Reload), reloadDelay);
        }

        private void Reload()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
