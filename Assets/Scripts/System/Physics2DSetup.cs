using UnityEngine;

public class Physics2DSetup : MonoBehaviour
{
    void Awake()
    {
        int W(string n) => LayerMask.NameToLayer(n);
        void Enable(int a, int b)
        {
            if (a >= 0 && b >= 0)
                Physics2D.IgnoreLayerCollision(a, b, false);
        }

        int wichetty = W("Wichetty");
        int ground = W("Ground");
        int edible = W("Edible");

        Enable(wichetty, ground);
        Enable(wichetty, edible);
    }
}
