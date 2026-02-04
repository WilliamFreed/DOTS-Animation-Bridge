using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
[BurstCompile]
public partial struct AnimParamExampleAnimationSystem : ISystem
{
    private ExampleTimer Timer;
    private NativeHashMap<int,int> LookupHashmap;
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AnimationBridge>();
         LookupHashmap = new(AnimParamExampleClassLookup.ParamReverseLookupDictionary.Count,Allocator.Persistent);
        var dictionaryLookup = AnimParamExampleClassLookup.ParamReverseLookupDictionary;
        foreach(var a in dictionaryLookup)
        {
            LookupHashmap.Add(a.Key,a.Value);
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if(LookupHashmap.IsCreated){ LookupHashmap.Dispose(); }
    }
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      foreach
      (
          var (bridge,tag,entity) in
          SystemAPI.Query<RefRW<AnimationBridge>,AnimationTag>().WithEntityAccess())
          {
              DynamicBuffer<AnimParamBuffer> buffer = SystemAPI.GetBuffer<AnimParamBuffer>(entity);
              var moveBuffer = buffer[LookupHashmap[(int)AnimParamExample_AnimParam.Move]];
              Timer.Timer += SystemAPI.Time.DeltaTime;
              if(Timer.Timer > 4f)
                {
                    Timer.Timer = 0;
                }

                if(Timer.Timer > 2f)
            {
                moveBuffer.Parameter.SetValue(1f);
            }
            else
            {
                moveBuffer.Parameter.SetValue(0f);
            }
            buffer[LookupHashmap[(int)AnimParamExample_AnimParam.Move]] = moveBuffer;
           }
     }
}

public struct ExampleTimer
{
    public float Timer;
}
