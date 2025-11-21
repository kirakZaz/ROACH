using UnityEngine;

namespace Roach.Assets.Scripts.Enemies
{
    public class FlyMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float speed = 3f;
        public float flightDistance = 3f;
        public bool flipOnTurn = true;

        private Vector2 startPosition;
        private bool movingRight = true;
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
            Vector2 targetPos = movingRight
                ? new Vector2(startPosition.x + flightDistance, transform.position.y)
                : new Vector2(startPosition.x - flightDistance, transform.position.y);

            transform.position = Vector2.MoveTowards(transform.position, targetPos, step);

            if (Vector2.Distance(transform.position, targetPos) < 0.05f)
            {
                movingRight = !movingRight;

                if (flipOnTurn && spriteRenderer != null)
                    spriteRenderer.flipX = !spriteRenderer.flipX;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                new Vector3(transform.position.x - flightDistance, transform.position.y, 0),
                new Vector3(transform.position.x + flightDistance, transform.position.y, 0)
            );
        }
    }
}
