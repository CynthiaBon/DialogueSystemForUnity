using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DropdownWindow : EditorWindow
{
    private static int _index = -1;
    private static string[] _options = null;

    public static int Open()
    {
        _options = new string[] { "World", "Character", "Filter" };
        DropdownWindow dropdownWindow = new DropdownWindow();
        dropdownWindow.ShowModal();
        return _index;
    }

    private void OnGUI()
    {
        for (int i = 0; i < _options.Length; i++)
        {
            if (GUILayout.Button(_options[i]))
            {
                _index = i;
                Close();
            }
        }
    }
}
