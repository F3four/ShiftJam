using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private TextMeshProUGUI _txtFinalScore;
    [SerializeField] private TextMeshProUGUI _txtPersonalBest;
    [SerializeField] private Button _restartButton;

    private void Start()
    {
        if (_gameManager != null && _txtFinalScore != null && _txtPersonalBest != null)
        {
            _txtFinalScore.text = $"Final Score: {_gameManager.Score}";
            _txtPersonalBest.text = $"Personal Best: ";//{_gameManager.PersonalBest}
        }

        if (_restartButton != null)
        {
            _restartButton.onClick.AddListener(RestartGame);
        }
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}