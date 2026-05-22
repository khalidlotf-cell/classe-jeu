using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathsClass
{
    // Écran scores (M_7_scores.jpg) : top 5 + stats joueur.
    public class ScoresScreen : MonoBehaviour
    {
        [System.Serializable]
        public class Row
        {
            public TMP_Text rank;
            public TMP_Text player;
            public TMP_Text score;
            public TMP_Text mode;
            public Image rankBadge; // facultatif (or/argent/bronze)
        }

        public Row[] rows;
        public TMP_Text bestText;
        public TMP_Text gamesPlayedText;
        public TMP_Text successRateText;
        public Button backButton;

        public Sprite goldSprite;
        public Sprite silverSprite;
        public Sprite bronzeSprite;

        void Awake()
        {
            if (backButton) backButton.onClick.AddListener(() => UIManager.Instance.Back());
        }

        void OnEnable()
        {
            var scores = SaveManager.LoadScores();
            for (int i = 0; i < rows.Length; i++)
            {
                bool has = i < scores.Count;
                if (rows[i].rank)   rows[i].rank.text   = has ? (i + 1).ToString() : "—";
                if (rows[i].player) rows[i].player.text = has ? scores[i].playerName : "";
                if (rows[i].score)  rows[i].score.text  = has ? scores[i].score.ToString() : "";
                if (rows[i].mode)   rows[i].mode.text   = has ? scores[i].mode : "";
                if (rows[i].rankBadge)
                {
                    rows[i].rankBadge.enabled = has;
                    if (has)
                    {
                        rows[i].rankBadge.sprite = i == 0 ? goldSprite : i == 1 ? silverSprite : i == 2 ? bronzeSprite : null;
                    }
                }
            }
            var stats = SaveManager.LoadStats();
            if (bestText)         bestText.text         = stats.bestScoreOverall.ToString();
            if (gamesPlayedText)  gamesPlayedText.text  = stats.gamesPlayed.ToString();
            if (successRateText)  successRateText.text  = (SaveManager.SuccessRate() * 100f).ToString("0") + " %";
        }
    }
}
