using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Text;

/// <summary>
/// Editor tool used to generate an animation parameter list for converting at runtime to ECS
/// </summary>
public class Animation_ParameterListGenerator : EditorWindow
{
    [Header("Animator Controller")]
    public AnimatorController AnimationControllerAsset;

    [Header("Parameter Holder Object")]
    public Animation_ParameterObject AnimationParameterScriptable;

    [Header("Parameter Holder Object")]
    public string ParamEnumName;

    public bool GenerateDOTSSystem, UseControllerNameForReferences;


    [MenuItem("Tools/Dots Animation/ Dots Animation Parameter Object Setup")]
    public static void Open()
    {
        GetWindow<Animation_ParameterListGenerator>("Dots Animation Parameter Setup");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("DOTS Animation Parameter Object and Enum Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        
        AnimationControllerAsset = (AnimatorController)EditorGUILayout.ObjectField(
            new GUIContent("Target Animator Controller","Animator Controller to Target. CANNOT BE NULL"),
            AnimationControllerAsset,
            typeof(AnimatorController),
            false
        );

        AnimationParameterScriptable = (Animation_ParameterObject)EditorGUILayout.ObjectField(
            new GUIContent("Target Parameter Object","The Parameter Object to write to, if null one will be generated"),
            AnimationParameterScriptable,
            typeof(Animation_ParameterObject), 
            false
        );

        UseControllerNameForReferences = EditorGUILayout.Toggle(
    new GUIContent(
        "Use Controller Name for References",
        "If enabled, generated enums, classes, and assets will use the Animator Controller's name"
    ),
    UseControllerNameForReferences
    );

    GenerateDOTSSystem = EditorGUILayout.Toggle(
        new GUIContent(
            "Generate DOTS System",
            "If enabled, also generates a DOTS ECS system wired to the generated animation parameters"
        ),
        GenerateDOTSSystem
    );

        if (!UseControllerNameForReferences)
        {
            ParamEnumName = EditorGUILayout.TextField(new GUIContent("Parameter Enum Name","The name to assign to the enum you will reference in your dots systems for this character"), ParamEnumName);
        }
        else
        {
            ParamEnumName = AnimationControllerAsset.name.Replace(" ","");
        }

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(AnimationControllerAsset == null))
        {
            if (GUILayout.Button("Generate Parameters"))
            {
                AnimationParameterScriptable = GetOrCreateParameterScriptable( AnimationControllerAsset, AnimationParameterScriptable );
                Generate(AnimationControllerAsset, AnimationParameterScriptable);
            }
        }
    }

    private static Animation_ParameterObject GetOrCreateParameterScriptable(
    AnimatorController controller,
    Animation_ParameterObject existing
    )
    {
        // if object is assigned in editor return it
        if (existing != null){ return existing; }

        // Get controller path
        var controllerPath = AssetDatabase.GetAssetPath(controller);
        var folderPath = Path.GetDirectoryName(controllerPath);
        var n = controller.name.Replace(" ","");
        var assetName = $"{n} - Parameter Object.asset";
        var assetPath = Path.Combine(folderPath, assetName).Replace("\\", "/");

        // Create new ScriptableObject
        var parameterObject = ScriptableObject.CreateInstance<Animation_ParameterObject>();
        AssetDatabase.CreateAsset(parameterObject, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = parameterObject;
        return parameterObject;
    }

    public void Generate(AnimatorController controller, Animation_ParameterObject dataObj)
    {
        if (controller == null || dataObj == null)
        {
            Debug.LogError("Select an AnimatorController first.");
            return;
        }

        dataObj.Parameters ??= new();
        if(dataObj.Parameters.Count > 0)
        {
            dataObj.Parameters.Clear();
        }

        var controllerPath = AssetDatabase.GetAssetPath(controller);
        var folderPath = Path.GetDirectoryName(controllerPath);
        var n = controller.name.Replace(" ","");
        var assetName = $"{ParamEnumName}_Enum.cs";
        var assetPath = Path.Combine(folderPath, assetName).Replace("\\", "/");
        if(ParamEnumName == null){ ParamEnumName = n; }
        string parameterEnumName = ParamEnumName;
        string ecsLookupEnumName = $"{ParamEnumName}Lookup";
        string path = $"{assetPath}";

        Directory.CreateDirectory(Path.GetDirectoryName(path));

        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated. Do not edit.");
        sb.AppendLine($"using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine($"public enum {parameterEnumName}_AnimParam");
        sb.AppendLine("{");

        foreach (var param in controller.parameters)
        {
            string safeName = MakeSafeEnumName(param.name);
            sb.AppendLine($"    {safeName} = {Animator.StringToHash(param.name)} ,");
        }

        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine();
        sb.AppendLine($"public enum {ecsLookupEnumName}");
        sb.AppendLine("{");

        int iterator = 0;
        foreach (var param in controller.parameters)
        {
            string safeName = MakeSafeEnumName(param.name);
            sb.AppendLine($"    {safeName} = {iterator} ,");
            iterator++;
        }

        sb.AppendLine("}");

        sb.AppendLine();
        sb.AppendLine($"public static class {ParamEnumName}ClassLookup");
        sb.AppendLine("{");
        sb.AppendLine();
        
        sb.AppendLine($"public static Dictionary<int,int> ParamLookupDictionary = new()");
        sb.AppendLine("{");

        iterator = 0;
        foreach (var param in controller.parameters)
        {
            if(iterator == controller.parameters.Length - 1)
            {
                sb.AppendLine("{" + $"{iterator},{Animator.StringToHash(param.name)}" + "}");
            }
            else
            {
                sb.AppendLine("{" + $"{iterator},{Animator.StringToHash(param.name)}" + "},");
            }
            iterator++;
        }

        sb.AppendLine("};");

        sb.AppendLine();

        sb.AppendLine();
        
        sb.AppendLine($"public static Dictionary<int,int> ParamReverseLookupDictionary = new()");
        sb.AppendLine("{");

        iterator = 0;
        foreach (var param in controller.parameters)
        {
            if(iterator == controller.parameters.Length - 1)
            {
                sb.AppendLine("{" + $" {Animator.StringToHash(param.name)},{iterator}" + " }");
            }
            else
            {
                sb.AppendLine("{" + $"{Animator.StringToHash(param.name)},{iterator}" + " },");
            }
            iterator++;
        }

        sb.AppendLine("};");
        sb.AppendLine("}");
        sb.AppendLine();

        File.WriteAllText(path, sb.ToString());
        AssetDatabase.Refresh();

        if (GenerateDOTSSystem)
        {
            var systemName = $"{ParamEnumName}_AnimationSystem.cs";
            var systemPath = Path.Combine(folderPath, systemName).Replace("\\", "/");

            sb = new();

            sb.AppendLine("using Unity.Burst;");
            sb.AppendLine("using Unity.Collections;");
            sb.AppendLine("using Unity.Entities;");
            sb.AppendLine("using Unity.Mathematics;");
            sb.AppendLine();
            sb.AppendLine("[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]");
            sb.AppendLine("[UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]");
            sb.AppendLine("[BurstCompile]");
            sb.AppendLine($"public partial struct {ParamEnumName}AnimationSystem : ISystem");
            sb.AppendLine("{");
            sb.AppendLine("private NativeHashMap<int,int> LookupHashmap;");

            sb.AppendLine("    public void OnCreate(ref SystemState state)");
            sb.AppendLine("    {");
            sb.AppendLine("        state.RequireForUpdate<AnimationBridge>();");
            sb.AppendLine("         LookupHashmap = new(PlayerAnimsClassLookup.ParamReverseLookupDictionary.Count,Allocator.Persistent);");
            sb.AppendLine($"        var dictionaryLookup = {ParamEnumName}ClassLookup.ParamReverseLookupDictionary;");
            sb.AppendLine($"        foreach(var a in dictionaryLookup)");
            sb.AppendLine("        {");
            sb.AppendLine("            LookupHashmap.Add(a.Key,a.Value);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("");
            sb.AppendLine("    [BurstCompile]");
            sb.AppendLine("    public void OnDestroy(ref SystemState state)");
            sb.AppendLine("    {");
            sb.AppendLine("        if(LookupHashmap.IsCreated){ LookupHashmap.Dispose(); }");
            sb.AppendLine("    }");

            sb.AppendLine("    [BurstCompile]");
            sb.AppendLine("    public void OnUpdate(ref SystemState state)");
            sb.AppendLine("    {");
            sb.AppendLine("      //foreach");
            sb.AppendLine("      //(");
            sb.AppendLine("          //var (animationBridge,entity) in");
            sb.AppendLine("          //SystemAPI.Query<RefRW<AnimationBridge>>().WithEntityAccess().WithAll<CUSTOMTAG>())");
            sb.AppendLine("          //{");
            sb.AppendLine("              // STICK INPUT EXAMPLE");
            sb.AppendLine("              //DynamicBuffer<AnimParamBuffer> buffer = SystemAPI.GetBuffer<AnimParamBuffer>(entity);");
            sb.AppendLine($"              //var leftStickXBuffer = buffer[LookupHashmap[(int){ParamEnumName}.X]];");
            sb.AppendLine($"              //var leftStickYBuffer = buffer[LookupHashmap[(int){ParamEnumName}.Y]];");
            sb.AppendLine("              //float2 lStickValue = LEFTSTICKINPUT;");
            sb.AppendLine("              //leftStickXBuffer.Parameter.SetValue(lStickValue.x);");
            sb.AppendLine("              //leftStickYBuffer.Parameter.SetValue(lStickValue.y);");
            sb.AppendLine("              //buffer[LookupHashmap[(int)AnimParam.X]] = leftStickX;");
            sb.AppendLine("              //buffer[LookupHashmap[(int)AnimParam.Y]] = leftStickY;");
            sb.AppendLine();
            sb.AppendLine("              // JUMP EXAMPLE");
            sb.AppendLine("              //var jumpBuffer = buffer[LookupHashmap[(int){ParamEnumName}.Jump]];");
            sb.AppendLine("              //if(JUMPPRESSED)");
            sb.AppendLine("              //{");
            sb.AppendLine("                   // Sets a trigger");
            sb.AppendLine("                   //jumpBuffer.Parameter.SetValue(true,true);");
            sb.AppendLine("              //}");
            sb.AppendLine();
            sb.AppendLine("           //}");
            sb.AppendLine("     }");
            sb.AppendLine("}");

            File.WriteAllText(systemPath, sb.ToString());
            AssetDatabase.Refresh();
        }
        
        foreach (var param in controller.parameters)
        {
            dataObj.Parameters.Add(ConvertParameter(param));
        }

        string fullName = $"{ParamEnumName}ClassLookup";
        dataObj.StaticReferenceName = fullName;
        
        AssetDatabase.Refresh();
        Debug.Log($"Generated enum: {parameterEnumName} and saved Scriptable Library for DOTS Animation");
    }

    private AnimParameterECS ConvertParameter(AnimatorControllerParameter p)
    {
        int hash = Animator.StringToHash(p.name);
        int t = 0;

        switch (p.type)
        {
            case AnimatorControllerParameterType.Bool:
                t = 0;
            break;
            case AnimatorControllerParameterType.Trigger:
                t = 1;
            break;
            case AnimatorControllerParameterType.Float:
                t = 2;
            break;
            case AnimatorControllerParameterType.Int:
                t = 3;
            break;
        }

        return new()
        {
            Parameter = (AnimParam)hash,
            T = t,
            IsDirty = 0,
            Float = new(),
            Bool = new(),
            Int = new()
        };
    }

    private string MakeSafeEnumName(string name)
    {
        // Replace spaces and invalid chars
        string result = name.Replace(" ", "_");
        result = System.Text.RegularExpressions.Regex.Replace(
            result,
            @"[^a-zA-Z0-9_]",
            "_"
        );

        // Enum members can't start with numbers
        if (char.IsDigit(result[0]))
            result = "_" + result;

        return result;
    }
}
