using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using NaughtyAttributes;
using System;
using UnityEngine.Events;
using System.Linq;

public class DialogueManager : LocalManager<DialogueManager>
{
    [SerializeField] private float _textSpeed = 0f;
    [SerializeField] private List<TextSpeedData> _textSpeedData = null;
    [SerializeField] public List<Characters> Npcs = null;
    [SerializeField] private List<UnityEvent> _customEvents = null;
    [Foldout("Setup")] [SerializeField] private TextMeshProUGUI _textDisplay = null;
    [Foldout("Setup")] [SerializeField] private TextMeshProUGUI _speakerNameDisplay = null;
    [Foldout("Setup")] [SerializeField] private List<TextMeshProUGUI> _choicesDisplay = null;

    public List<UnityEvent> CustomEvents { get { return _customEvents; } }
    public float DefaultTextSpeed { get; private set; }

    private bool _displayDialogue = false;
    private bool _areChoicesDisplayed = false;

    private Sentence _currentSentence = null;
    private string _currentText = null;
    private List<Sentence> _currentChoices = null;

    private float _timer = 0f;
    private readonly float _delayBetweenLetters = 10f;
    private int _letterIndex = 0;
    private int _choiceIndex = 0;

    private TextSpeed _textSpeedState = TextSpeed.Normal;
    private float _textSpeedSetting { get { return _textSpeed * _textSpeedData.Find(ts => ts.Name == _textSpeedState).Speed; } }

    protected override void Awake()
    {
        base.Awake();
        DefaultTextSpeed = _textSpeed;

        if (!PlayerPrefs.HasKey("TextSpeed"))
            PlayerPrefs.SetString("TextSpeed", $"{TextSpeed.Normal}");
        else
            _textSpeedState = (TextSpeed)Enum.Parse(typeof(TextSpeed), PlayerPrefs.GetString("TextSpeed"));
    }

    public void SetTextSpeed(TextSpeed textSpeed)
    {
        _textSpeedState = textSpeed;
        PlayerPrefs.SetString("TextSpeed", $"{textSpeed}");
    }

    public void StartDialogue(string characterName)
    {
        UIManager.Instance.ShowDialogueBox();
        Player.Instance.EnableDialogueInputs();
        UIManager.Instance.HideInteractionUI();
        Player.Instance.GetPlayerComponent<Interaction>().DisableInteraction();
        if (UIDeviceManager.Instance.CurrentInputDeivce == InputsDevicesNames.Keyboard)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        ulong firstSentenceId = DatabaseManager.Database.GetFirstSentenceID(characterName);
        if (firstSentenceId == 0)
        {
            Player.Instance.GetPlayerComponent<Dialogue>().DisableDialogue();
            return;
        }

        _currentSentence = DatabaseManager.Database.GetSentence(firstSentenceId);
        if (_currentSentence == null)
        {
            Player.Instance.GetPlayerComponent<Dialogue>().DisableDialogue();
            return;
        }

        _displayDialogue = true;
        InitializeSentence();
    }

    public void NextSentence()
    {
        if (_displayDialogue)
        {
            _displayDialogue = false;
            _textDisplay.text = _currentText;
            if (_currentSentence.HasChoice && !_areChoicesDisplayed)
            {
                DisplayChoices();
                return;
            }
            UIManager.Instance.ShowSkipCursor();
        }
        else if (!_currentSentence.HasChoice)
        {
            OnEndEvents();
            UIManager.Instance.HideSkipCursor();
            if (SetNextSentence())
            {
                InitializeSentence();
                _displayDialogue = true;
            }
        }
    }

    private void Update()
    {
        if (!_displayDialogue)
            return;

        if (_letterIndex < _currentText.Length)
        {
            _timer += Time.deltaTime * _textSpeedSetting;
            if (_timer >= _delayBetweenLetters)
            {
                while (_timer >= _delayBetweenLetters && _letterIndex < _currentText.Length)
                {
                    _textDisplay.text += _currentText[_letterIndex];
                    _timer -= _delayBetweenLetters;
                    _letterIndex += 1;
                }
            }
        }
        else
        {
            _displayDialogue = false;
            if (!_currentSentence.HasChoice)
                UIManager.Instance.ShowSkipCursor();
            else if (!_areChoicesDisplayed)
                DisplayChoices();
        }
    }

    private void InitializeSentence()
    {
        _currentText = _currentSentence.Text;
        _speakerNameDisplay.text = _currentSentence.Speaker;
        _textDisplay.text = "";
        _letterIndex = 0;
        _timer = _delayBetweenLetters;
    }

    private bool SetNextSentence()
    {
        if (_currentSentence.HasChoice)
        {
            if (_currentChoices[_currentChoices.Count - _choiceIndex - 1].NextSentenceID == 0)
            {
                Player.Instance.GetPlayerComponent<Dialogue>().DisableDialogue();
                _displayDialogue = false;
                return false;
            }
            _currentSentence = DatabaseManager.Database.GetSentence(_currentChoices[_currentChoices.Count - _choiceIndex - 1].NextSentenceID);
        }
        else
        {
            if (_currentSentence.NextSentenceID == 0)
            {
                Player.Instance.GetPlayerComponent<Dialogue>().DisableDialogue();
                _displayDialogue = false;
                return false;
            }
            _currentSentence = DatabaseManager.Database.GetSentence(_currentSentence.NextSentenceID);
        }

        if (_currentSentence == null)
        {
            Player.Instance.GetPlayerComponent<Dialogue>().DisableDialogue();
            _displayDialogue = false;
            return false;
        }
        else if (_currentSentence.Text == null || _currentSentence.Text == "")
        {
            OnEndEvents();
            Player.Instance.GetPlayerComponent<Dialogue>().DisableDialogue();
            UIManager.Instance.HideChoiceBox();
            _displayDialogue = false;
            return false;
        }

        return true;
    }

    private void OnEndEvents()
    {
        if (_currentSentence.OnEndEvents != null)
            _currentSentence.OnEndEvents.ForEach(method => method.Method(method.Argument, this));
    }

    private void DisplayChoices()
    {
        if (_currentSentence.ChoiceIDs == null || _currentSentence.ChoiceIDs.Count == 0)
            return;

        _currentChoices = new List<Sentence>();
        _currentSentence.ChoiceIDs.ForEach(id =>
        {
            Sentence choice = DatabaseManager.Database.GetSentence(id);
            if (choice != null && DatabaseManager.Database.IsChoiceValid(id, IDEncoder.Encode("ganesh")))
                _currentChoices.Add(choice);
        });

        if (_currentChoices == null || _currentChoices.Count == 0)
            return;

        _choiceIndex = _currentChoices.Count - 1;
        _areChoicesDisplayed = true;
        UIManager.Instance.ShowChoiceBox(_currentChoices.Count);
        UIManager.Instance.SetCursorPosition(_choiceIndex);

        for (int i = 0; i < _currentChoices.Count; i++)
        {
            _choicesDisplay[_currentChoices.Count - 1 - i].text = _currentChoices[i].Text;
        }
    }

    public void MoveChoiceSelection(int direction)
    {
        _choiceIndex += direction;
        if (_choiceIndex == -1)
            _choiceIndex = _currentChoices.Count - 1;
        if (_choiceIndex == _currentChoices.Count)
            _choiceIndex = 0;
        UIManager.Instance.SetCursorPosition(_choiceIndex);
    }

    public void SetChoiceSelection(int newIndex)
    {
        _choiceIndex = newIndex;
        UIManager.Instance.SetCursorPosition(_choiceIndex);
    }

    public void SelectChoice()
    {
        if (!_areChoicesDisplayed)
            return;
        OnEndEvents();
        _areChoicesDisplayed = false;
        if (!SetNextSentence())
            return;
        InitializeSentence();
        _displayDialogue = true;
        UIManager.Instance.HideChoiceBox();
    }

    public void SetQueryVariableFrom(string argument)
    {
        DatabaseManager.SetQueryVariable(argument);
    }

    public bool IsQueryVariableValid(string argument)
    {
        return DatabaseManager.IsQueryVariableValid(argument);
    }

    #region Events

    private void PlayTalkAnimation(string argument, DialogueManager self)
    {
        string[] splitArgument = argument.Split('>');
        NPC npc = self.Npcs.Find(character => character.CharacterName == splitArgument[0])?.Npc;
        if (splitArgument[1] == "True")
            npc.PlayTalk();
        else
            npc.PlayIdleTalk();

    }

    private void SetQueryVariable(string argument, DialogueManager self)
    {
        self.SetQueryVariableFrom(argument);
    }

    private void SetTextSpeed(string stringSpeed, DialogueManager self)
    {
        if (stringSpeed == "Default")
        {
            SetTextSpeedAt(self.DefaultTextSpeed);
            return;
        }

        int speed = int.Parse(stringSpeed);
        SetTextSpeedAt(speed);
    }

    public void SetTextSpeedAt(float textSpeed)
    {
        _textSpeed = textSpeed;
    }

    private void LookAtTarget(string argument, DialogueManager self)
    {
        string[] splitArgument = argument.Split('>');
        if (splitArgument[0] == "player")
        {
            Transform npc = self.Npcs.Find(character => character.CharacterName == splitArgument[1])?.Npc.LookAtPoint;
            if (npc != null)
                Player.Instance.GetPlayerComponent<PlayerCamera>().LookAtNPC(npc);
        }
        else
        {
            NPC npc = self.Npcs.Find(character => character.CharacterName == splitArgument[0])?.Npc;
            if (npc != null)
                npc.LookAtPlayer();
        }
    }

    private void SetNPCLayer(string argument, DialogueManager self)
    {
        string[] splitArgument = argument.Split('>');
        NPC npc = self.Npcs.Find(character => character.CharacterName == splitArgument[0])?.Npc;
        if (npc != null)
            npc.SetLayer(splitArgument[1]);
    }

    private void AddLogEntry(string argument, DialogueManager self)
    {
        int index = int.Parse(argument);
        Player.Instance.GetPlayerComponent<Menu>().AddEntry(index);
    }

    private void CustomEvent(string stringId, DialogueManager self)
    {
        int id = int.Parse(stringId);

        if (id >= self.CustomEvents.Count)
            return;

        self.CustomEvents[id].Invoke();
    }

    #endregion Events
}
