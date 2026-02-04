using UnityEngine;
using Unity.Entities;

public class AnimationTagAuthoring : MonoBehaviour
{
    public string CharacterTag;

    public void Awake()
    {
        EntityManager m = World.DefaultGameObjectInjectionWorld.EntityManager;
        Entity e = m.CreateEntity();
        AnimationTag tag = new()
        {
            CharacterTag = Animator.StringToHash(CharacterTag),
            InstanceID = gameObject.GetEntityId()
        };
        m.AddComponentData(e,tag);
    }
}

public struct AnimationTag : IComponentData
{
    public int CharacterTag;
    public int InstanceID;
}