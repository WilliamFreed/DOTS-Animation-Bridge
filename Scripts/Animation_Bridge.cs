using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Reflection;
using Unity.Collections;

[RequireComponent(typeof(Animator))]
public class Animation_Bridge : MonoBehaviour
{
    [Header("Animator Parameter Object Reference")]
    [Tooltip("Scriptable object asset that holds parameters in a list, setup data list using animation parameter editor tool")]
    [SerializeField] private Animation_ParameterObject _playerAnimParams;

    [Header("Return Curve Values")]
    [Tooltip("Assign a parameter here if its controlled by an animation curve. Value will be relayed to DOTS/ECS")]
    [SerializeField] private List<string> ReturnFloats;

    [Header("Character Tag")]
    [Tooltip("Tag for identifying your character type from animation tag")]
    [SerializeField] private string CharacterTag;


    private List<int> _returnFloatHash;

    /// <summary>
    /// Players gameobject animator
    /// </summary>
    private Animator _animator;

    /// <summary>
    /// Entity Manager cache
    /// </summary>
    private EntityManager _entityManager;

    /// <summary>
    /// Entity Cache
    /// </summary>
    private Entity _entity;

    /// <summary>
    /// Coroutine used to poll for entity object for setup
    /// </summary>
    private Coroutine _pollRoutine = null;

    /// <summary>
    /// Animator Bridge Component Cache
    /// </summary>
    private AnimationBridge _bridge;

    private Dictionary<int,int> lookupDictionary;

#region Setup Functions

    void Awake()
    {
        // cache animator and entity manager
        _animator = GetComponent<Animator>();
        _playerAnimParams.GetReference();
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // start poll routine to look for player entity
        _pollRoutine = StartCoroutine(nameof(PollTillReady));
        GetReturnFloatHashes();
    }

    /// <summary>
    /// Entity setup function that prepares the bridge component and parameters, 
    /// and then assigns them to the player entity
    /// </summary>
    private void SetupEntity()
    {
        // create a new cached bridge component we will use to assign the data on the entity 
        AnimationBridge _bridge = new()
        {
            AnimSpeed = 1,
            Disabled = false,
            Ragdoll = false
        };

        // Add a default bridge component to the entity
        _entityManager.AddComponentData(_entity,_bridge);


        // grab parameter count to cache for loop limiter and buffer capacity
        int l = _playerAnimParams.Parameters.Count;

        // Creat and then grab buffer reference to fill
        _entityManager.AddBuffer<AnimParamBuffer>(_entity);
        DynamicBuffer<AnimParamBuffer> temp = _entityManager.GetBuffer<AnimParamBuffer>(_entity);

        // loop over the parameter list in the parameter object
        // assign the parameter to the buffer
        for(int i = 0; i < l; i++)
        {
            temp.Add(new(){ Parameter = _playerAnimParams.Parameters[i]});
        }
    }

    /// <summary>
    /// Coroutine function assigned to _pollRoutine in awake, 
    /// creates an entity query to find the player entity based on the player input component, 
    /// runs the query / yields until there is a single result, and then triggers the find singleton function 
    /// </summary>
    /// <returns></returns>
    private IEnumerator PollTillReady()
    {
        // create the entity query 
        var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<AnimationTag>());
        while (query.CalculateEntityCount() == 0){ yield return null; }
        FindEntityFromTag(query);
    }

    /// <summary>
    /// assigns the cached entity and then disposes the entity query created in _pollRoutine
    /// </summary>
    /// <param name="query"></param>
    private void FindEntityFromTag(EntityQuery query)
    {
        var tagHash = Animator.StringToHash(CharacterTag);
        var id = gameObject.GetEntityId();
        var animationEntities = GetAllWith<AnimationTag>();
        foreach(var e in animationEntities)
        {
            var tag = _entityManager.GetComponentData<AnimationTag>(e);
            if(tag.CharacterTag == tagHash)
            {
                if(tag.InstanceID == id)
                {
                    _entity = e;
                    break;
                }
            }
        }
        query.Dispose();
        animationEntities.Dispose();
        SetupEntity();
        _pollRoutine = null;
    }

    public NativeArray<Entity> GetAllWith<T>() where T : unmanaged, IComponentData
    {
        var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
        var entities = query.ToEntityArray(Allocator.Temp);
        query.Dispose();
        return entities;
    }

#endregion

#region Update Functions

    private void Update()
    {
        // if poll routine isnt null were still searching for the player entity
        // exit update
        if(_pollRoutine != null){ return; }

        // if player entity doesnt have animator bridge component
        // run entity setup and exit update
        if (!_entityManager.HasComponent<AnimationBridge>(_entity))
        {
            SetupEntity();
        }
        
        // grab bridge component from entity manager and cache it
        _bridge = _entityManager.GetComponentData<AnimationBridge>(_entity);

        // if disabled flag is true in animator bridge disable animator component
        if (_bridge.Disabled)
        {
            if(_animator.enabled){ _animator.enabled = false; }
            return;
        }
        // if disabled flag is false, enable animator if it is disabled
        else
        {
            if(!_animator.enabled){ _animator.enabled = true; }
        }

        // set animator speed from bridge component
        _animator.speed = _bridge.AnimSpeed;

        // Detect changes then send them to animator
        DetectChanges();
    }

    /// <summary>
    /// Detects changes stores in ECS Animator Bridge and propogates them to animator
    /// </summary>
    private void DetectChanges()
    {
        // grab buffer from entity
        DynamicBuffer<AnimParamBuffer> buffer = _entityManager.GetBuffer<AnimParamBuffer>(_entity);

        // for each parameter in the bridge hash mapping
        // detect its parameter type from its T(int) value
        // switch case sends value to proper animator component function
        int iterator = 0;
        foreach(var p in _playerAnimParams.Parameters)
        {
            var param = buffer[iterator];
            switch (param.Parameter.T)
            {
                case 0: // bool value
                    if(param.Parameter.GetValue(out bool boolValue))
                    {
                        _animator.SetBool(param.Parameter.Parameter,boolValue);
                    }
                break;
                case 1: // trigger
                    if(param.Parameter.GetValue(out bool triggerValue))
                    {
                        StartCoroutine(TriggerRoutine(param.Parameter.Parameter));
                    }
                break;
                case 2: // float value
                    if(param.Parameter.GetValue(out float floatValue))
                    {
                        _animator.SetFloat(param.Parameter.Parameter,floatValue);
                    }
                break;
                case 3: // int value
                    if(param.Parameter.GetValue(out int intValue))
                    {
                        _animator.SetInteger(param.Parameter.Parameter,intValue);
                    }
                break;
            }
            iterator++;
        }
        if(_returnFloatHash != null && _returnFloatHash.Count > 0)
        {
            SetReturnFloats(buffer);
        }
    }

    private void GetReturnFloatHashes()
    {
        _returnFloatHash = new();
        foreach(var p in _animator.parameters)
        {
            foreach(var f in ReturnFloats)
            {
                if(p.name == f)
                {
                    _returnFloatHash.Add(p.nameHash);
                }
            }
        }
    }

    /// <summary>
    /// Grabs any float parameters in ReturnFloats list from the animator,
    ///  and updates them in the cached bridge before setting bridge value in ecs
    /// </summary>
    private void SetReturnFloats(DynamicBuffer<AnimParamBuffer> buffer)
    {
        if(lookupDictionary == null || lookupDictionary.Count == 0)
        {
            lookupDictionary = TypeToDictionaryHelper.GetDictionary<int,int>(_playerAnimParams.StaticReferenceClass,_playerAnimParams.EnumName);
        }
        
        /// for each parameter listed in ReturnFloats list
        /// grab its value from the animator and set it in the bridge
        foreach(var parameter in _returnFloatHash)
        {

            float animF = _animator.GetFloat(parameter);
            int lookupHash = lookupDictionary[parameter];
            buffer[lookupHash].Parameter.SetValue(animF);
        }

        // set the bridge data back to the entity component
        _entityManager.SetComponentData(_entity,_bridge);
    }

    /// <summary>
    /// Coroutine used to update triggers in the Animator. 
    /// Sets trigger, yields for a frame, and then resets trigger in animator. 
    /// This prevents the animator from holding trigger values for longer than a frame
    ///  which is often the reason for unwated delayed trigger response,
    /// can be replaced with simple SetTrigger call in DetectChanges function if desired
    /// </summary>
    /// <param name="hash"></param>
    /// <returns></returns>
    private IEnumerator TriggerRoutine(int hash)
    {
        _animator.SetTrigger(hash);
        yield return null;
        _animator.ResetTrigger(hash);
    }

#endregion

}

public static class EnumUtils
{
    public static object GetEnumFromString(Type enumType, string name, bool ignoreCase = true)
    {
        if (!enumType.IsEnum)
            throw new ArgumentException($"{enumType} is not an enum type");

        return Enum.Parse(enumType, name, ignoreCase);
    }
}

public static class TypeToDictionaryHelper
{
    /// <summary>
    /// Gets a static Dictionary field from a Type
    /// </summary>
    /// <param name="type">The Type of the static class</param>
    /// <param name="fieldName">The static field name</param>
    /// <returns>The dictionary, or null if not found</returns>
    public static Dictionary<TKey,TValue> GetDictionary<TKey,TValue>(Type type, string fieldName)
    {
        if(type == null) throw new ArgumentNullException(nameof(type));
        if(string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException(nameof(fieldName));

        // Get the static field
        FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
        if(field == null) throw new Exception($"Field '{fieldName}' not found in type {type.Name}");

        // Get the value (pass null for static fields)
        object value = field.GetValue(null);

        if(value is Dictionary<TKey,TValue> dict)
            return dict;

        throw new Exception($"Field '{fieldName}' is not a Dictionary<{typeof(TKey).Name},{typeof(TValue).Name}>");
    }
}