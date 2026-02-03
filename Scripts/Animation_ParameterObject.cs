#region Usings
using System;
using System.Collections.Generic;
using UnityEngine;
#endregion

#region Animation Parameter Object

/// <summary>
/// Scriptable object used to hold parameter references.  
/// Use editor tool to generate parameter data from animation controller. 
/// Parameter data is converted for ECS at runtime by animation bridge
/// </summary>
[CreateAssetMenu(menuName = "Dots Animation Bridge / Parameter Object", fileName = "New Animation Parameter Object")]
public class Animation_ParameterObject : ScriptableObject
{
    /// <summary>
    /// Generate this list using the editor tool
    /// </summary>
    public List<AnimParameterECS> Parameters;
    public string EnumName = "ParamReverseLookupDictionary";
    public string StaticReferenceName;
    public Type StaticReferenceClass;

    public void GetReference()
    {
        Type type = null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = asm.GetType(StaticReferenceName);
            if (type != null) break;
        }
        if(type == null) Debug.LogError("Type not found!");
        StaticReferenceClass = type;
    }
}

#endregion