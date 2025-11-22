using UnityEngine;

namespace Roach.Assets.Scripts.Enemies
{
    public class FlyMovementVertical : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float speed = 3f;
        public float flightDistance = 3f;
        
        [Header("Visual Settings")]
        public bool flipHorizontalOnTurn = false; // Optional: flip X instead of Y
        public bool rotateOnTurn = false; // Optional: rotate instead
        public float upRotation = 0f;
        public float downRotation = 180f;

        private Vector2 startPosition;
        private bool movingUp = true;
        private SpriteRenderer spriteRenderer;

        void Start()
        {
            startPosition = transform.position;
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            Move();
        }

        void Move()
        {
            float step = speed * Time.deltaTime;
            Vector2 targetPos = movingUp
                ? new Vector2(transform.position.x, startPosition.y + flightDistance)
                : new Vector2(transform.position.x, startPosition.y - flightDistance);

            transform.position = Vector2.MoveTowards(transform.position, targetPos, step);

            if (Vector2.Distance(transform.position, targetPos) < 0.05f)
            {
                movingUp = !movingUp;

                // Optional visual changes on turn
                if (rotateOnTurn)
                {
                    float targetRotation = movingUp ? upRotation : downRotation;
                    transform.rotation = Quaternion.Euler(0, 0, targetRotation);
                }
                else if (flipHorizontalOnTurn && spriteRenderer != null)
                {
                    spriteRenderer.flipX = !spriteRenderer.flipX;
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                new Vector3(transform.position.x, transform.position.y - flightDistance, 0),
                new Vector3(transform.position.x, transform.position.y + flightDistance, 0)
            );
        }
    }
}