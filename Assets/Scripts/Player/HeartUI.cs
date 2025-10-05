using UnityEngine;
using UnityEngine.UI;

public class HeartUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    
    public void SetFilled(bool filled)
    {
        fillImage.enabled = filled;
    }
}