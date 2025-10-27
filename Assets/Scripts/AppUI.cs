using UnityEngine;
using UnityEngine.UI;

namespace CPS
{
    public class AppUI : MonoBehaviour
    {
        [SerializeField] private Button quitButton;

        void Start()
        {
            quitButton.onClick.AddListener(OnQuit);
        }

        void OnQuit()
        {
            Debug.Log("Quit!");
            Application.Quit();
        }
    }
}
