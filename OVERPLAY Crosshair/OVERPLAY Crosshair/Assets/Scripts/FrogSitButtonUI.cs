using UnityEngine;

public class FrogSitButtonUI : MonoBehaviour
{
    public DragObjectFrog frog;

    public void SitFrog()
    {
        if (frog != null)
            frog.Sit();
    }

    public void StandFrog()
    {
        if (frog != null)
            frog.Stand();
    }
} 