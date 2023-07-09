using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DialogueTest : MonoBehaviour
{
    [SerializeField] string testString = "";
    void Start()
    {
        ulong id = IDEncoder.Encode(testString);
        Debug.Log(id);
        string decodedString = IDEncoder.Decode(id);
        Debug.Log(decodedString);

        /*List<int> list1 = new List<int>() { 1, 2, 3 };
        List<int> list2 = new List<int>() { 1, 4, 3, 5 };
        List<int> intersect = list1.Intersect(list2).ToList();

        intersect.ForEach(nb => Debug.Log(nb));*/
    }
}
