using System;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPanel : BasePanel
{
    [Header("Text")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text messageText;
    [SerializeField] private Text confirmText;
    [SerializeField] private Text cancelText;

    [Header("Actions")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action _onConfirm;
    private Action _onCancel;

    private void Start()
    {
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
    }

    public void Configure(string title, string message, Action onConfirm, Action onCancel = null, string confirmLabel = null, string cancelLabel = null)
    {
        _onConfirm = onConfirm;
        _onCancel = onCancel;

        SetText(titleText, title);
        SetText(messageText, message);
        SetText(confirmText, string.IsNullOrEmpty(confirmLabel) ? LocalizationManager.Get("common.confirm") : confirmLabel);
        SetText(cancelText, string.IsNullOrEmpty(cancelLabel) ? LocalizationManager.Get("common.cancel") : cancelLabel);
    }

    private void OnConfirmClicked()
    {
        Action callback = _onConfirm;
        CloseSelf();
        callback?.Invoke();
    }

    private void OnCancelClicked()
    {
        Action callback = _onCancel;
        CloseSelf();
        callback?.Invoke();
    }

    private void CloseSelf()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ClosePanel(panelName);
        }
        else
        {
            ClosePanel();
        }
    }

    private void SetText(Text targetText, string content)
    {
        if (targetText != null)
        {
            targetText.text = content;
        }
    }
}
