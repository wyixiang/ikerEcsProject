using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TMG.Survivors
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button _playButton_normal;
        [SerializeField] private Button _playButton_ecs;
        [SerializeField] private Button _quitButton;

        private void OnEnable()
        {
            _playButton_normal.onClick.AddListener(OnPlayNormalButton);
            _playButton_ecs.onClick.AddListener(OnPlayEcsButton);
            _quitButton.onClick.AddListener(OnQuitButton);
        }

        private void OnDisable()
        {
            _playButton_normal.onClick.RemoveAllListeners();
            _playButton_ecs.onClick.RemoveAllListeners();
            _quitButton.onClick.RemoveAllListeners();
        }

        private void OnPlayNormalButton()
        {
            SceneManager.LoadScene(1);
        }

        private void OnPlayEcsButton()
        {
            SceneManager.LoadScene(2);
        }

        private void OnQuitButton()
        {
            Application.Quit();
        }
    }
}