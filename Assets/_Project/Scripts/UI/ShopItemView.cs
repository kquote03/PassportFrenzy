using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PassportFrenzy
{
    public class ShopItemView : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text descText;
        [SerializeField] TMP_Text costText;
        [SerializeField] Image swatch;

        BuffDefinition _buff;

        public void Bind(BuffDefinition b, Action onClick)
        {
            _buff = b;
            nameText.text = b.displayName;
            descText.text = b.description;
            costText.text = $"{b.cost} mi";
            swatch.color = b.color;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }

        public void Refresh(int miles)
        {
            if (_buff == null) return;
            costText.color = miles >= _buff.cost ? new Color(0.1f, 0.5f, 0.1f) : new Color(0.8f, 0.15f, 0.15f);
        }
    }
}
