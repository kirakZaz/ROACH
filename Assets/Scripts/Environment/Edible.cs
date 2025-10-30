using UnityEngine;

public class Edible : MonoBehaviour
{
    [Tooltip("How long Witchetty needs to eat this (seconds).")]
    public float eatDuration = 1.2f;

    [Tooltip("Shrink while eating for a simple feedback.")]
    public bool shrinkOnEat = true;

    public System.Collections.IEnumerator ConsumeAndDestroy()
    {
        float t = 0f;
        Vector3 start = transform.localScale;
        while (t < eatDuration)
        {
            t += Time.deltaTime;
            if (shrinkOnEat)
                transform.localScale = Vector3.Lerp(start, Vector3.zero, t / eatDuration);
            yield return null;
        }
        Object.Destroy(gameObject);
    }
}
