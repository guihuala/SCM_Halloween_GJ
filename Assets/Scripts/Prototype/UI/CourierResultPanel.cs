using UnityEngine.UI;
namespace NightCourier
{
    public sealed class CourierResultPanel : CourierPanel
    {
        public Text titleText, summaryText;
        public Button restartButton, menuButton;
        protected override void Awake()
        {
            base.Awake(); restartButton.onClick.AddListener(() => owner.RestartShift());
            menuButton.onClick.AddListener(() => GameManager.Instance.ReturnToMainMenu());
        }
        protected override void Refresh()
        {
            titleText.text = Run.State == ShiftState.Won ? "夜班完成！" : "这班未能完成";
            summaryText.text = "成功送达  " + Run.Delivered + " / 4 件\n本班收入  ¥" + Run.Earnings + "\n已用时间  " + NightCourierPrototype.Minutes(Run.Elapsed) + "\n\n" + Run.Journal[0];
        }
    }
}
