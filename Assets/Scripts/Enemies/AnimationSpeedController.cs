using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Roach.Assets.Scripts.Player
{
    public class AnimationSpeedController : MonoBehaviour
    {
        public float animationSpeed = 0.5f; // 1 = normal, 0.5 = half speed

        private Animator animator;

        void Start()
        {
            animator = GetComponent<Animator>();
            animator.speed = animationSpeed;
        }
    }
}
