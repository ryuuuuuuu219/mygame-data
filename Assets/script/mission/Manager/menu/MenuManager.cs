using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace Assets.script.mission.Arry.Player
{
    public class MenuManager : MonoBehaviour
    {
        public Canvas canvas;
        public GameObject menuUI;
        public TextMeshProUGUI text;
        public bool ismenu;

        // Use this for initialization
        void Start()
        {
            ismenu = false;
            menuUI.SetActive(ismenu);

            text.text = " 一時停止中……"+
            "\n\n\n\n\n×：メニューを閉じる"+
            "\n〇：リザルトへ";
        }

        // Update is called once per frame
        void Update()
        {
            var inputManager = InputManager.Instance;
            if (inputManager.menu)
            {
                ismenu = !ismenu;
                menuUI.SetActive(ismenu);
            }
                Time.timeScale = ismenu ? 0 : 1;

            if (ismenu)
            {
                if (inputManager.cancel)
                {
                    ismenu = false;
                    menuUI.SetActive(ismenu);
                }
                else
                {
                    if (inputManager.submit)
                    {
                        // メニュー内での決定処理をここに追加
                        SceneManager.LoadScene("Result");
                    }
                }
            }

        }
    }
}
