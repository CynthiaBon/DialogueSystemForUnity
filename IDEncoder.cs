using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class IDEncoder
{
    private static List<char> _alphabet = new List<char>() { '_', 'e', 'a', 'r', 'i', 'o', 't', 'n', 's', 'l', 'c', 'u', 'd', 'p', 'm', 'h', 'g', 'b', 'f', 'y', 'w', 'k', 'v', 'x', 'z', 'j', 'q' };

    public static ulong Encode(string title)
    {
        if (title == null || title == "")
            return 0;

        ulong encodedId = 0;
        for (int i = 0; i < title.Length; i++)
        {
            ulong letterValue = (ulong)_alphabet.IndexOf(title[i]);
            encodedId += letterValue * (ulong)Math.Pow(27f, title.Length - i - 1);
        }
        return encodedId;
    }

    public static string Decode(ulong ID)
    {
        string reverseTitle = "";
        while (ID > 0)
        {
            ulong modulo = ID % 27;
            reverseTitle += _alphabet[(int)modulo];
            ID /= 27;
        }

        string title = "";
        for (int i = reverseTitle.Length - 1; i >= 0; i--)
        {
            title += reverseTitle[i];
        }

        return title;
    }

    public static ulong Parse(string stringId)
    {
        ulong value = 0;
        int length = stringId.Length;
        for (int i = 0; i < length; i++)
        {
            char c = stringId[i];
            value = 10 * value + (ulong)(c - 48);
        }
        return value;
    }
}
