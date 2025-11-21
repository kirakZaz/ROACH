using UnityEngine;

namespace Roach.Assets.Scripts.Enemies
{
    public class VertMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float speed = 3f;
        public float flightDistance = 3f;
        

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

            transform.position = Vector2.MoveTowards(targetPos, transform.position, step);

        
        }

        void OnDrawingGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                new Vector3(transform.position.x, transform.position.y - flightDistance, 0),
                new Vector3(transform.position.x, transform.position.y + flightDistance, 0)
            );
        }
    }
}