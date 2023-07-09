using NetSpell.SpellChecker;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class WritingWindow : EditorWindow
{
    private Spelling _spellCheck = null;
    private static string _text = "";
    private bool _focused;
    private bool _showSuggestion = false;
    private Vector2 _scrollPosition = Vector2.zero;

    private void OnEnable()
    {
        _spellCheck = new Spelling();
        _spellCheck.ShowDialog = false;
    }

    public static string Open(string currentText)
    {
        WritingWindow writingWindow = new WritingWindow();
        _text = currentText;

        writingWindow.ShowModal();
        return _text;
    }

    private void OnGUI()
    {
        GUI.SetNextControlName("text");
        _text = GUILayout.TextArea(_text);
        if (!_focused)
        {
            EditorGUI.FocusTextInControl("text");
            _focused = true;
        }
        if (GUILayout.Button("Spell check"))
        {
            _spellCheck.SpellCheck(_text);
            _spellCheck.Suggest(_spellCheck.CurrentWord);
            _showSuggestion = true;
        }

        if (_showSuggestion)
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int i = 0; i < _spellCheck.Suggestions.Count; i++)
            {
                if (GUILayout.Button(_spellCheck.Suggestions[i].ToString()))
                    ReplaceWord(_spellCheck.CurrentWord.ToString(), _spellCheck.Suggestions[i].ToString());
            }
            EditorGUILayout.EndScrollView();
        }

        if (GUILayout.Button("Close window"))
            Close();
    }

    private void ReplaceWord(string oldWord, string newWord)
    {
        if ('A' <= oldWord[0] && oldWord[0] <= 'Z')
        {
            string upperNewWorld = "";
            for (int i = 0; i < newWord.Length; i++)
            {
                if (i == 0)
                    upperNewWorld += (char)(newWord[i] - 32);
                else
                    upperNewWorld += newWord[i];
            }
            newWord = upperNewWorld;
        }

        string result = Regex.Replace(_text, Regex.Escape(oldWord), newWord.Replace("$", "$$"), RegexOptions.IgnoreCase);
        _text = result;

        _spellCheck.SpellCheck(_text);
        _spellCheck.Suggest(_spellCheck.CurrentWord);
    }
}
