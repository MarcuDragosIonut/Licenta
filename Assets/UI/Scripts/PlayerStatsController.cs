using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Scripts
{
    public class PlayerStatsController : MonoBehaviour
    {
        public GameObject hp;
        public GameObject mp;

        private TMP_Text _hpText;
        private TMP_Text _mpText;

        private void Awake()
        {
            _hpText = hp.GetComponent<TMP_Text>();
            _mpText = mp.GetComponent<TMP_Text>();
        }

        public void UpdateStats(float hp, float maxHp, float mana, float maxMana)
        {
            _hpText.text = $"{(int)hp}/{(int)maxHp}";
            _mpText.text = $"{(int)mana} / {(int)maxMana}";
        }
    }
}
