using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialoguePanel : BasePanel
{
    [Header("Content")]
    [SerializeField] private Text speakerNameText;
    [SerializeField] private Text dialogueText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image standingImage;
    [SerializeField] private Image boxBackground;

    [Header("Style")]
    [SerializeField] private GameObject speakerRoot;
    [SerializeField] private GameObject portraitRoot;
    [SerializeField] private GameObject standingRoot;
    [SerializeField] private Color normalColor = new Color(0.08f, 0.09f, 0.12f, 0.92f);
    [SerializeField] private Color narrationColor = new Color(0.04f, 0.04f, 0.05f, 0.9f);
    [SerializeField] private Color innerThoughtColor = new Color(0.11f, 0.08f, 0.16f, 0.92f);
    [SerializeField] private Color systemColor = new Color(0.1f, 0.12f, 0.14f, 0.94f);

    [Header("Control")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button autoPlayButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private Text autoPlayButtonText;
    [SerializeField] private Text hintText;
    [SerializeField] private float characterInterval = 0.035f;
    [SerializeField] private float acceleratedInterval = 0.005f;
    [SerializeField] private float defaultAutoPlayDelay = 1.2f;

    [Header("Options")]
    [SerializeField] private GameObject optionRoot;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private Text[] optionTexts;

    private readonly List<DialogueLine> _lines = new List<DialogueLine>();
    private readonly List<DialogueOption> _visibleOptions = new List<DialogueOption>();
    private Action _onComplete;
    private Coroutine _typingCoroutine;
    private Coroutine _autoPlayCoroutine;
    private int _lineIndex;
    private bool _isTyping;
    private bool _autoPlay;
    private bool _isAccelerating;
    private string _currentFullText;

    private void Start()
    {
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
        if (autoPlayButton != null) autoPlayButton.onClick.AddListener(OnAutoPlayClicked);
        if (skipButton != null) skipButton.onClick.AddListener(OnSkipClicked);
        InitOptionButtons();
        RefreshAutoPlayText();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            OnContinueClicked();
        }

        _isAccelerating = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }

    public void Play(IList<DialogueLine> lines, Action onComplete)
    {
        _lines.Clear();
        _lines.AddRange(lines);
        _onComplete = onComplete;
        _lineIndex = 0;
        _autoPlay = false;
        RefreshAutoPlayText();
        HideOptions();
        ShowCurrentLine();
    }

    private void OnContinueClicked()
    {
        if (_isTyping)
        {
            CompleteCurrentTyping();
            return;
        }

        if (_visibleOptions.Count > 0)
        {
            return;
        }

        ShowNextLine();
    }

    private void OnAutoPlayClicked()
    {
        _autoPlay = !_autoPlay;
        RefreshAutoPlayText();

        if (_autoPlay && !_isTyping)
        {
            StartAutoPlayNextLine(GetCurrentLineDelay());
        }
    }

    private void OnSkipClicked()
    {
        CompleteDialogue();
    }

    private void ShowCurrentLine()
    {
        if (_lineIndex < 0 || _lineIndex >= _lines.Count)
        {
            CompleteDialogue();
            return;
        }

        DialogueLine line = _lines[_lineIndex];
        HideOptions();
        ApplyLineVisuals(line);

        _currentFullText = DialogueVariableResolver.Resolve(line.text);
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
        }

        StopAutoPlayNextLine();
        _typingCoroutine = StartCoroutine(TypeText(_currentFullText));
    }

    private IEnumerator TypeText(string fullText)
    {
        _isTyping = true;
        SetText(dialogueText, string.Empty);

        for (int i = 0; i < fullText.Length; i++)
        {
            if (fullText[i] == '<')
            {
                int tagEndIndex = fullText.IndexOf('>', i);
                if (tagEndIndex >= 0)
                {
                    i = tagEndIndex;
                    continue;
                }
            }

            SetText(dialogueText, GetRichTextPreview(fullText, i + 1));
            float interval = _isAccelerating ? acceleratedInterval : characterInterval;
            if (interval > 0f)
            {
                yield return new WaitForSecondsRealtime(interval);
            }
        }

        _isTyping = false;
        _typingCoroutine = null;

        RefreshOptions();
        if (_visibleOptions.Count > 0)
        {
            _autoPlay = false;
            RefreshAutoPlayText();
            yield break;
        }

        if (_autoPlay)
        {
            StartAutoPlayNextLine(GetCurrentLineDelay());
        }
    }

    private IEnumerator AutoPlayNextLine(float delay)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, delay));
        _autoPlayCoroutine = null;

        if (_autoPlay && !_isTyping)
        {
            ShowNextLine();
        }
    }

    private void CompleteCurrentTyping()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        StopAutoPlayNextLine();
        _isTyping = false;
        SetText(dialogueText, _currentFullText);
        RefreshOptions();
    }

    private void ShowNextLine()
    {
        _lineIndex++;
        ShowCurrentLine();
    }

    private void CompleteDialogue()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        StopAutoPlayNextLine();
        _isTyping = false;
        _onComplete?.Invoke();
        _onComplete = null;
    }

    private void InitOptionButtons()
    {
        if (optionButtons == null) return;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int optionIndex = i;
            if (optionButtons[i] != null)
            {
                optionButtons[i].onClick.AddListener(() => OnOptionClicked(optionIndex));
            }
        }

        HideOptions();
    }

    private void RefreshOptions()
    {
        _visibleOptions.Clear();

        if (_lineIndex < 0 || _lineIndex >= _lines.Count)
        {
            HideOptions();
            return;
        }

        DialogueLine line = _lines[_lineIndex];
        if (line.options != null)
        {
            foreach (DialogueOption option in line.options)
            {
                if (option != null && DialogueRuntimeState.EvaluateCondition(option.condition))
                {
                    _visibleOptions.Add(option);
                }
            }
        }

        bool hasOptions = _visibleOptions.Count > 0;
        if (optionRoot != null)
        {
            optionRoot.SetActive(hasOptions);
        }

        if (optionButtons != null)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool visible = i < _visibleOptions.Count;
                if (optionButtons[i] != null)
                {
                    optionButtons[i].gameObject.SetActive(visible);
                }

                Text optionText = GetOptionText(i);
                if (optionText != null)
                {
                    optionText.text = visible ? DialogueVariableResolver.Resolve(_visibleOptions[i].text) : string.Empty;
                }
            }
        }

        if (hintText != null && hasOptions)
        {
            hintText.text = "请选择";
        }
    }

    private void HideOptions()
    {
        _visibleOptions.Clear();

        if (optionRoot != null)
        {
            optionRoot.SetActive(false);
        }

        if (optionButtons == null) return;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnOptionClicked(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= _visibleOptions.Count) return;

        DialogueOption option = _visibleOptions[optionIndex];
        DialogueRuntimeState.ApplyEffects(option.effects);
        HideOptions();

        if (!string.IsNullOrEmpty(option.nextDialogueId) &&
            DialogueManager.Instance != null &&
            DialogueManager.Instance.PlayById(option.nextDialogueId))
        {
            return;
        }

        ShowNextLine();
    }

    private void ApplyLineVisuals(DialogueLine line)
    {
        DialogueCharacterProfile character = line.character;
        string speakerName = !string.IsNullOrEmpty(line.speakerName)
            ? line.speakerName
            : character != null ? character.DisplayName : string.Empty;

        bool showSpeaker = line.style != DialogueBoxStyle.Narration && !string.IsNullOrEmpty(speakerName);
        if (speakerRoot != null) speakerRoot.SetActive(showSpeaker);
        SetText(speakerNameText, speakerName);

        Sprite portrait = line.portraitOverride != null
            ? line.portraitOverride
            : character != null ? character.GetExpressionPortrait(line.expression) : null;
        SetImage(portraitRoot, portraitImage, portrait);

        Sprite standing = line.standingOverride != null
            ? line.standingOverride
            : character != null ? character.DefaultStanding : null;
        SetImage(standingRoot, standingImage, standing);

        if (boxBackground != null)
        {
            boxBackground.color = GetStyleColor(line.style);
        }

        if (hintText != null)
        {
            hintText.text = _visibleOptions.Count > 0 ? "请选择" : (_autoPlay ? "自动播放中" : "点击继续");
        }
    }

    private void SetImage(GameObject root, Image image, Sprite sprite)
    {
        if (root != null)
        {
            root.SetActive(sprite != null);
        }

        if (image != null)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }

    private Color GetStyleColor(DialogueBoxStyle style)
    {
        switch (style)
        {
            case DialogueBoxStyle.Narration:
                return narrationColor;
            case DialogueBoxStyle.InnerThought:
                return innerThoughtColor;
            case DialogueBoxStyle.System:
                return systemColor;
            default:
                return normalColor;
        }
    }

    private float GetCurrentLineDelay()
    {
        if (_lineIndex < 0 || _lineIndex >= _lines.Count) return defaultAutoPlayDelay;
        return _lines[_lineIndex].autoPlayDelay > 0f ? _lines[_lineIndex].autoPlayDelay : defaultAutoPlayDelay;
    }

    private void RefreshAutoPlayText()
    {
        if (autoPlayButtonText != null)
        {
            autoPlayButtonText.text = _autoPlay ? "自动：开" : "自动：关";
        }

        if (hintText != null)
        {
            hintText.text = _autoPlay ? "自动播放中" : "点击继续";
        }
    }

    private void StartAutoPlayNextLine(float delay)
    {
        StopAutoPlayNextLine();
        _autoPlayCoroutine = StartCoroutine(AutoPlayNextLine(delay));
    }

    private void StopAutoPlayNextLine()
    {
        if (_autoPlayCoroutine != null)
        {
            StopCoroutine(_autoPlayCoroutine);
            _autoPlayCoroutine = null;
        }
    }

    private void SetText(Text target, string content)
    {
        if (target != null)
        {
            target.text = content;
        }
    }

    private Text GetOptionText(int index)
    {
        if (optionTexts == null || index < 0 || index >= optionTexts.Length) return null;
        return optionTexts[index];
    }

    private string GetRichTextPreview(string fullText, int rawEndIndex)
    {
        if (string.IsNullOrEmpty(fullText)) return string.Empty;

        string preview = fullText.Substring(0, Mathf.Clamp(rawEndIndex, 0, fullText.Length));
        List<string> openTags = new List<string>();

        for (int i = 0; i < preview.Length; i++)
        {
            if (preview[i] != '<') continue;

            int tagEndIndex = preview.IndexOf('>', i);
            if (tagEndIndex < 0) break;

            string tag = preview.Substring(i + 1, tagEndIndex - i - 1);
            if (string.IsNullOrEmpty(tag))
            {
                i = tagEndIndex;
                continue;
            }

            if (tag[0] == '/')
            {
                string closingName = GetTagName(tag.Substring(1));
                for (int tagIndex = openTags.Count - 1; tagIndex >= 0; tagIndex--)
                {
                    if (openTags[tagIndex] == closingName)
                    {
                        openTags.RemoveAt(tagIndex);
                        break;
                    }
                }
            }
            else if (!tag.EndsWith("/", System.StringComparison.Ordinal))
            {
                string tagName = GetTagName(tag);
                if (IsAutoClosedRichTextTag(tagName) == false)
                {
                    openTags.Add(tagName);
                }
            }

            i = tagEndIndex;
        }

        for (int i = openTags.Count - 1; i >= 0; i--)
        {
            preview += $"</{openTags[i]}>";
        }

        return preview;
    }

    private string GetTagName(string tag)
    {
        int separatorIndex = tag.IndexOfAny(new[] { ' ', '=' });
        return separatorIndex >= 0 ? tag.Substring(0, separatorIndex) : tag;
    }

    private bool IsAutoClosedRichTextTag(string tagName)
    {
        return tagName == "br";
    }
}
