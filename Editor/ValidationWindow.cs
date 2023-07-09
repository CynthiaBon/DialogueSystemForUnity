using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ValidationWindow : EditorWindow
{
    private static bool _result = false;
    private static string _textToDisplay = null;

    public static bool Open(string textToDisplay)
    {
        _textToDisplay = textToDisplay;
        ValidationWindow dropdownWindow = new ValidationWindow();
        dropdownWindow.ShowModal();
        return _result;
    }

    private void OnGUI()
    {
        GUILayout.Label(_textToDisplay);
        if (GUILayout.Button("Yes"))
        {
            _result = true;
            Close();
        }
        if (GUILayout.Button("No"))
        {
            _result = false;
            Close();
        }
    }
}
