using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class NetworkUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    private void Start()
    {
        if (hostButton != null)
            hostButton.onClick.AddListener(StartHost);

        if (clientButton != null)
            clientButton.onClick.AddListener(StartClient);
    }

    private void StartHost()
    {
        Debug.Log("Start Host");
        NetworkManager.Singleton.StartHost();
    }

    private void StartClient()
    {
        Debug.Log("Start Client");
        NetworkManager.Singleton.StartClient();
    }
}
