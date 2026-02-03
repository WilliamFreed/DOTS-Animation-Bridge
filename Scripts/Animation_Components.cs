#region Usings
using System;
using Unity.Entities;
#endregion

#region Animator Bridge ECS

/// <summary>
/// Serves as the component bridge between mono / ECS
/// </summary>
public struct AnimationBridge : IComponentData
{
    /// <summary>
    /// bool for disabling the animator
    /// </summary>
    public bool Disabled;

    /// <summary>
    /// bool for enabling a ragdoll
    /// </summary>
    public bool Ragdoll;

    /// <summary>
    /// Speed value for setting the animator speed
    /// </summary>
    public float AnimSpeed;
}

#endregion

#region Parameter Interface

/// <summary>
/// Interface from which parameters are derived
/// </summary>
public interface IAnimParamValue
{
    public void SetValue(float value);
    public void SetValue(bool value);
    public void SetValue(int value);
}

#endregion

#region Parameter Structs 

[Serializable]
public struct AnimParamBuffer : IBufferElementData
{
    public int ID;
    public AnimParameterECS Parameter;
}

[Serializable]
public struct AnimParameterECS
{
    public AnimParam Parameter;
    public FloatValue Float;
    public ByteValue Bool;
    public IntValue Int;
    public byte IsDirty;
    public int T;
    public void SetValue(bool value,bool isTrigger = false)
    {
        T = isTrigger ? 1 : 0;
        Bool.SetValue(value);
        IsDirty = 1;
    }

    public void SetValue(float value)
    {
        T = 2;
        Float.SetValue(value);
        IsDirty = 1;
    }

    public void SetValue(int value)
    {
        T = 3;
        Int.SetValue(value);
        IsDirty = 1;
    }

    public bool GetValue(out float _value)
    {
        _value = Float.Value;
        return ConvertDirty();
    }

    public bool GetValue(out bool _value)
    {
        _value = ConvertByte();
        return ConvertDirty();
    }

    public bool GetValue(out int _value)
    {
        _value = Int.Value;
        return ConvertDirty();
    }

    private readonly bool ConvertByte()
    {
        return Bool.Value > 0;
    }

    private bool ConvertDirty()
    {
        if(IsDirty > 0)
        {
            if(T == 1){ Bool.Value = 0; }
            IsDirty = 0;
            return true;
        }
        return false;
    }
}


[Serializable]
public struct ByteValue : IAnimParamValue
{
    public byte Value;

    public void SetValue(float value)
    {
        ErrorLogging.Log(LogType.Message,"Byte Set Value float");
    }

    public void SetValue(bool value)
    {
        Value = value? (byte)1 : (byte)0;
    }

    public void SetValue(int value)
    {
        ErrorLogging.Log(LogType.Message,"Byte Set Value Int");
    }
}

[Serializable]
public struct FloatValue : IAnimParamValue
{
    public float Value;

    public void SetValue(float value)
    {
        Value = value;
    }

    public void SetValue(bool value)
    {
        ErrorLogging.Log(LogType.Message,"Float Set Value Bool");
    }

    public void SetValue(int value)
    {
        ErrorLogging.Log(LogType.Message,"Float Set Value Int");
    }
}

[Serializable]
public struct IntValue : IAnimParamValue
{
    public int Value;
   
    public void SetValue(float value)
    {
        ErrorLogging.Log(LogType.Message,"Int Set Value Float");
    }

    public void SetValue(bool value)
    {
        ErrorLogging.Log(LogType.Message,"Int Set Value Bool");
    }

    public void SetValue(int value)
    {
        Value = value;
    }
}

#endregion