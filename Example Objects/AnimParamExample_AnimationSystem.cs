using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
[BurstCompile]
public partial struct AnimParamExampleAnimationSystem : ISystem
{
private NativeHashMap<int,int> LookupHashmap;
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AnimationBridge>();
         LookupHashmap = new(PlayerAnimsClassLookup.ParamReverseLookupDictionary.Count,Allocator.Persistent);
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
      //foreach
      //(
          //var (animationBridge,entity) in
          //SystemAPI.Query<RefRW<AnimationBridge>>().WithEntityAccess().WithAll<CUSTOMTAG>())
          //{
              // STICK INPUT EXAMPLE
              //DynamicBuffer<AnimParamBuffer> buffer = SystemAPI.GetBuffer<AnimParamBuffer>(entity);
              //var leftStickXBuffer = buffer[LookupHashmap[(int)AnimParamExample.X]];
              //var leftStickYBuffer = buffer[LookupHashmap[(int)AnimParamExample.Y]];
              //float2 lStickValue = LEFTSTICKINPUT;
              //leftStickXBuffer.Parameter.SetValue(lStickValue.x);
              //leftStickYBuffer.Parameter.SetValue(lStickValue.y);
              //buffer[LookupHashmap[(int)AnimParam.X]] = leftStickX;
              //buffer[LookupHashmap[(int)AnimParam.Y]] = leftStickY;

              // JUMP EXAMPLE
              //var jumpBuffer = buffer[LookupHashmap[(int){ParamEnumName}.Jump]];
              //if(JUMPPRESSED)
              //{
                   // Sets a trigger
                   //jumpBuffer.Parameter.SetValue(true,true);
              //}

           //}
     }
}
