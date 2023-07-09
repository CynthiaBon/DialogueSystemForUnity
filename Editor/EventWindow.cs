using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class EventWindow : EditorWindow
{
    private static EventMethodContainer methodEvent = null;

    //Set query variable
    private bool _isWorld = true;
    private string _queryName = "";
    private string _queryCharacterName = "";
    private int _queryValue = 0;

    //Set text speed
    private bool _isDefaultTextSpeed = false;
    private float _textSpeed = 0;

    //Look at
    private string _looker = null;
    private string _target = null;

    //Set layer
    private string _npc = null;
    private string _layer = null;

    //Animation
    private string _animNpc = null;
    private bool _playAnim = false;

    //Log entry
    private int _logIndex = 0;

    //Custom event
    private int _eventId = 0;

    public static EventMethodContainer Open()
    {
        EventWindow dropdownWindow = new EventWindow();
        dropdownWindow.ShowModal();
        return methodEvent;
    }

    private void OnGUI()
    {
        QueryVariableDisplay();
        DrawLine();
        TextSpeedDisplay();
        DrawLine();
        LookAtDisplay();
        DrawLine();
        SetNPCLayerDisplay();
        DrawLine();
        AnimationDisplay();
        DrawLine();
        AddLogEntryDisplay();
        DrawLine();
        CustomEventDisplay();
    }

    private void QueryVariableDisplay()
    {
        _queryName = EditorGUILayout.TextField("Query name", _queryName);
        _isWorld = EditorGUILayout.Toggle("Is world ?", _isWorld);
        if (!_isWorld)
            _queryCharacterName = EditorGUILayout.TextField("Query character name", _queryCharacterName);
        _queryValue = EditorGUILayout.IntField("Query value", _queryValue);
        if (IsTextValid(_queryName) && (_isWorld || IsTextValid(_queryCharacterName)) && GUILayout.Button("Add \"Set query vairable event\""))
        {
            string argument =  _isWorld ? "World:" : "Characters:";
            argument += _isWorld ? _queryName : $"{_queryCharacterName}.{_queryName}";
            argument += $":{_queryValue}";
            methodEvent = new EventMethodContainer("SetQueryVariable", argument);
            Close();
        }
    }

    private void TextSpeedDisplay()
    {
        _isDefaultTextSpeed = EditorGUILayout.Toggle("Set text speed to default ?", _isDefaultTextSpeed);
        _textSpeed = EditorGUILayout.FloatField("Text speed", _textSpeed);
        if ((_textSpeed > 0 || _isDefaultTextSpeed) && GUILayout.Button("Add \"Set text speed event\""))
        {
            methodEvent = new EventMethodContainer("SetTextSpeed", _textSpeed.ToString());
            Close();
        }
    }

    private void LookAtDisplay()
    {
        _looker = EditorGUILayout.TextField("Looker", _looker);
        _target = EditorGUILayout.TextField("Target", _target);

        if (GUILayout.Button("Add \"Look at event\""))
        {
            methodEvent = new EventMethodContainer("LookAtTarget", $"{_looker}>{_target}");
            Close();
        }
    }

    private void SetNPCLayerDisplay()
    {
        _npc = EditorGUILayout.TextField("Npc", _npc);
        _layer = EditorGUILayout.TextField("Layer", _layer);

        if (GUILayout.Button("Add \"Set layer event\""))
        {
            methodEvent = new EventMethodContainer("SetNPCLayer", $"{_npc}>{_layer}");
            Close();
        }
    }

    private void AnimationDisplay()
    {
        _animNpc = EditorGUILayout.TextField("Npc", _animNpc);
        _playAnim = EditorGUILayout.Toggle("Play talk animation ?", _playAnim);
        if (GUILayout.Button("Add \"Play animation event\""))
        {
            methodEvent = new EventMethodContainer("PlayTalkAnimation", $"{_animNpc}>{_playAnim}");
            Close();
        }
    }

    private void AddLogEntryDisplay()
    {
        _logIndex = EditorGUILayout.IntField("Log index", _logIndex);
        if (GUILayout.Button("Add \"Add log entry event\""))
        {
            methodEvent = new EventMethodContainer("AddLogEntry", $"{_logIndex}");
            Close();
        }
    }

    private void CustomEventDisplay()
    {
        _eventId = EditorGUILayout.IntField("Event id", _eventId);
        if (_eventId >= 0 && GUILayout.Button("Add \"Custom event\""))
        {
            methodEvent = new EventMethodContainer("CustomEvent", _eventId.ToString());
            Close();
        }
    }

    private bool IsTextValid(string text)
    {
        return 0 < text.Length && text.Length <= 12 && Regex.IsMatch(text, "^[a-z_]*$");
    }

    private void DrawLine()
    {
        GUILayout.Label("");
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("");
    }
}
