using UnityEngine;
using UnityEngine.UI;

public class MessageManager : MonoBehaviour
{
    public static MessageManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    [SerializeField] private Text messageText;
    [SerializeField] private Scrollbar scrollbar;

    public void UpdateMessageText(string message)
    {
        messageText.text += message + "\n";
        scrollbar.value = 0;
    }
}
