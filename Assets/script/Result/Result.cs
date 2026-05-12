using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.script.Result
{
    public class Result : MonoBehaviour
    {
        float score;
        public string scene_name;
        float hiscore;
        float timeBonus;
        float hpBonus;
        float finalScore;
        WeaponDropData droppedWeapon;

        public TextMeshProUGUI hudText;          // HUDテキスト


        // Use this for initialization
        void Start()
        {
            scene_name = PlayerPrefs.GetString("LastScene");
            hiscore = PlayerPrefs.GetFloat(scene_name + "_hiscore", 0);
            score = PlayerPrefs.GetFloat("LastScore", 0);
            timeBonus = PlayerPrefs.GetFloat("TimeBonus", 0);
            hpBonus=PlayerPrefs.GetFloat("hpBonus", 0);
            finalScore = score - timeBonus + hpBonus;
            if (finalScore > hiscore)
            {
                PlayerPrefs.SetFloat(scene_name + "_hiscore", finalScore);
                PlayerPrefs.Save();
            }

            int stageIndex = PlayerPrefs.GetInt("selectedstage", 0) + 1;
            droppedWeapon = WeaponStorage.GenerateDrop(finalScore, stageIndex, scene_name);
        }

        // Update is called once per frame
        void Update()
        {
            hudText.text = "Stage Clear!\n" +
                "Score: " + score.ToString("F0") + "\n" +
                "Time Bonus: -" + timeBonus.ToString("F0") + "\n" +
                "HP Bonus: " + hpBonus.ToString("F0") + "\n" +
                "-------------------\n" +
                "Final Score: " + finalScore.ToString("F0") + "\n" +
                "Hiscore: " + hiscore.ToString("F0") + "\n" +
                "-------------------\n" +
                "Drop: " + (droppedWeapon != null ? droppedWeapon.displayName : "None");

            var keyInput = InputManager.Instance;

            if (keyInput != null)
            {
                if (keyInput.fireMissile)
                {
                    SceneManager.LoadScene("SetUp");
                }
            }
        }
    }
}
