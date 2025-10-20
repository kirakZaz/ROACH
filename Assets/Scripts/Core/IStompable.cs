using UnityEngine;

public interface IStomp
{
    /// Called when the player stomps this enemy from above.
    /// </summary>
    /// <param name="stomper">Player gameObject.</param>
    void TakeStomp(GameObject stomper);
}
