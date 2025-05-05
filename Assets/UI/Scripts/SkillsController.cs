using System;
using Characters.Player.Scripts;
using TMPro;
using UnityEngine;

namespace UI.Scripts
{
    public class SkillsController : MonoBehaviour
    {
        public int attackUpgradeCost;
        public int healthUpgradeCost;
        public int manaUpgradeCost;
        public int manaRegenUpgradeCost;

        public enum StatType
        {
            Attack,
            Health,
            Mana,
            ManaRegen
        }

        private int _attackUpgradeCount;
        private int _healthUpgradeCount;
        private int _manaUpgradeCount;
        private int _manaRegenUpgradeCount;

        private float _totalAttackBonus;
        private float _totalHealthBonus;
        private float _totalManaBonus;
        private float _totalManaRegenBonus;

        private TMP_Text _skillPointsCount;
        private TMP_Text _attackCost;
        private TMP_Text _attackBonus;
        private TMP_Text _healthCost;
        private TMP_Text _healthBonus;
        private TMP_Text _manaCost;
        private TMP_Text _manaBonus;
        private TMP_Text _manaRegenCost;
        private TMP_Text _manaRegenBonus;
        private PlayerController _playerController;

        private void Awake()
        {
            _playerController = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
            _skillPointsCount = transform.GetChild(1).GetComponent<TMP_Text>();
            _attackBonus = transform.GetChild(6).GetComponent<TMP_Text>();
            _attackCost = transform.GetChild(10).GetComponent<TMP_Text>();
            _healthBonus = transform.GetChild(7).GetComponent<TMP_Text>();
            _healthCost = transform.GetChild(11).GetComponent<TMP_Text>();
            _manaBonus = transform.GetChild(8).GetComponent<TMP_Text>();
            _manaCost = transform.GetChild(12).GetComponent<TMP_Text>();
            _manaRegenBonus = transform.GetChild(9).GetComponent<TMP_Text>();
            _manaRegenCost = transform.GetChild(13).GetComponent<TMP_Text>();

            UpdateSkillPanel();
        }

        private void OnEnable()
        {
            UpdateSkillPanel();
        }

        public void UpgradeAttack()
        {
            UpgradeStat(StatType.Attack);
        }

        public void UpgradeHealth()
        {
            UpgradeStat(StatType.Health);
        }

        public void UpgradeMana()
        {
            UpgradeStat(StatType.Mana);
        }

        public void UpgradeManaRegen()
        {
            UpgradeStat(StatType.ManaRegen);
        }

        private void UpdateSkillPanel()
        {
            _skillPointsCount.text = $"Skill Points: {_playerController.GetSkillPoints()}";

            _attackBonus.text =
                $"{(int)(_totalAttackBonus * 100)}.{(int)(_totalAttackBonus * 1000) % 10}% Attack Multiplier";
            _attackCost.text = $"Cost: {GetUpgradeCost(StatType.Attack)}";

            _healthBonus.text = $"+{(int)_totalHealthBonus} Health";
            _healthCost.text = $"Cost: {GetUpgradeCost(StatType.Health)}";

            _manaBonus.text = $"+{(int)_totalManaBonus} Mana";
            _manaCost.text = $"Cost: {GetUpgradeCost(StatType.Mana)}";

            _manaRegenBonus.text =
                $"+{(int)(_totalManaRegenBonus * 100)}.{(int)(_totalManaRegenBonus * 1000) % 10}% ManaRegen";
            _manaRegenCost.text = $"Cost: {GetUpgradeCost(StatType.ManaRegen)}";
        }

        private void UpgradeStat(StatType statType)
        {
            var upgradeCost = GetUpgradeCost(statType);
            if (_playerController.GetSkillPoints() < upgradeCost) return;

            var bonusValue = GetBonusValue(statType);
            _playerController.UpgradeStat(upgradeCost, statType, bonusValue);

            switch (statType)
            {
                case StatType.Attack:
                    _attackUpgradeCount++;
                    _totalAttackBonus += bonusValue;
                    break;
                case StatType.Health:
                    _healthUpgradeCount++;
                    _totalHealthBonus += bonusValue;
                    break;
                case StatType.Mana:
                    _manaUpgradeCount++;
                    _totalManaBonus += bonusValue;
                    break;
                case StatType.ManaRegen:
                    _manaRegenUpgradeCount++;
                    _totalManaRegenBonus += bonusValue;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }

            UpdateSkillPanel();
        }

        private int GetUpgradeCost(StatType statType)
        {
            Debug.Log("Attack: " + attackUpgradeCost + " " + _attackUpgradeCount + " " +
                      (_attackUpgradeCount == 0
                          ? 0
                          : attackUpgradeCost / (_attackUpgradeCount + 1)));
            return statType switch
            {
                StatType.Attack => attackUpgradeCost + (_attackUpgradeCount == 0
                    ? 0
                    : attackUpgradeCost / (_attackUpgradeCount + 1)),
                StatType.Health => healthUpgradeCost + (_healthUpgradeCount == 0
                    ? 0
                    : healthUpgradeCost / (_healthUpgradeCount + 2)),
                StatType.Mana => manaUpgradeCost + (_manaUpgradeCount == 0
                    ? 0
                    : manaUpgradeCost / (_manaUpgradeCount + 3)),
                StatType.ManaRegen => manaRegenUpgradeCost + (_manaRegenUpgradeCount == 0
                    ? 0
                    : manaRegenUpgradeCost / (_manaRegenUpgradeCount + 2)),
                _ => 3
            };
        }

        private float GetBonusValue(StatType statType)
        {
            const float decayRate = 0.96f;
            return statType switch
            {
                StatType.Attack => _attackUpgradeCount == 0
                    ? 0.05f
                    : Math.Max(0.005f, 0.05f * Mathf.Pow(decayRate, _attackUpgradeCount)),
                StatType.Health => _healthUpgradeCount == 0
                    ? 15.0f
                    : Math.Max(3.0f, Mathf.Round(15.0f * Mathf.Pow(decayRate, _healthUpgradeCount))),
                StatType.Mana => _manaUpgradeCount == 0
                    ? 5.0f
                    : Math.Max(1.0f, Mathf.Round(5.0f * Mathf.Pow(decayRate, _manaUpgradeCount))),
                StatType.ManaRegen => _manaRegenUpgradeCount == 0
                    ? 0.075f
                    : Math.Max(0.01f, 0.075f * Mathf.Pow(decayRate, _manaRegenUpgradeCount)),
                _ => 0.0f
            };
        }
    }
}