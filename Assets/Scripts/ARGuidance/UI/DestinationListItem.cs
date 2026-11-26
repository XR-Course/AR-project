using System;
using CampusNavigation.Pathfinding;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampusNavigation.ARGuidance
{
    /// <summary>One row in the destination list. DestinationSelector clones a template row.</summary>
    public class DestinationListItem : MonoBehaviour
    {
        [SerializeField]
        private Button button;

        [SerializeField]
        private Image background;

        [SerializeField]
        private TMP_Text nameText;

        [SerializeField]
        private TMP_Text detailText;

        [SerializeField]
        private Color normalColor = new(1f, 1f, 1f, 0.08f);

        [SerializeField]
        private Color selectedColor = new(0.1f, 0.7f, 1f, 0.45f);

        [SerializeField]
        private Color detailColor = new(1f, 1f, 1f, 0.7f);

        [SerializeField]
        private Color unreachableDetailColor = new(1f, 0.55f, 0.45f, 1f);

        public CampusNode Node { get; private set; }

        public void Bind(CampusNode node, string detail, bool reachable, Action<DestinationListItem> onClick)
        {
            Node = node;
            nameText.text = node.displayName;
            detailText.text = detail;
            detailText.color = reachable ? detailColor : unreachableDetailColor;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick(this));

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            background.color = selected ? selectedColor : normalColor;
        }
    }
}
