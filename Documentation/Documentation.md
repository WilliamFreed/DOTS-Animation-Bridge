# DOTS Animation Bridge
## How To Use
### Relevant Scripts


- ***Animation_Bridge***(monobehaviour) 
this lives on the Game Object holding your Animator.  You will need to assign an ***Animation_ParmeterObject*** , as well as any curve controlled float values you want sent back to DOTS.

- ***AnimationTagAuthoring***(monobehaviour) 
this lives on the Game Object holding your Animator along with the Animation_Bridge.  The CharacterTag string variable here must match
the CharacterTag variable set in your Animation_Bridge.

- ***Animation_ParameterObject***(scriptable object) 
is a scriptable object that is created when you use the editor tool generator, or from the create sub menu in Unity. You should not need to interact with this object other than assigning it in the ***Animation_Bridge*** on your character. You must re-generate this object whenever you change parameters in the Unity Animator Controller

- ***Animation_Components*** (various)
this script holds the relevant DOTS components and structs for running the system.  Relevant here is the ***AnimationBridge*** component. 

- ***AnimationBridge*** Component -  this component serves as your gateway between DOTS / GameObject world.  This is the component that will live on the object holding the buffer with the data you need to interact with.  This also has built in capabilities for disabling the animator from DOTS Systems, as well as triggering a ***Ragdoll*** from a DOTS System 

## Using the Editor Tool
 All you need to use the editor tool is a valid Animator Controller Asset for the character you are working with.  The editor tool will create and propogate values for your ***Animation_ParameterObject*** as well as generate a .cs script with a custom enum you can use to easily reference your parameters from a DOTS System alongside a static class that your character will reference to decode its buffer mapping.

    Generated Scripts
    - Custom Enum type 
    - StaticReferenceClass
        - ParamLookupDictionary <index,HashInt>
        - ReverseParamLookupDictionary <HashInt,index>
    - Custom DOTS AnimationSystem (optional)

## Code Examples
### Editing Animator Parameters from an ECS System

#### Example of a Jump Trigger 
##### using a character controller named "Player" (custom generate enum "Player_AnimParam")

    ''' csharp

        foreach
        (
            var (bridge,entity) in 
            SystemAPI.Query<RefRW<AnimationBridge>>().WithEntityAccess().WithAll<CUSTOMTAG>())
            {
                DynamicBuffer<AnimParamBuffer> buffer = SystemAPI.GetBuffer<AnimParamBuffer>(entity);

                if (JUMP INPUT TRIGGERED)
                {
                    int jumpIndex = LookupHashmap[(int)Player_AnimParam.Jump];
                    var jumpBuffer = buffer[jumpIndex];
                    jumpBuffer.Parameter.SetValue(true, isTrigger: true);
                    buffer[jumpIndex] = jumpBuffer;
                }
            }

         
### Example of a float input 
#### using a character controller named "Ogre" (custom generate enum "Ogre_AnimParam")

    ''' csharp

        foreach
        (
            var (bridge,entity) in 
            SystemAPI.Query<RefRW<AnimationBridge>>().WithEntityAccess().WithAll<CUSTOMTAG>())
            {
                DynamicBuffer<AnimParamBuffer> buffer = SystemAPI.GetBuffer<AnimParamBuffer>(entity);

                int moveXIndex = LookupHashmap[(int)Ogre_AnimParam.MoveX];
                int moveYIndex = LookupHashmap[(int)Ogre_AnimParam.MoveY];

                var moveXBuffer = buffer[moveXIndex];
                var moveYBuffer = buffer[moveYIndex];

                moveXBuffer.Parameter.SetValue(X FLOAT VALUE);
                moveYBuffer.Parameter.SetValue(Y FLOAT VALUE);

                buffer[moveXIndex] = moveXBuffer;
                buffer[moveYIndex] = moveYBuffer;
                
            }
      
