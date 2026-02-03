using UnityEngine;
using TMPro;

public class GameVersion : MonoBehaviour
{
    public TextMeshProUGUI versionText;

    void Start()
    {
        versionText.text = Application.version;
    }
}