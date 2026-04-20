using TMPro;
using UnityEngine;

namespace _2._Scripts.UI.Gameplay.VaultMessage
{
    public class VaultMessageView : UIView
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private string startMessage = "Time to open:";
        [SerializeField] private string endMessage = "seconds.";

        public void SetSeconds(float p_seconds) => text.text = $"{startMessage} {p_seconds:F1} {endMessage}";

        public override void Initialize(UIPresenter p_presenter)
        {
            base.Initialize(p_presenter);
            Hide();
        }
    }
}