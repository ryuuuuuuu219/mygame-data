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
        float missionClearBonus;
        float hpRate;
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
            int stageIndex = PlayerPrefs.GetInt("selectedstage", 0) + 1;
            missionClearBonus = stageIndex * 1000f;
            hpRate = Mathf.Clamp01(PlayerPrefs.GetFloat("HpRate", 0));
            finalScore = (missionClearBonus + score - timeBonus) * hpRate;
            if (finalScore > hiscore)
            {
                PlayerPrefs.SetFloat(scene_name + "_hiscore", finalScore);
                PlayerPrefs.Save();
            }

            droppedWeapon = WeaponStorage.GenerateDrop(finalScore, stageIndex, scene_name);
        }

        // Update is called once per frame
        void Update()
        {
            hudText.text = "Stage Clear!\n" +
                "Mission Clear Bonus: " + missionClearBonus.ToString("F0") + "\n" +
                "Score: " + score.ToString("F0") + "\n" +
                "Time Bonus: -" + timeBonus.ToString("F0") + "\n" +
                "HP Rate: " + hpRate.ToString("P0") + "\n" +
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
